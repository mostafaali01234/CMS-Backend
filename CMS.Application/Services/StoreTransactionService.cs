// Services/StoreTransactionService.cs
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;
using CMS.Application.Interfaces;
using CMS.Application.Interfaces.Configuration;
using CMS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace CMS.Application.Services;

public class StoreTransactionService : IStoreTransactionService
{
    private readonly IAppDbContext _dbContext;
    private readonly IStockService _stock;
    private readonly ILogger<StoreTransactionService> _logger;

    private const int NotesMaxLength = 2000;

    public StoreTransactionService(
        IAppDbContext dbContext,
        IStockService stock,
        ILogger<StoreTransactionService> logger)
    {
        _dbContext = dbContext;
        _stock = stock;
        _logger = logger;
    }

    private StoreTransactionDto ToDto(StoreTransaction transaction)
    {
        return new StoreTransactionDto
        {
            Id = transaction.Id,
            TransactionDate = transaction.TransactionDate,
            AuditorId = transaction.AuditorId,
            AuditorName = transaction.Auditor?.Name ?? "",
            Notes = transaction.Notes,
            OutStoreId = transaction.OutStoreId,
            OutStoreName = transaction.OutStore?.Name ?? "",
            InStoreId = transaction.InStoreId,
            InStoreName = transaction.InStore?.Name ?? "",
            TransactionItems = transaction.Items?.Select(i => new TransactionItemsDto
            {
                ItemId = i.ProductId,
                ItemName = i.Product?.Name ?? "",
                Quantity = i.Quantity
            }).ToList() ?? new List<TransactionItemsDto>(),
            CreatedAtUtc = transaction.CreatedAtUtc,
            CreatedBy = transaction.CreatedBy ?? "",
            UpdatedAtUtc = transaction.UpdatedAtUtc,
            UpdatedBy = transaction.UpdatedBy ?? ""
        };
    }

    // Centralized validation for both Create and Update.
    private async Task<string?> ValidateAsync(StoreTransactionDto dto)
    {
        if (dto is null)
            return "Transaction payload is required";

        // --- TransactionDate ---
        if (dto.TransactionDate == default)
            return "TransactionDate is required";

        // --- Auditor (required FK) ---
        if (dto.AuditorId <= 0)
            return "AuditorId is required";

        var auditorExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == dto.AuditorId && !e.IsDeleted);

        if (!auditorExists)
            return $"Auditor (Employee) with Id {dto.AuditorId} was not found";

        // --- OutStore (required FK) ---
        if (dto.OutStoreId <= 0)
            return "OutStoreId is required";

        var outStoreExists = await _dbContext.Store
            .AnyAsync(s => s.Id == dto.OutStoreId && !s.IsDeleted);

        if (!outStoreExists)
            return $"Out store with Id {dto.OutStoreId} was not found";

        // --- InStore (required FK) ---
        if (dto.InStoreId <= 0)
            return "InStoreId is required";

        var inStoreExists = await _dbContext.Store
            .AnyAsync(s => s.Id == dto.InStoreId && !s.IsDeleted);

        if (!inStoreExists)
            return $"In store with Id {dto.InStoreId} was not found";

        // --- OutStore and InStore must differ ---
        if (dto.OutStoreId == dto.InStoreId)
            return "OutStoreId and InStoreId cannot be the same store";

        // --- Notes (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Length > NotesMaxLength)
            return $"Notes cannot exceed {NotesMaxLength} characters";

        // --- Transaction items (at least one required) ---
        if (dto.TransactionItems is null || dto.TransactionItems.Count == 0)
            return "At least one transaction item is required";

        var seenProductIds = new HashSet<long>();

        foreach (var item in dto.TransactionItems)
        {
            if (item.ItemId <= 0)
                return "Each transaction item must have a valid ItemId (ProductId)";

            if (!seenProductIds.Add(item.ItemId))
                return $"Product Id {item.ItemId} appears more than once in the transaction items";

            if (item.Quantity <= 0)
                return $"Quantity for product Id {item.ItemId} must be greater than zero";

            var productExists = await _dbContext.Product
                .AnyAsync(p => p.Id == item.ItemId && !p.IsDeleted);

            if (!productExists)
                return $"Product with Id {item.ItemId} was not found";
        }

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<StoreTransactionDto>>> GetAllAsync()
    {
        var transactions = await _dbContext.StoreTransaction
            .Where(z => !z.IsDeleted)
            .Include(t => t.Auditor)
            .Include(t => t.OutStore)
            .Include(t => t.InStore)
            .Include(t => t.Items!.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Product)
            .AsNoTracking()
            .ToListAsync();

        var dtos = transactions.Select(ToDto).ToList();
        return ServiceResult<List<StoreTransactionDto>>.Ok(dtos);
    }

    private static IQueryable<StoreTransaction> ApplyFilters(IQueryable<StoreTransaction> q, StoreTransactionFilterDto f)
    {
        if (f.DateFrom.HasValue) { var from = f.DateFrom.Value.Date; q = q.Where(t => t.TransactionDate >= from); }
        if (f.DateTo.HasValue) { var to = f.DateTo.Value.Date.AddDays(1); q = q.Where(t => t.TransactionDate < to); }

        if (f.OutStoreId.HasValue) q = q.Where(t => t.OutStoreId == f.OutStoreId.Value);
        if (f.InStoreId.HasValue) q = q.Where(t => t.InStoreId == f.InStoreId.Value);
        if (f.ProductId.HasValue)
            q = q.Where(t => t.Items.Any(i => !i.IsDeleted && i.ProductId == f.ProductId.Value));

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim();
            q = q.Where(t => t.Notes.Contains(s)
                          || t.OutStore!.Name.Contains(s) || t.InStore!.Name.Contains(s)
                          || t.Items.Any(i => !i.IsDeleted && i.Product!.Name.Contains(s)));
        }
        return q;
    }

    public async Task<ServiceResult<PagedResult<StoreTransactionDto>>> GetPagedAsync(StoreTransactionFilterDto f)
    {
        var page = await ApplyFilters(_dbContext.StoreTransaction.Where(t => !t.IsDeleted), f)
            .OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.Id)
            .Select(t => t.Id).ToPagedAsync(f);

        var loaded = await _dbContext.StoreTransaction
            .Where(t => page.Items.Contains(t.Id))
            .Include(t => t.Auditor).Include(t => t.OutStore).Include(t => t.InStore)
            .Include(t => t.Items!.Where(i => !i.IsDeleted)).ThenInclude(i => i.Product)
            .AsNoTracking().ToListAsync();

        return ServiceResult<PagedResult<StoreTransactionDto>>.Ok(new PagedResult<StoreTransactionDto>
        {
            Items = page.Items.Select(id => loaded.First(t => t.Id == id)).Select(ToDto).ToList(),
            Total = page.Total,
            Page = page.Page,
            PageSize = page.PageSize
        });
    }

    public async Task<ServiceResult<StoreTransactionDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<StoreTransactionDto?>.Failed("A valid transaction Id is required");

        var transaction = await _dbContext.StoreTransaction
            .Where(z => !z.IsDeleted && z.Id == id)
            .Include(t => t.Auditor)
            .Include(t => t.OutStore)
            .Include(t => t.InStore)
            .Include(t => t.Items!.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (transaction is null)
            return ServiceResult<StoreTransactionDto?>.NotFound("Transaction not found");

        return ServiceResult<StoreTransactionDto?>.Ok(ToDto(transaction));
    }

    public Task<ServiceResult<StoreTransactionDto>> CreateAsync(StoreTransactionDto dto) =>
    _stock.RunExclusiveAsync(() => CreateCoreAsync(dto));
    private async Task<ServiceResult<StoreTransactionDto>> CreateCoreAsync(StoreTransactionDto dto)
    {
        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
            return ServiceResult<StoreTransactionDto>.Failed(validationError);

        var stockError = await _stock.CheckChangeAsync(new StockExclusion(), Array.Empty<StockDelta>(),
            StockDeltas.Transfer(dto.OutStoreId, dto.InStoreId, dto.TransactionItems.Select(i => (i.ItemId, i.Quantity))));
        if (stockError is not null) return ServiceResult<StoreTransactionDto>.Failed(stockError);

        var transaction = new StoreTransaction
        {
            TransactionDate = dto.TransactionDate,
            AuditorId = dto.AuditorId,
            Notes = dto.Notes,
            OutStoreId = dto.OutStoreId,
            InStoreId = dto.InStoreId,
            Items = dto.TransactionItems.Select(i => new StoreTransactionItem
            {
                ProductId = i.ItemId,
                Quantity = i.Quantity
            }).ToList()
        };

        _dbContext.StoreTransaction.Add(transaction);
        await _dbContext.SaveChangesAsync();

        var created = await _dbContext.StoreTransaction
            .Include(t => t.Auditor).Include(t => t.OutStore).Include(t => t.InStore)
            .Include(t => t.Items!).ThenInclude(i => i.Product)
            .AsNoTracking().FirstAsync(t => t.Id == transaction.Id);

        return ServiceResult<StoreTransactionDto>.Ok(ToDto(created));
    }

    public Task<ServiceResult<bool>> UpdateAsync(long id, StoreTransactionDto dto) =>
    _stock.RunExclusiveAsync(() => UpdateCoreAsync(id, dto));
    private async Task<ServiceResult<bool>> UpdateCoreAsync(long id, StoreTransactionDto dto)
    {
        if (dto is null) return ServiceResult<bool>.Failed("Transaction payload is required");
        if (id != dto.Id) return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.StoreTransaction.Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        if (existing is null) return ServiceResult<bool>.NotFound("Transaction not found");

        var validationError = await ValidateAsync(dto);
        if (validationError is not null) return ServiceResult<bool>.Failed(validationError);

        var stockError = await _stock.CheckChangeAsync(new StockExclusion(StoreTransactionId: id),
            StockDeltas.Transfer(existing.OutStoreId, existing.InStoreId,
                existing.Items.Where(i => !i.IsDeleted).Select(i => (i.ProductId, i.Quantity))),
            StockDeltas.Transfer(dto.OutStoreId, dto.InStoreId, dto.TransactionItems.Select(i => (i.ItemId, i.Quantity))));
        if (stockError is not null) return ServiceResult<bool>.Failed(stockError);

        existing.TransactionDate = dto.TransactionDate;
        existing.AuditorId = dto.AuditorId;
        existing.Notes = dto.Notes;
        existing.OutStoreId = dto.OutStoreId;
        existing.InStoreId = dto.InStoreId;

        if (existing.Items is { Count: > 0 }) _dbContext.StoreTransactionItem.RemoveRange(existing.Items);
        await _dbContext.StoreTransactionItem.AddRangeAsync(dto.TransactionItems.Select(i => new StoreTransactionItem
        {
            TransactionId = existing.Id,
            ProductId = i.ItemId,
            Quantity = i.Quantity
        }));

        await _dbContext.SaveChangesAsync();
        return ServiceResult<bool>.Ok(true);
    }

    public Task<ServiceResult<bool>> DeleteAsync(long id) =>
    _stock.RunExclusiveAsync(() => DeleteCoreAsync(id));
    private async Task<ServiceResult<bool>> DeleteCoreAsync(long id)
    {
        var transaction = await _dbContext.StoreTransaction
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (transaction is null)
            return ServiceResult<bool>.NotFound("Transaction not found");

        var stockError = await _stock.CheckChangeAsync(new StockExclusion(StoreTransactionId: id),
            StockDeltas.Transfer(transaction.OutStoreId, transaction.InStoreId,
                transaction.Items.Where(i => !i.IsDeleted).Select(i => (i.ProductId, i.Quantity))),
            Array.Empty<StockDelta>());
        if (stockError is not null) return ServiceResult<bool>.Failed(stockError);

        var now = DateTime.UtcNow;

        transaction.IsDeleted = true;
        transaction.DeletedAtUtc = now;

        // Soft-delete the child items along with the header
        if (transaction.Items is { Count: > 0 })
        {
            foreach (var item in transaction.Items.Where(i => !i.IsDeleted))
            {
                item.IsDeleted = true;
                item.DeletedAtUtc = now;
            }
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Store transaction with Id {TransactionId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}