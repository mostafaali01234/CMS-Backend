// Services/StoreTransactionService.cs
using CMS.Api.Data;
using CMS.Domain.Models;
using CMS.Api.Models.DTOs;
using CMS.Api.Models.DTOs.Responses;
using CMS.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CMS.Api.Services;

public class StoreTransactionService : IStoreTransactionService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<StoreTransactionService> _logger;

    private const int NotesMaxLength = 2000;

    public StoreTransactionService(
        AppDbContext dbContext,
        ILogger<StoreTransactionService> logger)
    {
        _dbContext = dbContext;
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

    public async Task<ServiceResult<StoreTransactionDto>> CreateAsync(StoreTransactionDto dto)
    {
        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating store transaction: {Error}", validationError);
            return ServiceResult<StoreTransactionDto>.Failed(validationError);
        }

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

        // Using an execution strategy + explicit transaction so the header and all
        // item rows are inserted atomically (either all succeed or none do).
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

            _dbContext.StoreTransaction.Add(transaction);
            await _dbContext.SaveChangesAsync();

            await dbTransaction.CommitAsync();
        });

        _logger.LogInformation("Store transaction created with Id {TransactionId} ({ItemCount} items)",
            transaction.Id, transaction.Items.Count);

        var created = await _dbContext.StoreTransaction
            .Include(t => t.Auditor)
            .Include(t => t.OutStore)
            .Include(t => t.InStore)
            .Include(t => t.Items!)
                .ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstAsync(t => t.Id == transaction.Id);

        return ServiceResult<StoreTransactionDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, StoreTransactionDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Transaction payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.StoreTransaction
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (existing is null)
            return ServiceResult<bool>.NotFound("Transaction not found");

        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating store transaction {TransactionId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

            // Update header fields
            existing.TransactionDate = dto.TransactionDate;
            existing.AuditorId = dto.AuditorId;
            existing.Notes = dto.Notes;
            existing.OutStoreId = dto.OutStoreId;
            existing.InStoreId = dto.InStoreId;

            // Replace all line items: remove existing, add the new set
            if (existing.Items is { Count: > 0 })
            {
                _dbContext.StoreTransactionItem.RemoveRange(existing.Items);
            }

            var newItems = dto.TransactionItems.Select(i => new StoreTransactionItem
            {
                TransactionId = existing.Id,
                ProductId = i.ItemId,
                Quantity = i.Quantity
            }).ToList();

            await _dbContext.StoreTransactionItem.AddRangeAsync(newItems);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error occurred while updating store transaction with Id {TransactionId}", id);
                throw;
            }

            await dbTransaction.CommitAsync();
        });

        _logger.LogInformation("Store transaction with Id {TransactionId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var transaction = await _dbContext.StoreTransaction
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (transaction is null)
            return ServiceResult<bool>.NotFound("Transaction not found");

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