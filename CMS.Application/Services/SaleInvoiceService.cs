// Services/SaleInvoiceService.cs
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;
using CMS.Application.Interfaces;
using CMS.Application.Interfaces.Configuration;
using CMS.Domain.DTOs;
using CMS.Domain.Enums;
using CMS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

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
        var payments = invoice.Payments?.Where(p => !p.IsDeleted).ToList() ?? new List<CustomerPayment>();
        var paid = SaleBalances.Paid(payments);
        var refunded = SaleBalances.Refunded(payments);
        var returned = SaleBalances.Returned(invoice.Returns);
        var isSale = invoice.Type == InvoiceType.Sale;

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
            PaidAmount = paid.Egp,
            PaidAmountCurrency = paid.Cur,
            ReturnedAmount = returned.Egp,
            ReturnedAmountCurrency = returned.Cur,
            RefundedAmount = refunded.Egp,
            RefundedAmountCurrency = refunded.Cur,
            RemainingAmount = isSale ? Math.Max(0, invoice.NetTotal - returned.Egp - paid.Egp) : 0,
            RemainingAmountCurrency = isSale ? Math.Max(0, invoice.NetTotalCurrency - returned.Cur - paid.Cur) : 0,
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
                ProductNetTotal = i.ProductNetTotal,
                OriginalItemId = i.OriginalItemId
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
                UpdatedBy = z.UpdatedBy ?? "",
                Type = z.Type
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
            .Include(i => i.Returns.Where(r => !r.IsDeleted))
            .Include(i => i.Payments!.Where(p => !p.IsDeleted)).ThenInclude(z => z.Auditor)
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
        var egp = dto.Items.Where(i => i.PriceType == PriceType.Egyptian).ToList();
        var cur = dto.Items.Where(i => i.PriceType == PriceType.Currency).ToList();

        dto.Total = Math.Round(egp.Sum(i => i.ProductTotal), 2);
        dto.Discount = Math.Round(egp.Sum(i => i.ProductDiscount), 2);
        dto.NetTotal = Math.Round(dto.Total - dto.Discount, 2);
        dto.NetTotalCurrency = Math.Round(cur.Sum(i => i.ProductNetTotal), 2);
    }
    private async Task<long> NextNumberAsync()
    {
        await _dbContext.Database.ExecuteSqlRawAsync(@"
        DECLARE @r int;
        EXEC @r = sp_getapplock @Resource = 'sale_invoice_number', @LockMode = 'Exclusive',
                                @LockOwner = 'Transaction', @LockTimeout = 10000;
        IF @r < 0 THROW 50001, 'Could not lock invoice numbering', 1;");

        var max = await _dbContext.SaleInvoice.IgnoreQueryFilters().MaxAsync(i => (long?)i.Number) ?? 0;
        return max + 1;
    }

    // Centralized validation used by both Create and Update.
    private async Task<string?> ValidateAsync(SaleInvoiceDto dto, bool isUpdate, long? currentId = null)
    {
        if (dto is null)
            return "Invoice payload is required";

        // --- Type ---
        if (dto.Type != InvoiceType.Sale)
            return "Use the return endpoints for Sale_Return invoices";

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

            var orderCustomer = await _dbContext.Order.Where(o => o.Id == dto.OrderId.Value)
                .Select(o => o.CustomerId).FirstAsync();
            if (orderCustomer != dto.CustomerId)
                return $"Order {dto.OrderId.Value} does not belong to customer {dto.CustomerId}";
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

    private async Task<string?> ValidatePaymentsAsync(List<SaleInvoicePaymentDto>? payments, decimal netEgp, decimal netCur)
    {
        if (payments is null || payments.Count == 0) return null;

        var safeIds = payments.Select(p => p.MoneySafeId).Distinct().ToList();
        var safes = await _dbContext.MoneySafe.AsNoTracking()
            .Where(m => safeIds.Contains(m.Id) && !m.IsDeleted).ToListAsync();

        foreach (var p in payments)
        {
            var safe = safes.FirstOrDefault(m => m.Id == p.MoneySafeId);
            if (safe is null) return $"Money safe with Id {p.MoneySafeId} was not found";
            if (!safe.Active) return $"Money safe '{safe.Name}' is not active";
            if (p.Amount < 0 || p.AmountCurrency < 0) return "Payment amounts cannot be negative";
            if (p.Amount == 0 && p.AmountCurrency == 0) return "Each payment needs an amount or a currency amount";
            if (p.AmountCurrency > 0 && !safe.IsMultiCurrency)
                return $"Money safe '{safe.Name}' does not accept currency amounts";
            if (!string.IsNullOrWhiteSpace(p.Notes) && p.Notes.Length > NotesMaxLength)
                return $"Payment notes cannot exceed {NotesMaxLength} characters";
        }

        if (payments.Sum(p => p.Amount) > netEgp)
            return "Payments cannot exceed the invoice net total";
        if (payments.Sum(p => p.AmountCurrency) > netCur)
            return "Currency payments cannot exceed the invoice net total in currency";
        return null;
    }

    private static readonly Expression<Func<SaleInvoice, bool>> IsUnpaid = i =>
    i.Type == InvoiceType.Sale &&
    (i.NetTotal - i.Returns.Where(r => !r.IsDeleted).Sum(r => r.NetTotal)
                - i.Payments.Where(p => !p.IsDeleted && p.Type != PaymentType.Refund_Payment).Sum(p => p.Amount) > 0
  || i.NetTotalCurrency - i.Returns.Where(r => !r.IsDeleted).Sum(r => r.NetTotalCurrency)
                        - i.Payments.Where(p => !p.IsDeleted && p.Type != PaymentType.Refund_Payment).Sum(p => p.AmountCurrency) > 0);

    private static readonly Expression<Func<SaleInvoice, bool>> IsPaid = i =>
        i.Type == InvoiceType.Sale &&
        (i.NetTotal - i.Returns.Where(r => !r.IsDeleted).Sum(r => r.NetTotal)
                    - i.Payments.Where(p => !p.IsDeleted && p.Type != PaymentType.Refund_Payment).Sum(p => p.Amount) <= 0
      && i.NetTotalCurrency - i.Returns.Where(r => !r.IsDeleted).Sum(r => r.NetTotalCurrency)
                            - i.Payments.Where(p => !p.IsDeleted && p.Type != PaymentType.Refund_Payment).Sum(p => p.AmountCurrency) <= 0);

    private IQueryable<SaleInvoice> ApplyFilters(IQueryable<SaleInvoice> q, SaleInvoiceFilterDto f)
    {
        if (f.Number.HasValue)
        {
            q = q.Where(i => i.Number == f.Number.Value);
        }
        else if (f.OrderId.HasValue)
        {
            q = q.Where(i => i.OrderId == f.OrderId.Value);    // an order search ignores the dates too
        }
        else
        {
            if (f.DateFrom.HasValue) { var from = f.DateFrom.Value.Date; q = q.Where(i => i.Date >= from); }
            if (f.DateTo.HasValue) { var to = f.DateTo.Value.Date.AddDays(1); q = q.Where(i => i.Date < to); }
        }

        if (f.CustomerId.HasValue) q = q.Where(i => i.CustomerId == f.CustomerId.Value);
        if (f.Type.HasValue) q = q.Where(i => i.Type == f.Type.Value);

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim();
            q = q.Where(i => i.Customer!.Name.Contains(s) || i.Notes.Contains(s));
        }

        return f.PayStatus switch
        {
            "unpaid" => q.Where(IsUnpaid),
            "paid" => q.Where(IsPaid),
            _ => q
        };
    }


    //-----------------------

    public async Task<ServiceResult<List<SaleInvoiceDto>>> GetAllAsync()
    {
        var invoices = await WithIncludes(_dbContext.SaleInvoice.Where(z => !z.IsDeleted))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<SaleInvoiceDto>>.Ok(invoices.Select(ToDto).ToList());
    }
    public async Task<ServiceResult<PagedResult<SaleInvoiceDto, InvoiceListSummaryDto>>> GetPagedAsync(SaleInvoiceFilterDto f)
    {
        var q = ApplyFilters(_dbContext.SaleInvoice.Where(i => !i.IsDeleted), f);

        var rows = await q.Select(i => new
        {
            i.Type,
            i.NetTotal,
            i.NetTotalCurrency,
            Paid = i.Payments.Where(p => !p.IsDeleted && p.Type != PaymentType.Refund_Payment).Sum(p => p.Amount),
            PaidCur = i.Payments.Where(p => !p.IsDeleted && p.Type != PaymentType.Refund_Payment).Sum(p => p.AmountCurrency),
            Ret = i.Returns.Where(r => !r.IsDeleted).Sum(r => r.NetTotal),
            RetCur = i.Returns.Where(r => !r.IsDeleted).Sum(r => r.NetTotalCurrency)
        }).ToListAsync();

        var summary = new InvoiceListSummaryDto();
        foreach (var r in rows)
        {
            var sign = r.Type == InvoiceType.Sale_Return ? -1 : 1;
            summary.Net += sign * r.NetTotal;
            summary.NetCurrency += sign * r.NetTotalCurrency;
            if (r.Type != InvoiceType.Sale) continue;
            summary.Paid += r.Paid;
            summary.Remaining += Math.Max(0, r.NetTotal - r.Ret - r.Paid);
            summary.RemainingCurrency += Math.Max(0, r.NetTotalCurrency - r.RetCur - r.PaidCur);
        }

        var page = await q.OrderByDescending(i => i.Date).ThenByDescending(i => i.Id)
            .Select(i => i.Id).ToPagedAsync(f);

        var loaded = await WithIncludes(_dbContext.SaleInvoice.Where(i => page.Items.Contains(i.Id)))
            .AsNoTracking().ToListAsync();

        return ServiceResult<PagedResult<SaleInvoiceDto, InvoiceListSummaryDto>>.Ok(
            new PagedResult<SaleInvoiceDto, InvoiceListSummaryDto>
            {
                Items = page.Items.Select(id => loaded.First(i => i.Id == id)).Select(ToDto).ToList(),
                Total = page.Total,
                Page = page.Page,
                PageSize = page.PageSize,
                Summary = summary
            });
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

        // advance payments of this order that fit in the invoice get attached to it (oldest first)
        var advances = new List<CustomerPayment>();
        if (dto.OrderId.HasValue)
        {
            var pending = await _dbContext.CustomerPayment
                .Where(p => !p.IsDeleted && p.Type == PaymentType.Advance_Payment && p.InvoiceId == null
                            && p.AdvancePaymentOrderId == dto.OrderId && p.CustomerId == dto.CustomerId)
                .OrderBy(p => p.Date).ThenBy(p => p.Id)
                .ToListAsync();

            decimal e = 0, c = 0;
            foreach (var p in pending)
            {
                if (e + p.Amount > dto.NetTotal || c + p.AmountCurrency > dto.NetTotalCurrency) continue;
                advances.Add(p);
                e += p.Amount;
                c += p.AmountCurrency;
            }
        }

        var paymentError = await ValidatePaymentsAsync(dto.Payments,
            dto.NetTotal - advances.Sum(a => a.Amount),
            dto.NetTotalCurrency - advances.Sum(a => a.AmountCurrency));
        if (paymentError is not null)
            return ServiceResult<SaleInvoiceDto>.Failed(paymentError);

        var invoice = new SaleInvoice
        {
            Type = InvoiceType.Sale,
            Date = dto.Date,
            Notes = dto.Notes ?? "",
            CustomerId = dto.CustomerId,
            AuditorId = dto.AuditorId,
            OrderId = dto.OrderId,
            OriginalInvoiceId = null,
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
            }).ToList(),
            Payments = (dto.Payments ?? new()).Select(p => new CustomerPayment
            {
                Date = p.PaymentDate == default ? dto.Date : p.PaymentDate,
                CustomerId = dto.CustomerId,
                Type = PaymentType.Regular_Payment,
                MoneySafeId = p.MoneySafeId,
                Amount = p.Amount,
                AmountCurrency = p.AmountCurrency,
                Notes = p.Notes ?? "",
                AuditorId = dto.AuditorId
            }).ToList()
        };
        foreach (var a in advances) invoice.Payments.Add(a);   // the FK is filled when EF saves

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var tx = await _dbContext.Database.BeginTransactionAsync();
            invoice.Number = await NextNumberAsync();
            _dbContext.SaleInvoice.Add(invoice);
            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();
        });

        _logger.LogInformation("Sale invoice {InvoiceId} created (Number {Number}, {Advances} advance payments attached)",
            invoice.Id, invoice.Number, advances.Count);

        var created = await WithIncludes(_dbContext.SaleInvoice).AsNoTracking().FirstAsync(i => i.Id == invoice.Id);
        return ServiceResult<SaleInvoiceDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, SaleInvoiceDto dto, bool isSuperAdmin = false)
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

        if (existing.Type != InvoiceType.Sale)
            return ServiceResult<bool>.Failed("Use the return endpoints for Sale_Return invoices");

        var payments = existing.Payments.Where(p => !p.IsDeleted).ToList();
        var hasReturns = await _dbContext.SaleInvoice.AnyAsync(r => !r.IsDeleted && r.OriginalInvoiceId == id);

        if (!isSuperAdmin && payments.Count > 0)
            return ServiceResult<bool>.Failed("This invoice has payments and can no longer be edited");
        if (!isSuperAdmin && hasReturns)
            return ServiceResult<bool>.Failed("This invoice has returns and can no longer be edited");

        if (hasReturns)   // SuperAdmin on an invoice with returns: header only
        {
            existing.Date = dto.Date;
            existing.Notes = dto.Notes ?? "";
            await _dbContext.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        if (payments.Count > 0 && dto.CustomerId != existing.CustomerId)
            return ServiceResult<bool>.Failed("The customer of an invoice with payments cannot be changed");

        var validationError = await ValidateAsync(dto, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating sale invoice {InvoiceId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        RecalculateTotals(dto);

        if (payments.Count > 0)
        {
            var paid = SaleBalances.Paid(payments);
            if (dto.NetTotal < paid.Egp)
                return ServiceResult<bool>.Failed("Net total cannot be less than the amount already paid");
            if (dto.NetTotalCurrency < paid.Cur)
                return ServiceResult<bool>.Failed("Net total in currency cannot be less than the currency already paid");
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

            //existing.Number = dto.Number;
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

    public async Task<ServiceResult<bool>> DeleteAsync(long id, bool isSuperAdmin = false)
    {
        var invoice = await _dbContext.SaleInvoice
            .Include(i => i.Items).Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
        if (invoice is null)
            return ServiceResult<bool>.NotFound("Invoice not found");

        var payments = invoice.Payments.Where(p => !p.IsDeleted).ToList();
        var returns = invoice.Type == InvoiceType.Sale
            ? await _dbContext.SaleInvoice.Include(r => r.Items).Include(r => r.Payments)
                .Where(r => !r.IsDeleted && r.OriginalInvoiceId == id).ToListAsync()
            : new List<SaleInvoice>();

        var linked = payments.Count > 0 || returns.Count > 0;
        if (!isSuperAdmin && linked)
            return ServiceResult<bool>.Failed("Delete the payments and returns of this invoice first");

        var now = DateTime.UtcNow;
        foreach (var r in returns)
        {
            foreach (var p in r.Payments.Where(p => !p.IsDeleted)) { p.IsDeleted = true; p.DeletedAtUtc = now; }
            foreach (var it in r.Items) { it.IsDeleted = true; it.DeletedAtUtc = now; }
            r.IsDeleted = true; r.DeletedAtUtc = now;
        }
        foreach (var p in payments) { p.IsDeleted = true; p.DeletedAtUtc = now; }
        foreach (var it in invoice.Items.Where(i => !i.IsDeleted)) { it.IsDeleted = true; it.DeletedAtUtc = now; }
        invoice.IsDeleted = true; invoice.DeletedAtUtc = now;

        await _dbContext.SaveChangesAsync();
        _logger.LogWarning("Sale invoice {InvoiceId} deleted with {P} payments and {R} returns", id, payments.Count, returns.Count);
        return ServiceResult<bool>.Ok(true);
    }


    //-----------------------


    private async Task<List<SaleInvoiceDto>> UnpaidAsync(IQueryable<SaleInvoice> query)
    {
        var list = await query
            .Where(i => !i.IsDeleted && i.Type == InvoiceType.Sale)
            .Include(i => i.Customer)
            .Include(i => i.Payments!.Where(p => !p.IsDeleted))
            .Include(i => i.Returns.Where(r => !r.IsDeleted))
            .OrderBy(i => i.Date).ThenBy(i => i.Id)
            .AsNoTracking().ToListAsync();

        return list.Select(ToDto).Where(d => d.RemainingAmount > 0 || d.RemainingAmountCurrency > 0).ToList();
    }

    public async Task<ServiceResult<List<SaleInvoiceDto>>> GetUnpaidByCustomerAsync(long customerId) =>
        customerId <= 0
            ? ServiceResult<List<SaleInvoiceDto>>.Failed("A valid customer Id is required")
            : ServiceResult<List<SaleInvoiceDto>>.Ok(await UnpaidAsync(_dbContext.SaleInvoice.Where(i => i.CustomerId == customerId)));

    public async Task<ServiceResult<List<SaleInvoiceDto>>> GetUnpaidByOrderAsync(long orderId) =>
        orderId <= 0
            ? ServiceResult<List<SaleInvoiceDto>>.Failed("A valid order Id is required")
            : ServiceResult<List<SaleInvoiceDto>>.Ok(await UnpaidAsync(_dbContext.SaleInvoice.Where(i => i.OrderId == orderId)));

    public async Task<ServiceResult<List<SaleInvoiceDto>>> GetSaleByCustomerAsync(long customerId)
    {
        var list = await _dbContext.SaleInvoice
            .Where(i => !i.IsDeleted && i.CustomerId == customerId && i.Type == InvoiceType.Sale)
            .Include(i => i.Customer)
            .OrderByDescending(i => i.Date).ThenByDescending(i => i.Id)
            .AsNoTracking().ToListAsync();
        return ServiceResult<List<SaleInvoiceDto>>.Ok(list.Select(ToDto).ToList());
    }

    // the order's data to start an invoice from (nothing is saved)
    public async Task<ServiceResult<SaleInvoiceDto>> GetDraftFromOrderAsync(long orderId)
    {
        var order = await _dbContext.Order
            .Include(o => o.Customer)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Product)   // drop the filter if OrderItem has no IsDeleted
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);
        if (order is null)
            return ServiceResult<SaleInvoiceDto>.NotFound("Order not found");

        var adv = await _dbContext.CustomerPayment
            .Where(p => !p.IsDeleted && p.Type == PaymentType.Advance_Payment && p.InvoiceId == null
                        && p.AdvancePaymentOrderId == orderId)
            .Select(p => new { p.Amount, p.AmountCurrency }).ToListAsync();

        return ServiceResult<SaleInvoiceDto>.Ok(new SaleInvoiceDto
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer?.Name ?? "",
            OrderNetTotal = order.NetTotal,
            OrderAdvanceAmount = adv.Sum(a => a.Amount),
            OrderAdvanceAmountCurrency = adv.Sum(a => a.AmountCurrency),
            OrderItems = order.Items.Select(i => new SaleInvoiceItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? "",
                ProductPrice = i.ProductPrice,
                ProductQuantity = i.ProductQuantity,
                ProductDiscount = i.ProductDiscount
            }).ToList()
        });
    }

    //------------------

    public async Task<ServiceResult<SaleReturnableInvoiceDto>> GetReturnableAsync(long invoiceId, long? excludeReturnId)
    {
        var original = await _dbContext.SaleInvoice
            .Where(i => !i.IsDeleted && i.Id == invoiceId && i.Type == InvoiceType.Sale)
            .Include(i => i.Customer)
            .Include(i => i.Items!.Where(x => !x.IsDeleted)).ThenInclude(x => x.Product)
            .Include(i => i.Items!.Where(x => !x.IsDeleted)).ThenInclude(x => x.Store)
            .AsNoTracking().FirstOrDefaultAsync();
        if (original is null)
            return ServiceResult<SaleReturnableInvoiceDto>.NotFound("Sale invoice not found");

        var returned = await ReturnedQuantitiesAsync(original.Items.Select(i => i.Id).ToList(), excludeReturnId);

        return ServiceResult<SaleReturnableInvoiceDto>.Ok(new SaleReturnableInvoiceDto
        {
            InvoiceId = original.Id,
            Number = original.Number,
            Date = original.Date,
            CustomerId = original.CustomerId,
            CustomerName = original.Customer?.Name ?? "",
            Items = original.Items.Select(i =>
            {
                var done = returned.GetValueOrDefault(i.Id);
                return new ReturnableItemDto
                {
                    ItemId = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? "",
                    StoreName = i.Store?.Name ?? "",
                    PriceType = i.PriceType.ToString(),
                    ProductPrice = i.ProductPrice,
                    ProductDiscount = i.ProductDiscount,
                    OriginalQuantity = i.ProductQuantity,
                    ReturnedQuantity = done,
                    ReturnableQuantity = Math.Max(0, i.ProductQuantity - done)
                };
            }).ToList()
        });
    }

    private async Task<Dictionary<long, decimal>> ReturnedQuantitiesAsync(List<long> itemIds, long? excludeReturnId) =>
        await _dbContext.SaleInvoiceItem
            .Where(r => !r.IsDeleted && r.OriginalItemId != null && itemIds.Contains(r.OriginalItemId.Value)
                        && r.InvoiceId != (excludeReturnId ?? 0) && !r.Invoice!.IsDeleted)
            .GroupBy(r => r.OriginalItemId!.Value)
            .Select(g => new { ItemId = g.Key, Qty = g.Sum(x => x.ProductQuantity) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Qty);

    private async Task<(string? Error, SaleInvoice? Original, List<SaleInvoiceItem> Items)> PrepareReturnAsync(
        SaleReturnRequestDto req, long? currentId)
    {
        var items = new List<SaleInvoiceItem>();
        if (req is null) return ("Return payload is required", null, items);
        if (req.Date == default) return ("Date is required", null, items);
        if (!string.IsNullOrWhiteSpace(req.Notes) && req.Notes.Length > NotesMaxLength)
            return ($"Notes cannot exceed {NotesMaxLength} characters", null, items);

        if (!await _dbContext.Employee.AnyAsync(e => e.Id == req.AuditorId && !e.IsDeleted))
            return ($"Auditor (Employee) with Id {req.AuditorId} was not found", null, items);

        var original = await _dbContext.SaleInvoice.Include(i => i.Items).AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == req.OriginalInvoiceId && !i.IsDeleted && i.Type == InvoiceType.Sale);
        if (original is null) return ("Original sale invoice not found", null, items);

        if (req.Items is null || req.Items.Count == 0) return ("At least one item is required", original, items);

        var ids = req.Items.Select(i => i.OriginalItemId).ToList();
        if (ids.Distinct().Count() != ids.Count) return ("An item can appear only once", original, items);

        var returned = await ReturnedQuantitiesAsync(ids, currentId);

        foreach (var ri in req.Items)
        {
            var orig = original.Items.FirstOrDefault(x => x.Id == ri.OriginalItemId && !x.IsDeleted);
            if (orig is null)
                return ($"Item {ri.OriginalItemId} does not belong to invoice {original.Number}", original, items);
            if (ri.Quantity <= 0) return ("Quantity must be greater than zero", original, items);

            var available = orig.ProductQuantity - returned.GetValueOrDefault(orig.Id);
            if (ri.Quantity > available)
                return ($"Quantity for product {orig.ProductId} exceeds the returnable quantity ({available})", original, items);

            var total = Math.Round(orig.ProductPrice * ri.Quantity, 2);
            var discount = orig.ProductQuantity > 0
                ? Math.Round(orig.ProductDiscount * ri.Quantity / orig.ProductQuantity, 2) : 0;

            items.Add(new SaleInvoiceItem
            {
                ProductId = orig.ProductId,
                StoreId = orig.StoreId,
                ProjectId = orig.ProjectId,
                ProductNotes = orig.ProductNotes,
                PriceType = orig.PriceType,
                ProductPrice = orig.ProductPrice,
                ProductQuantity = ri.Quantity,
                ProductTotal = total,
                ProductDiscount = discount,
                ProductNetTotal = total - discount,
                OriginalItemId = orig.Id
            });
        }
        return (null, original, items);
    }

    private static void ApplyReturnTotals(SaleInvoice invoice, List<SaleInvoiceItem> items)
    {
        var egp = items.Where(i => i.PriceType == PriceType.Egyptian).ToList();
        var cur = items.Where(i => i.PriceType == PriceType.Currency).ToList();
        invoice.Total = Math.Round(egp.Sum(i => i.ProductTotal), 2);
        invoice.Discount = Math.Round(egp.Sum(i => i.ProductDiscount), 2);
        invoice.NetTotal = Math.Round(invoice.Total - invoice.Discount, 2);
        invoice.NetTotalCurrency = Math.Round(cur.Sum(i => i.ProductNetTotal), 2);
    }

    public async Task<ServiceResult<SaleInvoiceDto>> CreateReturnAsync(SaleReturnRequestDto req)
    {
        var (error, original, items) = await PrepareReturnAsync(req, null);
        if (error is not null)
            return ServiceResult<SaleInvoiceDto>.Failed(error);

        var invoice = new SaleInvoice
        {
            Type = InvoiceType.Sale_Return,
            Date = req.Date,
            Notes = req.Notes ?? "",
            CustomerId = original!.CustomerId,
            AuditorId = req.AuditorId,
            OrderId = original.OrderId,
            OriginalInvoiceId = original.Id,
            ShiftId = original.ShiftId,
            Items = items
        };
        ApplyReturnTotals(invoice, items);

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var tx = await _dbContext.Database.BeginTransactionAsync();
            invoice.Number = await NextNumberAsync();
            _dbContext.SaleInvoice.Add(invoice);
            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();
        });

        var created = await WithIncludes(_dbContext.SaleInvoice).AsNoTracking().FirstAsync(i => i.Id == invoice.Id);
        return ServiceResult<SaleInvoiceDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateReturnAsync(long id, SaleReturnRequestDto req, bool isSuperAdmin = false)
    {
        var existing = await _dbContext.SaleInvoice.Include(i => i.Items).Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted && i.Type == InvoiceType.Sale_Return);
        if (existing is null) return ServiceResult<bool>.NotFound("Return invoice not found");

        if (req is not null && req.OriginalInvoiceId != existing.OriginalInvoiceId)
            return ServiceResult<bool>.Failed("The original invoice of a return cannot be changed");

        var refunded = SaleBalances.Refunded(existing.Payments);
        if (refunded.Egp > 0 || refunded.Cur > 0)
        {
            if (!isSuperAdmin)
                return ServiceResult<bool>.Failed("This return has refunds and can no longer be edited");
        }

        var (error, original, items) = await PrepareReturnAsync(req!, id);
        if (error is not null) return ServiceResult<bool>.Failed(error);

        existing.Date = req!.Date;
        existing.Notes = req.Notes ?? "";
        existing.AuditorId = req.AuditorId;
        ApplyReturnTotals(existing, items);

        if (existing.NetTotal < refunded.Egp || existing.NetTotalCurrency < refunded.Cur)
            return ServiceResult<bool>.Failed("The return value cannot be less than what was already refunded");

        _dbContext.SaleInvoiceItem.RemoveRange(existing.Items);
        foreach (var item in items) item.InvoiceId = id;
        await _dbContext.SaleInvoiceItem.AddRangeAsync(items);
        await _dbContext.SaveChangesAsync();
        return ServiceResult<bool>.Ok(true);
    }

    //------------------

    public async Task<(byte[] Bytes, string FileName)?> ExportAsync(long id)
    {
        var invoice = await WithIncludes(_dbContext.SaleInvoice.Where(i => !i.IsDeleted && i.Id == id))
            .AsNoTracking().FirstOrDefaultAsync();
        if (invoice is null) return null;

        var d = ToDto(invoice);
        var safeIds = d.Payments.Select(p => p.MoneySafeId).Distinct().ToList();
        var safes = await _dbContext.MoneySafe.Where(m => safeIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Name);

        string Kind(PaymentType t) => t switch
        {
            PaymentType.Advance_Payment => "دفعة مقدمة",
            PaymentType.Refund_Payment => "استرداد",
            _ => "دفعة"
        };

        var isReturn = d.Type == InvoiceType.Sale_Return;
        var sheet = new InvoiceSheet
        {
            Title = isReturn ? "مرتجع بيع" : "فاتورة بيع",
            PartyLabel = "العميل",
            PartyName = d.CustomerName,
            Number = d.Number,
            Date = d.Date,
            Notes = d.Notes,
            Auditor = d.AuditorName,
            OriginalNumber = d.OriginalInvoiceNumber,
            OrderId = d.OrderId,
            Items = d.Items.Select(i => new InvoiceSheetItem(i.ProductName, i.StoreName, i.ProjectName,
                InvoiceExcel.PriceTypeLabel(i.PriceType), i.ProductPrice, i.ProductQuantity, i.ProductTotal,
                i.ProductDiscount, i.ProductNetTotal, i.ProductNotes)).ToList(),
            Payments = d.Payments.Select(p => new InvoiceSheetPayment(p.PaymentDate, Kind(p.Type),
                safes.GetValueOrDefault(p.MoneySafeId, ""), p.Amount, p.AmountCurrency, p.Notes,
                string.IsNullOrEmpty(p.AuditorName) ? p.CreatedBy : p.AuditorName)).ToList(),
            Summary = new List<InvoiceSheetSummary>
        {
            new("الاجمالي", d.Total, 0),
            new("الخصم", d.Discount, 0),
            new("الصافي", d.NetTotal, d.NetTotalCurrency),
        }
        };

        if (isReturn)
        {
            sheet.Summary.Add(new("المسترد", d.RefundedAmount, d.RefundedAmountCurrency));
        }
        else
        {
            sheet.Summary.Add(new("المرتجعات", d.ReturnedAmount, d.ReturnedAmountCurrency));
            sheet.Summary.Add(new("المدفوع", d.PaidAmount, d.PaidAmountCurrency));
            sheet.Summary.Add(new("المتبقي", d.RemainingAmount, d.RemainingAmountCurrency));
        }

        return (InvoiceExcel.Build(sheet), $"sale-invoice-{d.Number}.xlsx");
    }
}