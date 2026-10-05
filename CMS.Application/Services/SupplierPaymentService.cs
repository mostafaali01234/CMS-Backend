// Services/SupplierPaymentService.cs
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using CMS.Domain.DTOs;
using CMS.Domain.Models;
using CMS.Application.Interfaces.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMS.Application.Services;

public class SupplierPaymentService : ISupplierPaymentService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<SupplierPaymentService> _logger;

    private const int NotesMaxLength = 2000;

    public SupplierPaymentService(
        IAppDbContext dbContext,
        ILogger<SupplierPaymentService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private SupplierPaymentDto ToDto(SupplierPayment p)
    {
        return new SupplierPaymentDto
        {
            Id = p.Id,
            Date = p.Date,
            Type = p.Type,
            SupplierId = p.SupplierId,
            SupplierName = p.Supplier?.Name ?? "",
            InvoiceId = p.InvoiceId,
            MoneySafeId = p.MoneySafeId,
            MoneySafeName = p.MoneySafe?.Name ?? "",
            Amount = p.Amount,
            AmountCurrency = p.AmountCurrency,
            Notes = p.Notes,
            AuditorId = p.AuditorId,
            AuditorName = p.Auditor?.Name ?? "",
            CreatedAtUtc = p.CreatedAtUtc,
            CreatedBy = p.CreatedBy ?? "",
            UpdatedAtUtc = p.UpdatedAtUtc,
            UpdatedBy = p.UpdatedBy ?? ""
        };
    }

    private IQueryable<SupplierPayment> WithIncludes(IQueryable<SupplierPayment> query)
    {
        return query
            .Include(p => p.Supplier)
            .Include(p => p.MoneySafe)
            .Include(p => p.Auditor);
    }

    // Centralized validation used by both Create and Update.
    private async Task<string?> ValidateAsync(SupplierPaymentDto dto)
    {
        if (dto is null)
            return "Supplier payment payload is required";

        // --- Date ---
        if (dto.Date == default)
            return "Date is required";

        // --- Supplier (required FK) ---
        if (dto.SupplierId <= 0)
            return "SupplierId is required";

        var supplierExists = await _dbContext.Supplier
            .AnyAsync(s => s.Id == dto.SupplierId && !s.IsDeleted);

        if (!supplierExists)
            return $"Supplier with Id {dto.SupplierId} was not found";

        // --- Invoice (optional FK, must belong to the same supplier if provided) ---
        if (dto.InvoiceId.HasValue)
        {
            if (dto.InvoiceId.Value <= 0)
                return "InvoiceId is invalid";

            var invoice = await _dbContext.BuyInvoice
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == dto.InvoiceId.Value && !i.IsDeleted);

            if (invoice is null)
                return $"Invoice with Id {dto.InvoiceId.Value} was not found";

            if (invoice.SupplierId != dto.SupplierId)
                return $"Invoice {dto.InvoiceId.Value} does not belong to supplier {dto.SupplierId}";
        }

        // --- MoneySafe (required FK, must be active) ---
        if (dto.MoneySafeId <= 0)
            return "MoneySafeId is required";

        var safe = await _dbContext.MoneySafe
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == dto.MoneySafeId && !m.IsDeleted);

        if (safe is null)
            return $"Money safe with Id {dto.MoneySafeId} was not found";

        if (!safe.Active)
            return $"Money safe '{safe.Name}' is not active";

        // --- Amount ---
        if (dto.Amount <= 0)
            return "Amount must be greater than zero";

        if (dto.AmountCurrency < 0)
            return "AmountCurrency cannot be negative";

        // --- Notes (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Length > NotesMaxLength)
            return $"Notes cannot exceed {NotesMaxLength} characters";

        // --- Auditor (required FK) ---
        if (dto.AuditorId <= 0)
            return "AuditorId is required";

        var auditorExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == dto.AuditorId && !e.IsDeleted);

        if (!auditorExists)
            return $"Auditor (Employee) with Id {dto.AuditorId} was not found";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<SupplierPaymentDto>>> GetAllAsync()
    {
        var items = await WithIncludes(_dbContext.SupplierPayment.Where(z => !z.IsDeleted))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<SupplierPaymentDto>>.Ok(items.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<SupplierPaymentDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<SupplierPaymentDto?>.Failed("A valid payment Id is required");

        var item = await WithIncludes(_dbContext.SupplierPayment.Where(z => !z.IsDeleted && z.Id == id))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (item is null)
            return ServiceResult<SupplierPaymentDto?>.NotFound("Supplier payment not found");

        return ServiceResult<SupplierPaymentDto?>.Ok(ToDto(item));
    }

    public async Task<ServiceResult<SupplierPaymentDto>> CreateAsync(SupplierPaymentDto dto)
    {
        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating supplier payment: {Error}", validationError);
            return ServiceResult<SupplierPaymentDto>.Failed(validationError);
        }

        var entity = new SupplierPayment
        {
            Date = dto.Date,
            SupplierId = dto.SupplierId,
            InvoiceId = dto.InvoiceId,
            MoneySafeId = dto.MoneySafeId,
            Amount = dto.Amount,
            AmountCurrency = dto.AmountCurrency,
            Notes = dto.Notes,
            AuditorId = dto.AuditorId
        };

        _dbContext.SupplierPayment.Add(entity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Supplier payment created with Id {PaymentId} for Supplier {SupplierId}", entity.Id, entity.SupplierId);

        var created = await WithIncludes(_dbContext.SupplierPayment)
            .AsNoTracking()
            .FirstAsync(p => p.Id == entity.Id);

        return ServiceResult<SupplierPaymentDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, SupplierPaymentDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Supplier payment payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.SupplierPayment.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Supplier payment not found");

        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating supplier payment {PaymentId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        existing.Date = dto.Date;
        existing.SupplierId = dto.SupplierId;
        existing.InvoiceId = dto.InvoiceId;
        existing.MoneySafeId = dto.MoneySafeId;
        existing.Amount = dto.Amount;
        existing.AmountCurrency = dto.AmountCurrency;
        existing.Notes = dto.Notes;
        existing.AuditorId = dto.AuditorId;
        existing.Type = dto.Type;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating supplier payment with Id {PaymentId}", id);
            throw;
        }

        _logger.LogInformation("Supplier payment with Id {PaymentId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var entity = await _dbContext.SupplierPayment.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (entity is null)
            return ServiceResult<bool>.NotFound("Supplier payment not found");

        entity.IsDeleted = true;
        entity.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Supplier payment with Id {PaymentId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}