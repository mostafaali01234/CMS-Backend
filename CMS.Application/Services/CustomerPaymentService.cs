// Services/CustomerPaymentService.cs
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;
using CMS.Application.Interfaces;
using CMS.Application.Interfaces.Configuration;
using CMS.Domain.DTOs;
using CMS.Domain.Enums;
using CMS.Domain.Models;
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
            InvoiceNumber = p.Invoice?.Number,
            InvoiceOrderId = p.Invoice?.OrderId ?? p.AdvancePaymentOrderId
        };
    }

    private IQueryable<CustomerPayment> WithIncludes(IQueryable<CustomerPayment> query)
    {
        return query
            .Include(p => p.Customer)
            .Include(p => p.MoneySafe)
            .Include(p => p.Invoice)
            .Include(p => p.Auditor);
    }

    private async Task<(decimal Egp, decimal Cur)> RefundableAsync(long returnId, long? excludePaymentId = null)
    {
        var ret = await _dbContext.SaleInvoice.Include(i => i.Payments).AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == returnId && !i.IsDeleted && i.Type == InvoiceType.Sale_Return);
        if (ret?.OriginalInvoiceId is null) return (0, 0);

        var orig = await _dbContext.SaleInvoice.AsNoTracking()
            .Include(i => i.Payments)
            .Include(i => i.Returns).ThenInclude(r => r.Payments)
            .FirstAsync(i => i.Id == ret.OriginalInvoiceId);

        bool Refund(CustomerPayment p) =>
            !p.IsDeleted && p.Type == PaymentType.Refund_Payment && p.Id != (excludePaymentId ?? 0);

        var returns = orig.Returns.Where(r => !r.IsDeleted).ToList();
        var paid = SaleBalances.Paid(orig.Payments);
        var returned = SaleBalances.Returned(returns);
        var refundedAll = returns.SelectMany(r => r.Payments).Where(Refund).ToList();
        var refundedOwn = ret.Payments.Where(Refund).ToList();

        var overEgp = Math.Max(0, paid.Egp - Math.Max(0, orig.NetTotal - returned.Egp)) - refundedAll.Sum(p => p.Amount);
        var overCur = Math.Max(0, paid.Cur - Math.Max(0, orig.NetTotalCurrency - returned.Cur)) - refundedAll.Sum(p => p.AmountCurrency);

        return (Math.Max(0, Math.Min(ret.NetTotal - refundedOwn.Sum(p => p.Amount), overEgp)),
                Math.Max(0, Math.Min(ret.NetTotalCurrency - refundedOwn.Sum(p => p.AmountCurrency), overCur)));
    }

    public async Task<ServiceResult<RefundableDto>> GetRefundableAsync(long returnInvoiceId)
    {
        var (egp, cur) = await RefundableAsync(returnInvoiceId);
        return ServiceResult<RefundableDto>.Ok(new RefundableDto { Amount = egp, AmountCurrency = cur });
    }

    // Centralized validation used by both Create and Update.
    private async Task<string?> ValidateAsync(CustomerPaymentDto dto, long? currentId = null)
    {
        if (dto is null) return "Customer payment payload is required";
        if (dto.Date == default) return "Date is required";
        if (!Enum.IsDefined(typeof(PaymentType), dto.Type)) return "Type is not a valid value";

        if (dto.CustomerId <= 0) return "CustomerId is required";
        if (!await _dbContext.Customer.AnyAsync(c => c.Id == dto.CustomerId && !c.IsDeleted))
            return $"Customer with Id {dto.CustomerId} was not found";

        if (dto.MoneySafeId <= 0) return "MoneySafeId is required";
        var safe = await _dbContext.MoneySafe.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == dto.MoneySafeId && !m.IsDeleted);
        if (safe is null) return $"Money safe with Id {dto.MoneySafeId} was not found";
        if (!safe.Active) return $"Money safe '{safe.Name}' is not active";

        if (dto.Amount < 0) return "Amount cannot be negative";
        if (dto.AmountCurrency < 0) return "AmountCurrency cannot be negative";
        if (dto.Amount == 0 && dto.AmountCurrency == 0) return "Amount or AmountCurrency must be greater than zero";
        if (dto.AmountCurrency > 0 && !safe.IsMultiCurrency)
            return $"Money safe '{safe.Name}' does not accept currency amounts";

        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Length > NotesMaxLength)
            return $"Notes cannot exceed {NotesMaxLength} characters";

        if (dto.AuditorId <= 0) return "AuditorId is required";
        if (!await _dbContext.Employee.AnyAsync(e => e.Id == dto.AuditorId && !e.IsDeleted))
            return $"Auditor (Employee) with Id {dto.AuditorId} was not found";

        switch (dto.Type)
        {
            case PaymentType.Regular_Payment:
                {
                    if (dto.AdvancePaymentOrderId.HasValue) return "A regular payment cannot have an advance order";
                    if (!dto.InvoiceId.HasValue) break;   // goes to the customer balance

                    var invoice = await _dbContext.SaleInvoice
                        .Include(i => i.Payments).Include(i => i.Returns)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(i => i.Id == dto.InvoiceId.Value && !i.IsDeleted);
                    if (invoice is null) return $"Invoice with Id {dto.InvoiceId.Value} was not found";
                    if (invoice.CustomerId != dto.CustomerId) return $"Invoice {invoice.Id} does not belong to customer {dto.CustomerId}";
                    if (invoice.Type != InvoiceType.Sale) return "Regular payments can only be recorded on Sale invoices";

                    invoice.Payments = invoice.Payments.Where(p => p.Id != (currentId ?? 0)).ToList();
                    var (rem, remCur) = SaleBalances.Remaining(invoice);
                    if (dto.Amount > rem) return $"Amount exceeds the remaining amount ({rem}) of invoice {invoice.Number}";
                    if (dto.AmountCurrency > remCur) return $"Currency amount exceeds the remaining currency amount ({remCur}) of invoice {invoice.Number}";
                    break;
                }

            case PaymentType.Advance_Payment:
                {
                    if (dto.InvoiceId.HasValue) return "An advance payment cannot be tied to an invoice";
                    if (!dto.AdvancePaymentOrderId.HasValue) return "AdvancePaymentOrderId is required";

                    var order = await _dbContext.Order.AsNoTracking()
                        .FirstOrDefaultAsync(o => o.Id == dto.AdvancePaymentOrderId.Value && !o.IsDeleted);
                    if (order is null) return $"Order with Id {dto.AdvancePaymentOrderId.Value} was not found";
                    if (order.CustomerId != dto.CustomerId) return $"Order {order.Id} does not belong to customer {dto.CustomerId}";

                    var hasInvoice = await _dbContext.SaleInvoice.AnyAsync(i =>
                        !i.IsDeleted && i.Type == InvoiceType.Sale && i.OrderId == order.Id);
                    if (hasInvoice) return "This order already has a sale invoice. Record a regular payment on it instead";
                    break;
                }

            case PaymentType.Refund_Payment:
                {
                    if (dto.AdvancePaymentOrderId.HasValue) return "A refund cannot have an advance order";
                    if (!dto.InvoiceId.HasValue) return "A refund must be tied to a Sale_Return invoice";

                    var ret = await _dbContext.SaleInvoice.AsNoTracking()
                        .FirstOrDefaultAsync(i => i.Id == dto.InvoiceId.Value && !i.IsDeleted);
                    if (ret is null) return $"Invoice with Id {dto.InvoiceId.Value} was not found";
                    if (ret.CustomerId != dto.CustomerId) return $"Invoice {ret.Id} does not belong to customer {dto.CustomerId}";
                    if (ret.Type != InvoiceType.Sale_Return) return "Refunds can only be recorded on Sale_Return invoices";

                    var (cap, capCur) = await RefundableAsync(ret.Id, currentId);
                    if (dto.Amount > cap) return $"Refund exceeds the refundable amount ({cap})";
                    if (dto.AmountCurrency > capCur) return $"Currency refund exceeds the refundable currency amount ({capCur})";
                    break;
                }
        }
        return null;
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
            AuditorId = dto.AuditorId,
            Type = dto.Type,
            AdvancePaymentOrderId = dto.Type == PaymentType.Advance_Payment ? dto.AdvancePaymentOrderId : null,
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
        if (dto.Type != existing.Type)
            return ServiceResult<bool>.Failed("The payment type cannot be changed");
        if (existing.Type == PaymentType.Advance_Payment && existing.InvoiceId != null)
            return ServiceResult<bool>.Failed("This advance payment is attached to an invoice. Delete it and enter it again");


        var validationError = await ValidateAsync(dto, id);
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
        existing.AdvancePaymentOrderId = dto.Type == PaymentType.Advance_Payment ? dto.AdvancePaymentOrderId : null;

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

    public async Task<ServiceResult<List<CustomerPaymentDto>>> CreateAllocatedAsync(CustomerPaymentAllocationRequest req)
    {
        if (req is null) return ServiceResult<List<CustomerPaymentDto>>.Failed("Payment payload is required");
        req.Allocations ??= new();

        var headerError = await ValidateAsync(new CustomerPaymentDto
        {
            Date = req.Date,
            Type = PaymentType.Regular_Payment,
            CustomerId = req.CustomerId,
            MoneySafeId = req.MoneySafeId,
            Amount = req.Amount,
            AmountCurrency = req.AmountCurrency,
            Notes = req.Notes,
            AuditorId = req.AuditorId
        });
        if (headerError is not null) return ServiceResult<List<CustomerPaymentDto>>.Failed(headerError);

        if (req.Allocations.Any(a => a.Amount < 0 || a.AmountCurrency < 0 || (a.Amount == 0 && a.AmountCurrency == 0)))
            return ServiceResult<List<CustomerPaymentDto>>.Failed("Each allocation needs an amount or a currency amount");

        var invoiceIds = req.Allocations.Select(a => a.InvoiceId).ToList();
        if (invoiceIds.Distinct().Count() != invoiceIds.Count)
            return ServiceResult<List<CustomerPaymentDto>>.Failed("An invoice can appear only once");

        var allocated = req.Allocations.Sum(a => a.Amount);
        var allocatedCur = req.Allocations.Sum(a => a.AmountCurrency);
        if (allocated > req.Amount || allocatedCur > req.AmountCurrency)
            return ServiceResult<List<CustomerPaymentDto>>.Failed("Allocated amounts cannot exceed the payment amounts");

        var invoices = await _dbContext.SaleInvoice
            .Where(i => invoiceIds.Contains(i.Id) && !i.IsDeleted && i.CustomerId == req.CustomerId && i.Type == InvoiceType.Sale)
            .Include(i => i.Payments).Include(i => i.Returns)
            .AsNoTracking().ToListAsync();
        if (invoices.Count != invoiceIds.Count)
            return ServiceResult<List<CustomerPaymentDto>>.Failed("One or more invoices were not found for this customer");

        foreach (var a in req.Allocations)
        {
            var inv = invoices.First(i => i.Id == a.InvoiceId);
            var (rem, remCur) = SaleBalances.Remaining(inv);
            if (a.Amount > rem)
                return ServiceResult<List<CustomerPaymentDto>>.Failed($"Allocation for invoice {inv.Number} exceeds its remaining amount ({rem})");
            if (a.AmountCurrency > remCur)
                return ServiceResult<List<CustomerPaymentDto>>.Failed($"Currency allocation for invoice {inv.Number} exceeds its remaining currency amount ({remCur})");
        }

        var rows = req.Allocations.Select(a => (InvoiceId: (long?)a.InvoiceId, a.Amount, Cur: a.AmountCurrency)).ToList();
        var rest = req.Amount - allocated;
        var restCur = req.AmountCurrency - allocatedCur;
        if (rest > 0 || restCur > 0) rows.Add((null, rest, restCur));   // the customer balance

        var entities = rows.Select(r => new CustomerPayment
        {
            Date = req.Date,
            CustomerId = req.CustomerId,
            Type = PaymentType.Regular_Payment,
            InvoiceId = r.InvoiceId,
            MoneySafeId = req.MoneySafeId,
            Amount = r.Amount,
            AmountCurrency = r.Cur,
            Notes = req.Notes ?? "",
            AuditorId = req.AuditorId
        }).ToList();

        _dbContext.CustomerPayment.AddRange(entities);
        await _dbContext.SaveChangesAsync();

        var ids = entities.Select(e => e.Id).ToList();
        var created = await WithIncludes(_dbContext.CustomerPayment.Where(p => ids.Contains(p.Id))).AsNoTracking().ToListAsync();
        return ServiceResult<List<CustomerPaymentDto>>.Ok(created.Select(ToDto).ToList());
    }
}