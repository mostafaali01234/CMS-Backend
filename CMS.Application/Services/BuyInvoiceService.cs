// Services/BuyInvoiceService.cs
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

public class BuyInvoiceService : IBuyInvoiceService
{
    private readonly IAppDbContext _dbContext;
    private readonly ISupplierPaymentService _paymentService;
    private readonly ILogger<BuyInvoiceService> _logger;

    private const int NotesMaxLength = 2000;
    private const int ProductNotesMaxLength = 500;

    public BuyInvoiceService(
        IAppDbContext dbContext,
        ISupplierPaymentService paymentService,
        ILogger<BuyInvoiceService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _paymentService = paymentService;
    }

    private BuyInvoiceDto ToDto(BuyInvoice invoice)
    {
        var payments = invoice.Payments?.Where(p => !p.IsDeleted).ToList() ?? new List<SupplierPayment>();
        var returns = invoice.Returns?.Where(r => !r.IsDeleted).ToList() ?? new List<BuyInvoice>();

        var paidAmount = payments.Sum(p => p.Amount);
        var paidCurrency = payments.Sum(p => p.AmountCurrency);
        var returned = returns.Sum(r => r.NetTotal);
        var returnedCurrency = returns.Sum(r => r.NetTotalCurrency);
        var isBuy = invoice.Type == InvoiceType.Buy;

        return new BuyInvoiceDto
        {
            Id = invoice.Id,
            Number = invoice.Number,
            Type = invoice.Type,
            Date = invoice.Date,
            Notes = invoice.Notes,
            SupplierId = invoice.SupplierId,
            SupplierName = invoice.Supplier?.Name ?? "",
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
            PaidAmountCurrency = paidCurrency,
            ReturnedAmount = returned,
            ReturnedAmountCurrency = returnedCurrency,
            RemainingAmount = isBuy ? Math.Max(0, invoice.NetTotal - returned - paidAmount) : 0,
            RemainingAmountCurrency = isBuy ? Math.Max(0, invoice.NetTotalCurrency - returnedCurrency - paidCurrency) : 0,
            Items = invoice.Items?.Where(i => !i.IsDeleted).Select(i => new BuyInvoiceItemDto
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
                OriginalItemId = i.OriginalItemId,
            }).ToList() ?? new List<BuyInvoiceItemDto>(),
            Payments = invoice.Payments?.Where(z => !z.IsDeleted).Select(z => new BuyInvoicePaymentDto
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
            }).ToList() ?? new List<BuyInvoicePaymentDto>(),
            CreatedAtUtc = invoice.CreatedAtUtc,
            CreatedBy = invoice.CreatedBy ?? "",
            UpdatedAtUtc = invoice.UpdatedAtUtc,
            UpdatedBy = invoice.UpdatedBy ?? ""
        };
    }

    private IQueryable<BuyInvoice> WithIncludes(IQueryable<BuyInvoice> query)
    {
        return query
            .Include(i => i.Supplier)
            .Include(i => i.Auditor)
            .Include(i => i.OriginalInvoice)
            .Include(i => i.Payments!.Where(p => !p.IsDeleted)).ThenInclude(z => z.Auditor)
            .Include(i => i.Items!.Where(x => !x.IsDeleted))
                .ThenInclude(x => x.Product)
            .Include(i => i.Items!.Where(x => !x.IsDeleted))
                .ThenInclude(x => x.Store)
            .Include(i => i.Items!.Where(x => !x.IsDeleted))
                .ThenInclude(x => x.Project)
            .Include(i => i.Returns.Where(r => !r.IsDeleted));
    }

    // Recalculates each item's ProductTotal/ProductNetTotal, then the invoice-level
    // Total/Discount/NetTotal as the sum of the item values. NetTotalCurrency is left
    // as supplied — it's an FX conversion of NetTotal, not something derivable from the items.
    private void RecalculateTotals(BuyInvoiceDto dto)
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
        // one writer at a time until this transaction ends, so two saves can't read the same max
        await _dbContext.Database.ExecuteSqlRawAsync(@"
        DECLARE @r int;
        EXEC @r = sp_getapplock @Resource = 'buy_invoice_number', @LockMode = 'Exclusive',
                                @LockOwner = 'Transaction', @LockTimeout = 10000;
        IF @r < 0 THROW 50001, 'Could not lock invoice numbering', 1;");

        // includes soft-deleted rows, so a deleted invoice's number is never reused
        var max = await _dbContext.BuyInvoice
            .IgnoreQueryFilters()
            .MaxAsync(i => (long?)i.Number) ?? 0;

        return max + 1;
    }

    // Centralized validation used by both Create and Update.
    private async Task<string?> ValidateAsync(BuyInvoiceDto dto, bool isUpdate, long? currentId = null)
    {
        if (dto is null)
            return "Invoice payload is required";

        //// --- Number (required + unique) ---
        //if (dto.Number <= 0)
        //    return "Number is required and must be greater than zero";

        //var numberQuery = _dbContext.BuyInvoice.Where(i => !i.IsDeleted && i.Number == dto.Number);

        //if (isUpdate && currentId.HasValue)
        //    numberQuery = numberQuery.Where(i => i.Id != currentId.Value);

        //if (await numberQuery.AnyAsync())
        //    return $"An invoice with number {dto.Number} already exists";

        // --- Type ---
        if (dto.Type != InvoiceType.Buy && dto.Type != InvoiceType.Buy_Return)
            return "Type must be Buy or Buy_Return for a buy invoice";
        if (dto.Type != InvoiceType.Buy)
            return "Use the return endpoints for Buy_Return invoices";
        // --- Date ---
        if (dto.Date == default)
            return "Date is required";

        // --- Notes (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Length > NotesMaxLength)
            return $"Notes cannot exceed {NotesMaxLength} characters";

        // --- Supplier (required FK) ---
        if (dto.SupplierId <= 0)
            return "SupplierId is required";

        var supplierExists = await _dbContext.Supplier
            .AnyAsync(s => s.Id == dto.SupplierId && !s.IsDeleted);

        if (!supplierExists)
            return $"Supplier with Id {dto.SupplierId} was not found";

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

        // --- OriginalInvoice (optional FK, must be a different invoice from the same supplier) ---
        if (dto.OriginalInvoiceId.HasValue)
        {
            if (dto.OriginalInvoiceId.Value <= 0)
                return "OriginalInvoiceId is invalid";

            if (isUpdate && currentId.HasValue && dto.OriginalInvoiceId.Value == currentId.Value)
                return "An invoice cannot reference itself as the original invoice";

            var original = await _dbContext.BuyInvoice
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == dto.OriginalInvoiceId.Value && !i.IsDeleted);

            if (original is null)
                return $"Original invoice with Id {dto.OriginalInvoiceId.Value} was not found";

            if (original.SupplierId != dto.SupplierId)
                return $"Original invoice {dto.OriginalInvoiceId.Value} does not belong to supplier {dto.SupplierId}";
        }

        //// --- NetTotalCurrency ---
        //if (dto.NetTotalCurrency < 0)
        //    return "NetTotalCurrency cannot be negative";

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

    private async Task<string?> ValidatePaymentsAsync(
    List<BuyInvoicePaymentDto>? payments, decimal netTotal, decimal netTotalCurrency, InvoiceType type)
    {
        if (payments is null || payments.Count == 0) return null;

        if (type != InvoiceType.Buy)
            return "Payments can only be recorded on Buy invoices";

        var safeIds = payments.Select(p => p.MoneySafeId).Distinct().ToList();
        var safes = await _dbContext.MoneySafe
            .AsNoTracking()
            .Where(m => safeIds.Contains(m.Id) && !m.IsDeleted)
            .ToListAsync();

        foreach (var p in payments)
        {
            var safe = safes.FirstOrDefault(m => m.Id == p.MoneySafeId);
            if (safe is null) return $"Money safe with Id {p.MoneySafeId} was not found";
            if (!safe.Active) return $"Money safe '{safe.Name}' is not active";
            if (p.Amount < 0) return "Payment amount cannot be negative";
            if (p.AmountCurrency < 0) return "Payment AmountCurrency cannot be negative";
            if (p.Amount == 0 && p.AmountCurrency == 0)
                return "Each payment needs an amount or a currency amount";
            if (p.AmountCurrency > 0 && !safe.IsMultiCurrency)
                return $"Money safe '{safe.Name}' does not accept currency amounts";
            if (!string.IsNullOrWhiteSpace(p.Notes) && p.Notes.Length > NotesMaxLength)
                return $"Payment notes cannot exceed {NotesMaxLength} characters";
        }

        if (payments.Sum(p => p.Amount) > netTotal)
            return "Payments cannot exceed the invoice net total";

        if (payments.Sum(p => p.AmountCurrency) > netTotalCurrency)
            return "Currency payments cannot exceed the invoice net total in currency";

        return null;
    }

    private static readonly Expression<Func<BuyInvoice, bool>> IsUnpaid = i =>
        i.Type == InvoiceType.Buy &&
        (i.NetTotal - i.Returns.Where(r => !r.IsDeleted).Sum(r => r.NetTotal)
                    - i.Payments.Where(p => !p.IsDeleted).Sum(p => p.Amount) > 0
      || i.NetTotalCurrency - i.Returns.Where(r => !r.IsDeleted).Sum(r => r.NetTotalCurrency)
                            - i.Payments.Where(p => !p.IsDeleted).Sum(p => p.AmountCurrency) > 0);

    private static readonly Expression<Func<BuyInvoice, bool>> IsPaid = i =>
        i.Type == InvoiceType.Buy &&
            (i.NetTotal - i.Returns.Where(r => !r.IsDeleted).Sum(r => r.NetTotal)
                        - i.Payments.Where(p => !p.IsDeleted).Sum(p => p.Amount) <= 0
          && i.NetTotalCurrency - i.Returns.Where(r => !r.IsDeleted).Sum(r => r.NetTotalCurrency)
                                - i.Payments.Where(p => !p.IsDeleted).Sum(p => p.AmountCurrency) <= 0);

    private IQueryable<BuyInvoice> ApplyFilters(IQueryable<BuyInvoice> q, BuyInvoiceFilterDto f)
    {
        if (f.Number.HasValue)
        {
            q = q.Where(i => i.Number == f.Number.Value);      // a number search ignores the dates
        }
        else
        {
            if (f.DateFrom.HasValue) { var from = f.DateFrom.Value.Date; q = q.Where(i => i.Date >= from); }
            if (f.DateTo.HasValue) { var to = f.DateTo.Value.Date.AddDays(1); q = q.Where(i => i.Date < to); }
        }

        if (f.SupplierId.HasValue) q = q.Where(i => i.SupplierId == f.SupplierId.Value);
        if (f.Type.HasValue) q = q.Where(i => i.Type == f.Type.Value);

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim();
            q = q.Where(i => i.Supplier!.Name.Contains(s) || i.Notes.Contains(s));
        }

        return f.PayStatus switch
        {
            "unpaid" => q.Where(IsUnpaid),
            "paid" => q.Where(IsPaid),
            _ => q
        };
    }

    ///--------------
    ///

    public async Task<ServiceResult<List<BuyInvoiceDto>>> GetAllAsync()
    {
        var invoices = await WithIncludes(_dbContext.BuyInvoice.Where(z => !z.IsDeleted))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<BuyInvoiceDto>>.Ok(invoices.Select(ToDto).ToList());
    }
    public async Task<ServiceResult<PagedResult<BuyInvoiceDto, InvoiceListSummaryDto>>> GetPagedAsync(BuyInvoiceFilterDto f)
    {
        var q = ApplyFilters(_dbContext.BuyInvoice.Where(i => !i.IsDeleted), f);

        // totals of the whole filter: only numbers are loaded, not invoices
        var rows = await q.Select(i => new
        {
            i.Type,
            i.NetTotal,
            i.NetTotalCurrency,
            Paid = i.Payments.Where(p => !p.IsDeleted).Sum(p => p.Amount),
            PaidCur = i.Payments.Where(p => !p.IsDeleted).Sum(p => p.AmountCurrency),
            Ret = i.Returns.Where(r => !r.IsDeleted).Sum(r => r.NetTotal),
            RetCur = i.Returns.Where(r => !r.IsDeleted).Sum(r => r.NetTotalCurrency)
        }).ToListAsync();

        var summary = new InvoiceListSummaryDto();
        foreach (var r in rows)
        {
            var sign = r.Type == InvoiceType.Buy_Return ? -1 : 1;
            summary.Net += sign * r.NetTotal;
            summary.NetCurrency += sign * r.NetTotalCurrency;
            if (r.Type != InvoiceType.Buy) continue;
            summary.Paid += r.Paid;
            summary.Remaining += Math.Max(0, r.NetTotal - r.Ret - r.Paid);
            summary.RemainingCurrency += Math.Max(0, r.NetTotalCurrency - r.RetCur - r.PaidCur);
        }

        // one page of ids first, then only those invoices are loaded with their items and payments
        var page = await q.OrderByDescending(i => i.Date).ThenByDescending(i => i.Id)
            .Select(i => i.Id).ToPagedAsync(f);

        var loaded = await WithIncludes(_dbContext.BuyInvoice.Where(i => page.Items.Contains(i.Id)))
            .AsNoTracking().ToListAsync();

        return ServiceResult<PagedResult<BuyInvoiceDto, InvoiceListSummaryDto>>.Ok(
            new PagedResult<BuyInvoiceDto, InvoiceListSummaryDto>
            {
                Items = page.Items.Select(id => loaded.First(i => i.Id == id)).Select(ToDto).ToList(),
                Total = page.Total,
                Page = page.Page,
                PageSize = page.PageSize,
                Summary = summary
            });
    }

    public async Task<ServiceResult<BuyInvoiceDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<BuyInvoiceDto?>.Failed("A valid invoice Id is required");

        var invoice = await WithIncludes(_dbContext.BuyInvoice.Where(z => !z.IsDeleted && z.Id == id))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (invoice is null)
            return ServiceResult<BuyInvoiceDto?>.NotFound("Invoice not found");

        return ServiceResult<BuyInvoiceDto?>.Ok(ToDto(invoice));
    }

    public async Task<ServiceResult<BuyInvoiceDto>> CreateAsync(BuyInvoiceDto dto)
    {
        var validationError = await ValidateAsync(dto, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating buy invoice: {Error}", validationError);
            return ServiceResult<BuyInvoiceDto>.Failed(validationError);
        }

        RecalculateTotals(dto);

        var paymentError = await ValidatePaymentsAsync(dto.Payments, dto.NetTotal, dto.NetTotalCurrency, dto.Type);
        if (paymentError is not null)
            return ServiceResult<BuyInvoiceDto>.Failed(paymentError);

        var invoice = new BuyInvoice
        {
            //Number = dto.Number,
            Type = dto.Type,
            Date = dto.Date,
            Notes = dto.Notes,
            SupplierId = dto.SupplierId,
            AuditorId = dto.AuditorId,
            OrderId = dto.OrderId,
            OriginalInvoiceId = dto.OriginalInvoiceId,
            Total = dto.Total,
            Discount = dto.Discount,
            NetTotal = dto.NetTotal,
            NetTotalCurrency = dto.NetTotalCurrency,
            Items = dto.Items.Select(i => new BuyInvoiceItem
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
            Payments = (dto.Payments ?? new()).Select(p => new SupplierPayment
            {
                Date = p.PaymentDate == default ? dto.Date : p.PaymentDate,
                SupplierId = dto.SupplierId,
                MoneySafeId = p.MoneySafeId,
                Amount = p.Amount,
                AmountCurrency = p.AmountCurrency,
                Notes = p.Notes ?? "",
                AuditorId = dto.AuditorId
            }).ToList(),
        };

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

            invoice.Number = await NextNumberAsync();   // after the lock, inside the transaction

            _dbContext.BuyInvoice.Add(invoice);
            await _dbContext.SaveChangesAsync();

            await dbTransaction.CommitAsync();
        });

        _logger.LogInformation("Buy invoice created with Id {InvoiceId} (Number {Number}, {ItemCount} items)",
            invoice.Id, invoice.Number, invoice.Items.Count);

        var created = await WithIncludes(_dbContext.BuyInvoice)
            .AsNoTracking()
            .FirstAsync(i => i.Id == invoice.Id);

        return ServiceResult<BuyInvoiceDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, BuyInvoiceDto dto, bool isSuperAdmin = false)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Invoice payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.BuyInvoice
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

        if (existing is null)
            return ServiceResult<bool>.NotFound("Invoice not found");

        if (existing.Type != InvoiceType.Buy)
            return ServiceResult<bool>.Failed("Use the return endpoints for Buy_Return invoices");

        var payments = await _dbContext.SupplierPayment
            .Where(p => !p.IsDeleted && p.InvoiceId == id)
            .Select(p => new { p.Amount, p.AmountCurrency })
            .ToListAsync();

        var hasReturns = await _dbContext.BuyInvoice
            .AnyAsync(r => !r.IsDeleted && r.OriginalInvoiceId == id);

        if (!isSuperAdmin && payments.Count > 0)
            return ServiceResult<bool>.Failed("This invoice has payments and can no longer be edited");
        if (!isSuperAdmin && hasReturns)
            return ServiceResult<bool>.Failed("This invoice has returns and can no longer be edited");

        // SuperAdmin on an invoice with returns: header only, items and totals stay as they are
        if (hasReturns)
        {
            existing.Date = dto.Date;
            existing.Notes = dto.Notes ?? "";
            await _dbContext.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        // SuperAdmin on an invoice with payments: the supplier is fixed, and totals can't drop below what was paid
        if (payments.Count > 0 && dto.SupplierId != existing.SupplierId)
            return ServiceResult<bool>.Failed("The supplier of an invoice with payments cannot be changed");

        var validationError = await ValidateAsync(dto, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating buy invoice {InvoiceId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        RecalculateTotals(dto);

        if (payments.Count > 0)
        {
            if (dto.NetTotal < payments.Sum(p => p.Amount))
                return ServiceResult<bool>.Failed("Net total cannot be less than the amount already paid");
            if (dto.NetTotalCurrency < payments.Sum(p => p.AmountCurrency))
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
            existing.SupplierId = dto.SupplierId;
            existing.AuditorId = dto.AuditorId;
            existing.OrderId = dto.OrderId;
            existing.OriginalInvoiceId = dto.OriginalInvoiceId;
            existing.Total = dto.Total;
            existing.Discount = dto.Discount;
            existing.NetTotal = dto.NetTotal;
            existing.NetTotalCurrency = dto.NetTotalCurrency;

            if (existing.Items is { Count: > 0 })
            {
                _dbContext.BuyInvoiceItem.RemoveRange(existing.Items);
            }

            var newItems = dto.Items.Select(i => new BuyInvoiceItem
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

            await _dbContext.BuyInvoiceItem.AddRangeAsync(newItems);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error occurred while updating buy invoice with Id {InvoiceId}", id);
                throw;
            }

            await dbTransaction.CommitAsync();
        });

        _logger.LogInformation("Buy invoice with Id {InvoiceId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id, bool isSuperAdmin = false)
    {
        var invoice = await _dbContext.BuyInvoice
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
        if (invoice is null)
            return ServiceResult<bool>.NotFound("Invoice not found");

        var payments = await _dbContext.SupplierPayment
            .Where(p => !p.IsDeleted && p.InvoiceId == id).ToListAsync();

        var returns = invoice.Type == InvoiceType.Buy
            ? await _dbContext.BuyInvoice.Include(r => r.Items)
                .Where(r => !r.IsDeleted && r.OriginalInvoiceId == id).ToListAsync()
            : new List<BuyInvoice>();

        if (!isSuperAdmin && (payments.Count > 0 || returns.Count > 0))
            return ServiceResult<bool>.Failed("Delete the payments and returns of this invoice first");

        var now = DateTime.UtcNow;

        foreach (var p in payments) { p.IsDeleted = true; p.DeletedAtUtc = now; }
        foreach (var r in returns)
        {
            foreach (var it in r.Items) { it.IsDeleted = true; it.DeletedAtUtc = now; }
            r.IsDeleted = true; r.DeletedAtUtc = now;
        }
        foreach (var it in invoice.Items) { it.IsDeleted = true; it.DeletedAtUtc = now; }
        invoice.IsDeleted = true; invoice.DeletedAtUtc = now;

        await _dbContext.SaveChangesAsync();   // one save: all or nothing

        _logger.LogWarning("Invoice {InvoiceId} deleted with {Payments} payments and {Returns} returns",
            id, payments.Count, returns.Count);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<List<BuyInvoiceDto>>> GetUnpaidBySupplierAsync(long supplierId)
    {
        if (supplierId <= 0)
            return ServiceResult<List<BuyInvoiceDto>>.Failed("A valid supplier Id is required");

        var invoices = await _dbContext.BuyInvoice
            .Where(i => !i.IsDeleted && i.SupplierId == supplierId)
            .Include(i => i.Payments!.Where(p => !p.IsDeleted))
            .Include(i => i.Returns.Where(r => !r.IsDeleted))
            .OrderBy(i => i.Date).ThenBy(i => i.Id)
            .AsNoTracking()
            .ToListAsync();

        var unpaid = invoices.Select(ToDto)
            .Where(d => d.RemainingAmount > 0 || d.RemainingAmountCurrency > 0)
            .ToList();
        return ServiceResult<List<BuyInvoiceDto>>.Ok(unpaid);
    }

    ///------ Returns
    ///

    public async Task<ServiceResult<List<BuyInvoiceDto>>> GetBuyBySupplierAsync(long supplierId)
    {
        if (supplierId <= 0)
            return ServiceResult<List<BuyInvoiceDto>>.Failed("A valid supplier Id is required");

        // a light list for the invoice picker (no items or payments)
        var invoices = await _dbContext.BuyInvoice
            .Where(i => !i.IsDeleted && i.SupplierId == supplierId && i.Type == InvoiceType.Buy)
            .Include(i => i.Supplier)
            .OrderByDescending(i => i.Date).ThenByDescending(i => i.Id)
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<BuyInvoiceDto>>.Ok(invoices.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<ReturnableInvoiceDto>> GetReturnableAsync(long invoiceId, long? excludeReturnId)
    {
        var original = await _dbContext.BuyInvoice
            .Where(i => !i.IsDeleted && i.Id == invoiceId && i.Type == InvoiceType.Buy)
            .Include(i => i.Supplier)
            .Include(i => i.Items!.Where(x => !x.IsDeleted)).ThenInclude(x => x.Product)
            .Include(i => i.Items!.Where(x => !x.IsDeleted)).ThenInclude(x => x.Store)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (original is null)
            return ServiceResult<ReturnableInvoiceDto>.NotFound("Buy invoice not found");

        var itemIds = original.Items.Select(i => i.Id).ToList();
        var returned = await ReturnedQuantitiesAsync(itemIds, excludeReturnId);

        return ServiceResult<ReturnableInvoiceDto>.Ok(new ReturnableInvoiceDto
        {
            InvoiceId = original.Id,
            Number = original.Number,
            Date = original.Date,
            SupplierId = original.SupplierId,
            SupplierName = original.Supplier?.Name ?? "",
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

    // quantity already returned per original item, optionally ignoring one return (when editing it)
    private async Task<Dictionary<long, decimal>> ReturnedQuantitiesAsync(List<long> itemIds, long? excludeReturnId)
    {
        return await _dbContext.BuyInvoiceItem
            .Where(r => !r.IsDeleted && r.OriginalItemId != null
                        && itemIds.Contains(r.OriginalItemId.Value)
                        && r.InvoiceId != (excludeReturnId ?? 0)
                        && !r.Invoice!.IsDeleted)
            .GroupBy(r => r.OriginalItemId!.Value)
            .Select(g => new { ItemId = g.Key, Qty = g.Sum(x => x.ProductQuantity) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Qty);
    }

    // validates the request and builds the return's items from the original invoice
    private async Task<(string? Error, BuyInvoice? Original, List<BuyInvoiceItem> Items)> PrepareReturnAsync(
        BuyReturnRequestDto req, long? currentId)
    {
        var items = new List<BuyInvoiceItem>();

        if (req is null) return ("Return payload is required", null, items);
        //if (req.Number <= 0) return ("Number is required and must be greater than zero", null, items);

        //var numberTaken = await _dbContext.BuyInvoice
        //    .AnyAsync(i => !i.IsDeleted && i.Number == req.Number && i.Id != (currentId ?? 0));
        //if (numberTaken) return ($"An invoice with number {req.Number} already exists", null, items);

        if (req.Date == default) return ("Date is required", null, items);

        if (!string.IsNullOrWhiteSpace(req.Notes) && req.Notes.Length > NotesMaxLength)
            return ($"Notes cannot exceed {NotesMaxLength} characters", null, items);

        var auditorExists = await _dbContext.Employee.AnyAsync(e => e.Id == req.AuditorId && !e.IsDeleted);
        if (!auditorExists) return ($"Auditor (Employee) with Id {req.AuditorId} was not found", null, items);

        var original = await _dbContext.BuyInvoice
            .Include(i => i.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == req.OriginalInvoiceId && !i.IsDeleted && i.Type == InvoiceType.Buy);
        if (original is null) return ("Original buy invoice not found", null, items);

        if (req.Items is null || req.Items.Count == 0)
            return ("At least one item is required", original, items);

        var ids = req.Items.Select(i => i.OriginalItemId).ToList();
        if (ids.Distinct().Count() != ids.Count)
            return ("An item can appear only once", original, items);

        var returned = await ReturnedQuantitiesAsync(ids, currentId);

        foreach (var ri in req.Items)
        {
            var orig = original.Items.FirstOrDefault(x => x.Id == ri.OriginalItemId && !x.IsDeleted);
            if (orig is null)
                return ($"Item {ri.OriginalItemId} does not belong to invoice {original.Number}", original, items);

            if (ri.Quantity <= 0)
                return ("Quantity must be greater than zero", original, items);

            var available = orig.ProductQuantity - returned.GetValueOrDefault(orig.Id);
            if (ri.Quantity > available)
                return ($"Quantity for product {orig.ProductId} exceeds the returnable quantity ({available})", original, items);

            var total = Math.Round(orig.ProductPrice * ri.Quantity, 2);
            var discount = orig.ProductQuantity > 0
                ? Math.Round(orig.ProductDiscount * ri.Quantity / orig.ProductQuantity, 2)
                : 0;

            items.Add(new BuyInvoiceItem
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

    private static void ApplyReturnTotals(BuyInvoice invoice, List<BuyInvoiceItem> items)
    {
        var egp = items.Where(i => i.PriceType == PriceType.Egyptian).ToList();
        var cur = items.Where(i => i.PriceType == PriceType.Currency).ToList();

        invoice.Total = Math.Round(egp.Sum(i => i.ProductTotal), 2);
        invoice.Discount = Math.Round(egp.Sum(i => i.ProductDiscount), 2);
        invoice.NetTotal = Math.Round(invoice.Total - invoice.Discount, 2);
        invoice.NetTotalCurrency = Math.Round(cur.Sum(i => i.ProductNetTotal), 2);
    }

    public async Task<ServiceResult<BuyInvoiceDto>> CreateReturnAsync(BuyReturnRequestDto req)
    {
        var (error, original, items) = await PrepareReturnAsync(req, null);
        if (error is not null)
        {
            _logger.LogWarning("Validation failed while creating buy return: {Error}", error);
            return ServiceResult<BuyInvoiceDto>.Failed(error);
        }

        var invoice = new BuyInvoice
        {
            //Number = req.Number,
            Type = InvoiceType.Buy_Return,
            Date = req.Date,
            Notes = req.Notes ?? "",
            SupplierId = original!.SupplierId,
            AuditorId = req.AuditorId,
            OrderId = original.OrderId,
            OriginalInvoiceId = original.Id,
            Items = items
        };
        ApplyReturnTotals(invoice, items);


        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var tx = await _dbContext.Database.BeginTransactionAsync();

            invoice.Number = await NextNumberAsync();

            _dbContext.BuyInvoice.Add(invoice);
            await _dbContext.SaveChangesAsync();

            await tx.CommitAsync();
        });
        //_dbContext.BuyInvoice.Add(invoice);
        //await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Buy return {InvoiceId} created against invoice {OriginalId}", invoice.Id, original.Id);

        var created = await WithIncludes(_dbContext.BuyInvoice)
            .AsNoTracking()
            .FirstAsync(i => i.Id == invoice.Id);

        return ServiceResult<BuyInvoiceDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateReturnAsync(long id, BuyReturnRequestDto req)
    {
        var existing = await _dbContext.BuyInvoice
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted && i.Type == InvoiceType.Buy_Return);

        if (existing is null)
            return ServiceResult<bool>.NotFound("Return invoice not found");

        if (req is not null && req.OriginalInvoiceId != existing.OriginalInvoiceId)
            return ServiceResult<bool>.Failed("The original invoice of a return cannot be changed");

        var (error, original, items) = await PrepareReturnAsync(req!, id);
        if (error is not null)
        {
            _logger.LogWarning("Validation failed while updating buy return {InvoiceId}: {Error}", id, error);
            return ServiceResult<bool>.Failed(error);
        }

        //existing.Number = req!.Number;
        existing.Date = req.Date;
        existing.Notes = req.Notes ?? "";
        existing.AuditorId = req.AuditorId;
        ApplyReturnTotals(existing, items);

        _dbContext.BuyInvoiceItem.RemoveRange(existing.Items);
        foreach (var item in items) item.InvoiceId = id;
        await _dbContext.BuyInvoiceItem.AddRangeAsync(items);

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Buy return {InvoiceId} updated", id);
        return ServiceResult<bool>.Ok(true);
    }


    // Excel
    public async Task<(byte[] Bytes, string FileName)?> ExportAsync(long id)
    {
        var invoice = await WithIncludes(_dbContext.BuyInvoice.Where(i => !i.IsDeleted && i.Id == id))
            .AsNoTracking().FirstOrDefaultAsync();
        if (invoice is null) return null;

        var d = ToDto(invoice);
        var safeIds = d.Payments.Select(p => p.MoneySafeId).Distinct().ToList();
        var safes = await _dbContext.MoneySafe.Where(m => safeIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Name);

        var isReturn = d.Type == InvoiceType.Buy_Return;
        var sheet = new InvoiceSheet
        {
            Title = isReturn ? "مرتجع شراء" : "فاتورة شراء",
            PartyLabel = "المورد",
            PartyName = d.SupplierName,
            Number = d.Number,
            Date = d.Date,
            Notes = d.Notes,
            Auditor = d.AuditorName,
            OriginalNumber = d.OriginalInvoiceNumber,
            OrderId = d.OrderId,
            Items = d.Items.Select(i => new InvoiceSheetItem(i.ProductName, i.StoreName, i.ProjectName,
                InvoiceExcel.PriceTypeLabel(i.PriceType), i.ProductPrice, i.ProductQuantity, i.ProductTotal,
                i.ProductDiscount, i.ProductNetTotal, i.ProductNotes)).ToList(),
            Payments = d.Payments.Select(p => new InvoiceSheetPayment(p.PaymentDate, "دفعة",
                safes.GetValueOrDefault(p.MoneySafeId, ""), p.Amount, p.AmountCurrency, p.Notes,
                string.IsNullOrEmpty(p.AuditorName) ? p.CreatedBy : p.AuditorName)).ToList(),
            Summary = new List<InvoiceSheetSummary>
        {
            new("الاجمالي", d.Total, 0),
            new("الخصم", d.Discount, 0),
            new("الصافي", d.NetTotal, d.NetTotalCurrency),
        }
        };

        if (!isReturn)
        {
            sheet.Summary.Add(new("المرتجعات", d.ReturnedAmount, d.ReturnedAmountCurrency));
            sheet.Summary.Add(new("المدفوع", d.PaidAmount, d.PaidAmountCurrency));
            sheet.Summary.Add(new("المتبقي", d.RemainingAmount, d.RemainingAmountCurrency));
        }

        return (InvoiceExcel.Build(sheet), $"buy-invoice-{d.Number}.xlsx");
    }
}