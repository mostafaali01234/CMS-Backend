using System.Linq.Expressions;
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;
using CMS.Domain.Enums;
using CMS.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Services;

// 1, 2, 3: plain list reports
public partial class ReportService
{
    private static string Ids(IEnumerable<long> ids) => string.Join("، ", ids);

    // ------------------------------------------------------------------ 1: unfinished orders that have sale invoices
    // "unfinished" = not done (تام), not cancelled (ملغي) and not deleted. A sale invoice linked to an order normally
    // closes it, so every row here is an order that was re-opened / never closed after its invoice was made.

    public async Task<ServiceResult<List<UnfinishedOrderRowDto>>> GetUnfinishedOrdersAsync(ReportDateFilterDto f)
    {
        var rangeError = CheckRange(f);
        if (rangeError is not null) return ServiceResult<List<UnfinishedOrderRowDto>>.Failed(rangeError);
        var (from, toExcl) = OrderRange(f);

        var q = _db.Order.AsNoTracking().Where(o => !o.IsDeleted
            && o.Status != OrderStatus.تام && o.Status != OrderStatus.ملغي && o.Status != OrderStatus.محذوف
            && _db.SaleInvoice.Any(i => i.OrderId == o.Id && !i.IsDeleted && i.Type == InvoiceType.Sale));
        if (from.HasValue) { var a = from.Value; q = q.Where(o => o.CreatedAtUtc >= a); }
        if (toExcl.HasValue) { var b = toExcl.Value; q = q.Where(o => o.CreatedAtUtc < b); }

        var rows = await q.OrderBy(o => o.CreatedAtUtc).ThenBy(o => o.Id).Select(o => new
        {
            o.Id,
            o.CreatedAtUtc,
            o.InstallationDate,
            o.Status,
            o.Type,
            o.Category,
            o.NetTotal,
            CustomerName = o.Customer!.Name,
            CustomerPhone = o.Customer!.Phone,
            CityName = o.City!.Name,
            StateName = o.City!.State!.Name,
            SellerName = o.Seller!.Name,
            TechName = o.TechHistory.Where(t => t.Status == TechStatus.current && t.TechType == OrderTechType.فني && !t.IsDeleted)
                         .OrderByDescending(t => t.Id).Select(t => t.Tech!.Name).FirstOrDefault(),
            Invoices = _db.SaleInvoice.Where(i => i.OrderId == o.Id && !i.IsDeleted && i.Type == InvoiceType.Sale)
                         .OrderBy(i => i.Number).Select(i => new { i.Number, i.NetTotal }).ToList()
        }).ToListAsync();

        var list = rows.Select(r => new UnfinishedOrderRowDto
        {
            OrderId = r.Id,
            CreatedAt = r.CreatedAtUtc.ToLocalTime(),
            InstallationDate = r.InstallationDate,
            Status = r.Status,
            Type = r.Type,
            Category = r.Category,
            CustomerName = r.CustomerName ?? "",
            CustomerPhone = r.CustomerPhone ?? "",
            StateName = r.StateName ?? "",
            CityName = r.CityName ?? "",
            SellerName = r.SellerName ?? "",
            TechName = r.TechName ?? "",
            OrderNetTotal = r.NetTotal,
            InvoicesCount = r.Invoices.Count,
            InvoiceNumbers = r.Invoices.Select(i => i.Number).ToList(),
            InvoicesNetTotal = r.Invoices.Sum(i => i.NetTotal)
        }).ToList();

        return ServiceResult<List<UnfinishedOrderRowDto>>.Ok(list);
    }

    public async Task<ExportResult> ExportUnfinishedOrdersAsync(ReportDateFilterDto f)
    {
        var r = await GetUnfinishedOrdersAsync(f);
        if (!r.Succeeded) return Fail(r.Errors);
        var d = r.Data!;

        var cols = new List<XCol>
        {
            new("رقم الاوردر", 11, null, true), new("تاريخ الانشاء", 13, TableExcel.Day, true),
            new("تاريخ التركيب", 13, TableExcel.Day, true), new("الحالة", 14, null, true), new("النوع", 11, null, true),
            new("العميل", 24), new("الموبايل", 15, null, true), new("المحافظة", 15), new("المدينة", 15),
            new("البائع", 18), new("الفني", 18), new("صافي الاوردر", 15, TableExcel.Money),
            new("عدد الفواتير", 10, null, true), new("ارقام الفواتير", 20, null, true, true), new("اجمالي الفواتير", 15, TableExcel.Money),
        };
        var rows = d.Select(x => new object?[]
        {
            x.OrderId, x.CreatedAt, x.InstallationDate, Label(x.Status), Label(x.Type),
            x.CustomerName, x.CustomerPhone, x.StateName, x.CityName, x.SellerName, x.TechName, x.OrderNetTotal,
            x.InvoicesCount, Ids(x.InvoiceNumbers), x.InvoicesNetTotal
        }).ToList();
        var totals = new object?[]
        {
            null, null, null, null, null, null, null, null, null, null, null,
            d.Sum(x => x.OrderNetTotal), d.Sum(x => x.InvoicesCount), null, d.Sum(x => x.InvoicesNetTotal)
        };

        var bytes = TableExcel.Build("اوردرات غير تامة", "اوردرات غير تامة لها فواتير بيع",
            Sub(Period(f.DateFrom, f.DateTo), $"{d.Count} اوردر — تاريخ الانشاء"), cols, rows, totals);
        return new ExportResult(bytes, null, $"unfinished-orders-{Stamp}.xlsx");
    }

    // ------------------------------------------------------------------ 2: orders with an advance payment that is not on an invoice
    // an advance payment is "pending" while it has no invoice (it is attached to the order's next sale invoice)

    public async Task<ServiceResult<List<AdvanceOrderRowDto>>> GetAdvanceOrdersAsync(ReportDateFilterDto f)
    {
        var rangeError = CheckRange(f);
        if (rangeError is not null) return ServiceResult<List<AdvanceOrderRowDto>>.Failed(rangeError);
        var (from, toExcl) = OrderRange(f);

        var advances = await _db.CustomerPayment.AsNoTracking()
            .Where(p => !p.IsDeleted && p.Type == PaymentType.Advance_Payment && p.InvoiceId == null && p.AdvancePaymentOrderId != null)
            .GroupBy(p => p.AdvancePaymentOrderId!.Value)
            .Select(g => new
            {
                OrderId = g.Key,
                Count = g.Count(),
                Amount = g.Sum(p => p.Amount),
                AmountCurrency = g.Sum(p => p.AmountCurrency),
                First = g.Min(p => p.Date),
                Last = g.Max(p => p.Date)
            }).ToListAsync();

        var ids = advances.Select(a => a.OrderId).ToList();
        var oq = _db.Order.AsNoTracking().Where(o => !o.IsDeleted && ids.Contains(o.Id));
        if (from.HasValue) { var a = from.Value; oq = oq.Where(o => o.CreatedAtUtc >= a); }
        if (toExcl.HasValue) { var b = toExcl.Value; oq = oq.Where(o => o.CreatedAtUtc < b); }

        var orders = await oq.Select(o => new
        {
            o.Id,
            o.CreatedAtUtc,
            o.Status,
            o.Type,
            o.NetTotal,
            CustomerName = o.Customer!.Name,
            CustomerPhone = o.Customer!.Phone,
            StateName = o.City!.State!.Name,
            SellerName = o.Seller!.Name,
            SaleInvoices = _db.SaleInvoice.Count(i => i.OrderId == o.Id && !i.IsDeleted && i.Type == InvoiceType.Sale)
        }).ToListAsync();

        var byOrder = advances.ToDictionary(a => a.OrderId);
        var list = orders.OrderBy(o => o.CreatedAtUtc).ThenBy(o => o.Id).Select(o =>
        {
            var a = byOrder[o.Id];
            return new AdvanceOrderRowDto
            {
                OrderId = o.Id,
                CreatedAt = o.CreatedAtUtc.ToLocalTime(),
                Status = o.Status,
                Type = o.Type,
                CustomerName = o.CustomerName ?? "",
                CustomerPhone = o.CustomerPhone ?? "",
                StateName = o.StateName ?? "",
                SellerName = o.SellerName ?? "",
                OrderNetTotal = o.NetTotal,
                AdvanceCount = a.Count,
                AdvanceTotal = a.Amount,
                AdvanceTotalCurrency = a.AmountCurrency,
                FirstAdvanceDate = a.First,
                LastAdvanceDate = a.Last,
                SaleInvoicesCount = o.SaleInvoices
            };
        }).ToList();

        return ServiceResult<List<AdvanceOrderRowDto>>.Ok(list);
    }

    public async Task<ExportResult> ExportAdvanceOrdersAsync(ReportDateFilterDto f)
    {
        var r = await GetAdvanceOrdersAsync(f);
        if (!r.Succeeded) return Fail(r.Errors);
        var d = r.Data!;

        var cols = new List<XCol>
        {
            new("رقم الاوردر", 11, null, true), new("تاريخ الانشاء", 13, TableExcel.Day, true), new("الحالة", 14, null, true),
            new("النوع", 11, null, true), new("العميل", 24), new("الموبايل", 15, null, true), new("المحافظة", 15), new("البائع", 18),
            new("صافي الاوردر", 15, TableExcel.Money), new("عدد الدفعات", 10, null, true),
            new("اجمالي الدفعات المقدمة", 17, TableExcel.Money), new("اجمالي الدفعات (عملة)", 17, TableExcel.Money),
            new("اول دفعة", 13, TableExcel.Day, true), new("اخر دفعة", 13, TableExcel.Day, true), new("فواتير بيع للاوردر", 12, null, true),
        };
        var rows = d.Select(x => new object?[]
        {
            x.OrderId, x.CreatedAt, Label(x.Status), Label(x.Type), x.CustomerName, x.CustomerPhone, x.StateName, x.SellerName,
            x.OrderNetTotal, x.AdvanceCount, x.AdvanceTotal, x.AdvanceTotalCurrency, x.FirstAdvanceDate, x.LastAdvanceDate, x.SaleInvoicesCount
        }).ToList();
        var totals = new object?[]
        {
            null, null, null, null, null, null, null, null,
            d.Sum(x => x.OrderNetTotal), d.Sum(x => x.AdvanceCount), d.Sum(x => x.AdvanceTotal), d.Sum(x => x.AdvanceTotalCurrency), null, null, null
        };

        var bytes = TableExcel.Build("دفعات مقدمة بدون فاتورة", "اوردرات بها دفعات مقدمة بدون فاتورة",
            Sub(Period(f.DateFrom, f.DateTo), $"{d.Count} اوردر — تاريخ الانشاء"), cols, rows, totals);
        return new ExportResult(bytes, null, $"advance-orders-{Stamp}.xlsx");
    }

    // ------------------------------------------------------------------ 3: unpaid sale invoices
    // same rule as the invoice list's "unpaid" filter: net - returns - payments (refunds are not payments) > 0, in EGP or in currency

    private static readonly Expression<Func<SaleInvoice, bool>> IsUnpaid = i =>
        i.Type == InvoiceType.Sale &&
        (i.NetTotal - i.Returns.Where(r => !r.IsDeleted).Sum(r => r.NetTotal)
                - i.Payments.Where(p => !p.IsDeleted && p.Type != PaymentType.Refund_Payment).Sum(p => p.Amount) > 0
        || i.NetTotalCurrency - i.Returns.Where(r => !r.IsDeleted).Sum(r => r.NetTotalCurrency)
                        - i.Payments.Where(p => !p.IsDeleted && p.Type != PaymentType.Refund_Payment).Sum(p => p.AmountCurrency) > 0);

    public async Task<ServiceResult<List<UnpaidInvoiceRowDto>>> GetUnpaidInvoicesAsync(ReportDateFilterDto f)
    {
        var rangeError = CheckRange(f);
        if (rangeError is not null) return ServiceResult<List<UnpaidInvoiceRowDto>>.Failed(rangeError);
        var (from, toExcl) = InvoiceRange(f);

        var q = _db.SaleInvoice.AsNoTracking().Where(i => !i.IsDeleted).Where(IsUnpaid);
        if (from.HasValue) { var a = from.Value; q = q.Where(i => i.Date >= a); }
        if (toExcl.HasValue) { var b = toExcl.Value; q = q.Where(i => i.Date < b); }

        var rows = await q.OrderBy(i => i.Date).ThenBy(i => i.Id).Select(i => new
        {
            i.Id,
            i.Number,
            i.Date,
            i.CustomerId,
            CustomerName = i.Customer!.Name,
            CustomerPhone = i.Customer!.Phone,
            i.OrderId,
            SellerName = i.Order!.Seller!.Name,
            i.NetTotal,
            i.NetTotalCurrency,
            Paid = i.Payments.Where(p => !p.IsDeleted && p.Type != PaymentType.Refund_Payment).Sum(p => p.Amount),
            PaidCur = i.Payments.Where(p => !p.IsDeleted && p.Type != PaymentType.Refund_Payment).Sum(p => p.AmountCurrency),
            Ret = i.Returns.Where(x => !x.IsDeleted).Sum(x => x.NetTotal),
            RetCur = i.Returns.Where(x => !x.IsDeleted).Sum(x => x.NetTotalCurrency)
        }).ToListAsync();

        var today = DateTime.Today;
        var list = rows.Select(r => new UnpaidInvoiceRowDto
        {
            InvoiceId = r.Id,
            Number = r.Number,
            Date = r.Date,
            AgeDays = Math.Max(0, (today - r.Date.Date).Days),
            CustomerId = r.CustomerId,
            CustomerName = r.CustomerName ?? "",
            CustomerPhone = r.CustomerPhone ?? "",
            OrderId = r.OrderId,
            SellerName = r.SellerName ?? "",
            NetTotal = r.NetTotal,
            NetTotalCurrency = r.NetTotalCurrency,
            ReturnedTotal = r.Ret,
            PaidTotal = r.Paid,
            Remaining = Math.Max(0, r.NetTotal - r.Ret - r.Paid),
            RemainingCurrency = Math.Max(0, r.NetTotalCurrency - r.RetCur - r.PaidCur)
        }).Where(x => x.Remaining > 0 || x.RemainingCurrency > 0).ToList();

        return ServiceResult<List<UnpaidInvoiceRowDto>>.Ok(list);
    }

    public async Task<ExportResult> ExportUnpaidInvoicesAsync(ReportDateFilterDto f)
    {
        var r = await GetUnpaidInvoicesAsync(f);
        if (!r.Succeeded) return Fail(r.Errors);
        var d = r.Data!;

        var cols = new List<XCol>
        {
            new("رقم الفاتورة", 12, null, true), new("التاريخ", 13, TableExcel.Day, true), new("العمر (يوم)", 10, null, true),
            new("العميل", 26), new("الموبايل", 15, null, true), new("رقم الاوردر", 11, null, true), new("البائع", 18),
            new("صافي الفاتورة", 15, TableExcel.Money), new("المرتجع", 14, TableExcel.Money), new("المسدد", 14, TableExcel.Money),
            new("المتبقي", 15, TableExcel.Money), new("صافي (عملة)", 14, TableExcel.Money), new("المتبقي (عملة)", 14, TableExcel.Money),
        };
        var rows = d.Select(x => new object?[]
        {
            x.Number, x.Date, x.AgeDays, x.CustomerName, x.CustomerPhone, x.OrderId, x.SellerName,
            x.NetTotal, x.ReturnedTotal, x.PaidTotal, x.Remaining, x.NetTotalCurrency, x.RemainingCurrency
        }).ToList();
        var totals = new object?[]
        {
            null, null, null, null, null, null, null,
            d.Sum(x => x.NetTotal), d.Sum(x => x.ReturnedTotal), d.Sum(x => x.PaidTotal), d.Sum(x => x.Remaining),
            d.Sum(x => x.NetTotalCurrency), d.Sum(x => x.RemainingCurrency)
        };

        var bytes = TableExcel.Build("فواتير غير مسددة", "فواتير البيع غير المسددة",
            Sub(Period(f.DateFrom, f.DateTo), $"{d.Count} فاتورة — تاريخ الفاتورة"), cols, rows, totals);
        return new ExportResult(bytes, null, $"unpaid-invoices-{Stamp}.xlsx");
    }
}
