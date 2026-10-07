// Services/SaleInvoiceService.cs
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using CMS.Domain.DTOs;
using CMS.Domain.Enums;
using CMS.Domain.Models;
using CMS.Application.Interfaces.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMS.Application.Services;

public class SaleInvoiceService : ISaleInvoiceService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<SaleInvoiceService> _logger;

    private const int NotesMaxLength = 2000;
    private const int ProductNotesMaxLength = 500;

    public SaleInvoiceService(
        IAppDbContext dbContext,
        ILogger<SaleInvoiceService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private SaleInvoiceDto ToDto(SaleInvoice invoice)
    {
        var paidAmount = invoice.Payments?.Where(p => !p.IsDeleted).Sum(p => p.Amount) ?? 0;

        return new SaleInvoiceDto
        {
            Id = invoice.Id,
            Number = invoice.Number,
            Type = invoice.Type,
            Date = invoice.Date,
            Notes = invoice.Notes,
            CustomerId = invoice.CustomerId,
            CustomerName = invoice.Customer?.Name ?? "",
            AuditorId = invoice.AuditorId,
            AuditorName = invoice.Auditor?.Name ?? "",
            OrderId = invoice.OrderId,
            OriginalInvoiceId = invoice.OriginalInvoiceId,
            OriginalInvoiceNumber = invoice.OriginalInvoice?.Number,
            Total = invoice.Total,
            Discount = invoice.Discount,
            NetTotal = invoice.NetTotal,
            NetTotalCurrency = invoice.NetTotalCurrency,
            PaidAmount = paidAmount,
            RemainingAmount = invoice.NetTotal - paidAmount,
            ShiftId = invoice.ShiftId,
            Items = invoice.Items?.Where(i => !i.IsDeleted).Select(i => new SaleInvoiceItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? "",
                StoreId = i.StoreId,
                StoreName = i.Store?.Name ?? "",
                ProjectId = i.ProjectId,
                ProjectName = i.Project?.Name ?? "",
                ProductNotes = i.ProductNotes,
                PriceType = i.PriceType,
                ProductPrice = i.ProductPrice,
                ProductQuantity = i.ProductQuantity,
                ProductTotal = i.ProductTotal,
                ProductDiscount = i.ProductDiscount,
                ProductNetTotal = i.ProductNetTotal
            }).ToList() ?? new List<SaleInvoiceItemDto>(),
            Payments = invoice.Payments?.Where(z => !z.IsDeleted).Select(z => new SaleInvoicePaymentDto
            {
                Id = z.Id,
                PaymentDate = z.Date,
                Amount = z.Amount,
                AmountCurrency = z.AmountCurrency,
                AuditorId = z.AuditorId,
                AuditorName = z.Auditor?.Name ?? "",
                Notes = z.Notes,
                MoneySafeId = z.MoneySafeId,
                CreatedAtUtc = z.CreatedAtUtc,
                CreatedBy = z.CreatedBy ?? "",
                UpdatedAtUtc = z.UpdatedAtUtc,
                UpdatedBy = z.UpdatedBy ?? ""
            }).ToList() ?? new List<SaleInvoicePaymentDto>(),
            CreatedAtUtc = invoice.CreatedAtUtc,
            CreatedBy = invoice.CreatedBy ?? "",
            UpdatedAtUtc = invoice.UpdatedAtUtc,
            UpdatedBy = invoice.UpdatedBy ?? ""
        };
    }

    private IQueryable<SaleInvoice> WithIncludes(IQueryable<SaleInvoice> query)
    {
        return query
            .Include(i => i.Customer)
            .Include(i => i.Auditor)
            .Include(i => i.OriginalInvoice)
            .Include(i => i.Payments!.Where(p => !p.IsDeleted))
            .Include(i => i.Items!.Where(x => !x.IsDeleted))
                .ThenInclude(x => x.Product)
            .Include(i => i.Items!.Where(x => !x.IsDeleted))
                .ThenInclude(x => x.Store)
            .Include(i => i.Items!.Where(x => !x.IsDeleted))
                .ThenInclude(x => x.Project);
    }

    // Recalculates each item's ProductTotal/ProductNetTotal, then the invoice-level
    // Total/Discount/NetTotal as the sum of the item values. NetTotalCurrency is left
    // as supplied — it's an FX conversion of NetTotal, not derivable from the items.
    private void RecalculateTotals(SaleInvoiceDto dto)
    {
        foreach (var item in dto.Items)
        {
            item.ProductTotal = Math.Round(item.ProductPrice * item.ProductQuantity, 2);
            item.ProductNetTotal = Math.Round(item.ProductTotal - item.ProductDiscount, 2);
        }

        dto.Total = Math.Round(dto.Items.Sum(i => i.ProductTotal), 2);
        dto.Discount = Math.Round(dto.Items.Sum(i => i.ProductDiscount), 2);
        dto.NetTotal = Math.Round(dto.Total - dto.Discount, 2);
    }

    // Centralized validation used by both Create and Update.
    private async Task<string?> ValidateAsync(SaleInvoiceDto dto, bool isUpdate, long? currentId = null)
    {
        if (dto is null)
            return "Invoice payload is required";

        // --- Number (required + unique) ---
        if (dto.Number <= 0)
            return "Number is required and must be greater than zero";

        var numberQuery = _dbContext.SaleInvoice.Where(i => !i.IsDeleted && i.Number == dto.Number);

        if (isUpdate && currentId.HasValue)
            numberQuery = numberQuery.Where(i => i.Id != currentId.Value);

        if (await numberQuery.AnyAsync())
            return $"An invoice with number {dto.Number} already exists";

        // --- Type ---
        if (!Enum.IsDefined(typeof(InvoiceType), dto.Type))
            return "Type is not a valid value";

        // --- Date ---
        if (dto.Date == default)
            return "Date is required";

        // --- Notes (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Length > NotesMaxLength)
            return $"Notes cannot exceed {NotesMaxLength} characters";

        // --- Customer (required FK) ---
        if (dto.CustomerId <= 0)
            return "CustomerId is required";

        var customerExists = await _dbContext.Customer
            .AnyAsync(c => c.Id == dto.CustomerId && !c.IsDeleted);

        if (!customerExists)
            return $"Customer with Id {dto.CustomerId} was not found";

        // --- Auditor (required FK) ---
        if (dto.AuditorId <= 0)
            return "AuditorId is required";

        var auditorExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == dto.AuditorId && !e.IsDeleted);

        if (!auditorExists)
            return $"Auditor (Employee) with Id {dto.AuditorId} was not found";

        // --- Order (optional FK) ---
        if (dto.OrderId.HasValue)
        {
            if (dto.OrderId.Value <= 0)
                return "OrderId is invalid";

            var orderExists = await _dbContext.Order
                .AnyAsync(o => o.Id == dto.OrderId.Value && !o.IsDeleted);

            if (!orderExists)
                return $"Order with Id {dto.OrderId.Value} was not found";
        }

        // --- OriginalInvoice (optional FK, must be a different invoice from the same customer) ---
        if (dto.OriginalInvoiceId.HasValue)
        {
            if (dto.OriginalInvoiceId.Value <= 0)
                return "OriginalInvoiceId is invalid";

            if (isUpdate && currentId.HasValue && dto.OriginalInvoiceId.Value == currentId.Value)
                return "An invoice cannot reference itself as the original invoice";

            var original = await _dbContext.SaleInvoice
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == dto.OriginalInvoiceId.Value && !i.IsDeleted);

            if (original is null)
                return $"Original invoice with Id {dto.OriginalInvoiceId.Value} was not found";

            if (original.CustomerId != dto.CustomerId)
                return $"Original invoice {dto.OriginalInvoiceId.Value} does not belong to customer {dto.CustomerId}";
        }

        // --- NetTotalCurrency ---
        if (dto.NetTotalCurrency < 0)
            return "NetTotalCurrency cannot be negative";

        // --- Items (at least one required) ---
        if (dto.Items is null || dto.Items.Count == 0)
            return "At least one invoice item is required";

        foreach (var item in dto.Items)
        {
            if (item.ProductId <= 0)
                return "Each invoice item must have a valid ProductId";

            var productExists = await _dbContext.Product
                .AnyAsync(p => p.Id == item.ProductId && !p.IsDeleted);

            if (!productExists)
                return $"Product with Id {item.ProductId} was not found";

            if (item.StoreId <= 0)
                return "Each invoice item must have a valid StoreId";

            var storeExists = await _dbContext.Store
                .AnyAsync(s => s.Id == item.StoreId && !s.IsDeleted);

            if (!storeExists)
                return $"Store with Id {item.StoreId} was not found";

            if (item.ProjectId.HasValue)
            {
                if (item.ProjectId.Value <= 0)
                    return $"ProjectId for product Id {item.ProductId} is invalid";

                var projectExists = await _dbContext.Project
                    .AnyAsync(p => p.Id == item.ProjectId.Value);

                if (!projectExists)
                    return $"Project with Id {item.ProjectId.Value} was not found";
            }

            if (!string.IsNullOrWhiteSpace(item.ProductNotes) && item.ProductNotes.Length > ProductNotesMaxLength)
                return $"ProductNotes for product Id {item.ProductId} cannot exceed {ProductNotesMaxLength} characters";

            if (!Enum.IsDefined(typeof(PriceType), item.PriceType))
                return $"PriceType for product Id {item.ProductId} is not a valid value";

            if (item.ProductPrice < 0)
                return $"ProductPrice for product Id {item.ProductId} cannot be negative";

            if (item.ProductQuantity <= 0)
                return $"ProductQuantity for product Id {item.ProductId} must be greater than zero";

            if (item.ProductDiscount < 0)
                return $"ProductDiscount for product Id {item.ProductId} cannot be negative";

            var lineTotal = Math.Round(item.ProductPrice * item.ProductQuantity, 2);
            if (item.ProductDiscount > lineTotal)
                return $"ProductDiscount for product Id {item.ProductId} cannot exceed the line total ({lineTotal})";
        }

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<SaleInvoiceDto>>> GetAllAsync()
    {
        var invoices = await WithIncludes(_dbContext.SaleInvoice.Where(z => !z.IsDeleted))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<SaleInvoiceDto>>.Ok(invoices.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<SaleInvoiceDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<SaleInvoiceDto?>.Failed("A valid invoice Id is required");

        var invoice = await WithIncludes(_dbContext.SaleInvoice.Where(z => !z.IsDeleted && z.Id == id))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (invoice is null)
            return ServiceResult<SaleInvoiceDto?>.NotFound("Invoice not found");

        return ServiceResult<SaleInvoiceDto?>.Ok(ToDto(invoice));
    }

    public async Task<ServiceResult<SaleInvoiceDto>> CreateAsync(SaleInvoiceDto dto)
    {
        var validationError = await ValidateAsync(dto, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating sale invoice: {Error}", validationError);
            return ServiceResult<SaleInvoiceDto>.Failed(validationError);
        }

        RecalculateTotals(dto);

        var invoice = new SaleInvoice
        {
            Number = dto.Number,
            Type = dto.Type,
            Date = dto.Date,
            Notes = dto.Notes,
            CustomerId = dto.CustomerId,
            AuditorId = dto.AuditorId,
            OrderId = dto.OrderId,
            OriginalInvoiceId = dto.OriginalInvoiceId,
            Total = dto.Total,
            Discount = dto.Discount,
            NetTotal = dto.NetTotal,
            NetTotalCurrency = dto.NetTotalCurrency,
            ShiftId = dto.ShiftId,
            Items = dto.Items.Select(i => new SaleInvoiceItem
            {
                ProductId = i.ProductId,
                StoreId = i.StoreId,
                ProjectId = i.ProjectId,
                ProductNotes = i.ProductNotes,
                PriceType = i.PriceType,
                ProductPrice = i.ProductPrice,
                ProductQuantity = i.ProductQuantity,
                ProductTotal = i.ProductTotal,
                ProductDiscount = i.ProductDiscount,
                ProductNetTotal = i.ProductNetTotal
            }).ToList()
        };

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

            _dbContext.SaleInvoice.Add(invoice);
            await _dbContext.SaveChangesAsync();

            await dbTransaction.CommitAsync();
        });

        _logger.LogInformation("Sale invoice created with Id {InvoiceId} (Number {Number}, {ItemCount} items)",
            invoice.Id, invoice.Number, invoice.Items.Count);

        var created = await WithIncludes(_dbContext.SaleInvoice)
            .AsNoTracking()
            .FirstAsync(i => i.Id == invoice.Id);

        return ServiceResult<SaleInvoiceDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, SaleInvoiceDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Invoice payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.SaleInvoice
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

        if (existing is null)
            return ServiceResult<bool>.NotFound("Invoice not found");

        var hasPayments = existing.Payments.Any(p => !p.IsDeleted);
        if (hasPayments)
            return ServiceResult<bool>.Failed("This invoice has recorded payments and can no longer be edited");

        var validationError = await ValidateAsync(dto, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating sale invoice {InvoiceId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        RecalculateTotals(dto);

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

            existing.Number = dto.Number;
            existing.Type = dto.Type;
            existing.Date = dto.Date;
            existing.Notes = dto.Notes;
            existing.CustomerId = dto.CustomerId;
            existing.AuditorId = dto.AuditorId;
            existing.OrderId = dto.OrderId;
            existing.OriginalInvoiceId = dto.OriginalInvoiceId;
            existing.Total = dto.Total;
            existing.Discount = dto.Discount;
            existing.NetTotal = dto.NetTotal;
            existing.NetTotalCurrency = dto.NetTotalCurrency;
            existing.ShiftId = dto.ShiftId;

            if (existing.Items is { Count: > 0 })
            {
                _dbContext.SaleInvoiceItem.RemoveRange(existing.Items);
            }

            var newItems = dto.Items.Select(i => new SaleInvoiceItem
            {
                InvoiceId = existing.Id,
                ProductId = i.ProductId,
                StoreId = i.StoreId,
                ProjectId = i.ProjectId,
                ProductNotes = i.ProductNotes,
                PriceType = i.PriceType,
                ProductPrice = i.ProductPrice,
                ProductQuantity = i.ProductQuantity,
                ProductTotal = i.ProductTotal,
                ProductDiscount = i.ProductDiscount,
                ProductNetTotal = i.ProductNetTotal
            }).ToList();

            await _dbContext.SaleInvoiceItem.AddRangeAsync(newItems);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error occurred while updating sale invoice with Id {InvoiceId}", id);
                throw;
            }

            await dbTransaction.CommitAsync();
        });

        _logger.LogInformation("Sale invoice with Id {InvoiceId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var invoice = await _dbContext.SaleInvoice
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

        if (invoice is null)
            return ServiceResult<bool>.NotFound("Invoice not found");

        var hasPayments = invoice.Payments.Any(p => !p.IsDeleted);
        if (hasPayments)
            return ServiceResult<bool>.Failed("This invoice has recorded payments and cannot be deleted");

        var now = DateTime.UtcNow;

        invoice.IsDeleted = true;
        invoice.DeletedAtUtc = now;

        foreach (var item in invoice.Items.Where(i => !i.IsDeleted))
        {
            item.IsDeleted = true;
            item.DeletedAtUtc = now;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Sale invoice with Id {InvoiceId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}