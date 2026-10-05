// Services/CustomerPaymentService.cs
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using CMS.Domain.DTOs;
using CMS.Domain.Models;
using CMS.Application.Interfaces.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMS.Application.Services;

public class CustomerPaymentService : ICustomerPaymentService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<CustomerPaymentService> _logger;

    private const int NotesMaxLength = 2000;

    public CustomerPaymentService(
        IAppDbContext dbContext,
        ILogger<CustomerPaymentService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private CustomerPaymentDto ToDto(CustomerPayment p)
    {
        return new CustomerPaymentDto
        {
            Id = p.Id,
            Date = p.Date,
            Type = p.Type,
            CustomerId = p.CustomerId,
            CustomerName = p.Customer?.Name ?? "",
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
            UpdatedBy = p.UpdatedBy ?? "",
            AdvancePaymentOrderId = p.AdvancePaymentOrderId,
        };
    }

    private IQueryable<CustomerPayment> WithIncludes(IQueryable<CustomerPayment> query)
    {
        return query
            .Include(p => p.Customer)
            .Include(p => p.MoneySafe)
            .Include(p => p.Auditor);
    }

    // Centralized validation used by both Create and Update.
    private async Task<string?> ValidateAsync(CustomerPaymentDto dto)
    {
        if (dto is null)
            return "Customer payment payload is required";

        // --- Date ---
        if (dto.Date == default)
            return "Date is required";

        // --- Customer (required FK) ---
        if (dto.CustomerId <= 0)
            return "CustomerId is required";

        var customerExists = await _dbContext.Customer
            .AnyAsync(c => c.Id == dto.CustomerId && !c.IsDeleted);

        if (!customerExists)
            return $"Customer with Id {dto.CustomerId} was not found";

        // --- Invoice (optional FK, must belong to the same customer if provided) ---
        if (dto.InvoiceId.HasValue)
        {
            if (dto.InvoiceId.Value <= 0)
                return "InvoiceId is invalid";

            var invoice = await _dbContext.SaleInvoice
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == dto.InvoiceId.Value && !i.IsDeleted);

            if (invoice is null)
                return $"Invoice with Id {dto.InvoiceId.Value} was not found";

            if (invoice.CustomerId != dto.CustomerId)
                return $"Invoice {dto.InvoiceId.Value} does not belong to customer {dto.CustomerId}";
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

    public async Task<ServiceResult<List<CustomerPaymentDto>>> GetAllAsync()
    {
        var items = await WithIncludes(_dbContext.CustomerPayment.Where(z => !z.IsDeleted))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<CustomerPaymentDto>>.Ok(items.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<CustomerPaymentDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<CustomerPaymentDto?>.Failed("A valid payment Id is required");

        var item = await WithIncludes(_dbContext.CustomerPayment.Where(z => !z.IsDeleted && z.Id == id))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (item is null)
            return ServiceResult<CustomerPaymentDto?>.NotFound("Customer payment not found");

        return ServiceResult<CustomerPaymentDto?>.Ok(ToDto(item));
    }

    public async Task<ServiceResult<CustomerPaymentDto>> CreateAsync(CustomerPaymentDto dto)
    {
        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating customer payment: {Error}", validationError);
            return ServiceResult<CustomerPaymentDto>.Failed(validationError);
        }

        var entity = new CustomerPayment
        {
            Date = dto.Date,
            CustomerId = dto.CustomerId,
            InvoiceId = dto.InvoiceId,
            MoneySafeId = dto.MoneySafeId,
            Amount = dto.Amount,
            AmountCurrency = dto.AmountCurrency,
            Notes = dto.Notes,
            AuditorId = dto.AuditorId
        };

        _dbContext.CustomerPayment.Add(entity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Customer payment created with Id {PaymentId} for Customer {CustomerId}", entity.Id, entity.CustomerId);

        var created = await WithIncludes(_dbContext.CustomerPayment)
            .AsNoTracking()
            .FirstAsync(p => p.Id == entity.Id);

        return ServiceResult<CustomerPaymentDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, CustomerPaymentDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Customer payment payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.CustomerPayment.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Customer payment not found");

        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating customer payment {PaymentId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        existing.Date = dto.Date;
        existing.CustomerId = dto.CustomerId;
        existing.InvoiceId = dto.InvoiceId;
        existing.MoneySafeId = dto.MoneySafeId;
        existing.Amount = dto.Amount;
        existing.AmountCurrency = dto.AmountCurrency;
        existing.Notes = dto.Notes;
        existing.AuditorId = dto.AuditorId;
        existing.Type = dto.Type;
        existing.AdvancePaymentOrderId = dto.AdvancePaymentOrderId;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating customer payment with Id {PaymentId}", id);
            throw;
        }

        _logger.LogInformation("Customer payment with Id {PaymentId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var entity = await _dbContext.CustomerPayment.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (entity is null)
            return ServiceResult<bool>.NotFound("Customer payment not found");

        entity.IsDeleted = true;
        entity.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Customer payment with Id {PaymentId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}