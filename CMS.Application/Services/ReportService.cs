using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;
using CMS.Application.Interfaces;
using CMS.Application.Interfaces.Configuration;
using CMS.Domain.Enums;
using CMS.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Services;

// rows read from the database for the state / seller / tech x category reports
internal sealed class ReportItemRow
{
    public long CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public decimal Net { get; set; }
    public bool IsEgp { get; set; }          // false = priced in the foreign currency (not part of the EGP totals)
}

internal sealed class ReportDocRow
{
    public long Id { get; set; }
    public int Sign { get; set; } = 1;       // -1 for sale returns
    public long? StateId { get; set; }
    public string? StateName { get; set; }
    public int? StateSort { get; set; }
    public long? SellerId { get; set; }
    public string? SellerName { get; set; }
    public long? TechId { get; set; }
    public string? TechName { get; set; }
    public List<ReportItemRow> Items { get; set; } = new();
}

/// <summary>
/// Reports. Shared helpers and the "rows x product category" reports (4 - 10) live here;
/// the three list reports are in ReportService.Lists.cs and the maintenance report in ReportService.Maintenance.cs.
/// </summary>
public partial class ReportService : IReportService
{
    private readonly IAppDbContext _db;
    public ReportService(IAppDbContext db) { _db = db; }

    private const string NoCategory = "بدون تصنيف";
    private const string MoneyDash = "#,##0.00;[Red]-#,##0.00;\"-\"";
    private static string Stamp => DateTime.Now.ToString("yyyyMMdd-HHmm");

    // ------------------------------------------------------------------ dates

    private static string? CheckRange(ReportDateFilterDto f) =>
        f.DateFrom.HasValue && f.DateTo.HasValue && f.DateTo.Value.Date < f.DateFrom.Value.Date
            ? "تاريخ النهاية قبل البداية" : null;

    // orders keep CreatedAtUtc in UTC: a local day range -> [from, toExclusive) in UTC (same as the order stats)
    private static (DateTime? From, DateTime? ToExcl) OrderRange(ReportDateFilterDto f) =>
        (f.DateFrom.HasValue ? DateTime.SpecifyKind(f.DateFrom.Value.Date, DateTimeKind.Local).ToUniversalTime() : (DateTime?)null,
         f.DateTo.HasValue ? DateTime.SpecifyKind(f.DateTo.Value.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime() : (DateTime?)null);

    // invoices keep a plain business date
    private static (DateTime? From, DateTime? ToExcl) InvoiceRange(ReportDateFilterDto f) =>
        (f.DateFrom?.Date, f.DateTo?.Date.AddDays(1));

    private static string Period(DateTime? from, DateTime? to) =>
        from.HasValue || to.HasValue
            ? $"الفترة: {from?.ToString("yyyy-MM-dd") ?? "..."} الى {to?.ToString("yyyy-MM-dd") ?? "..."}"
            : "كل الفترات";

    private static string Sub(string period, string? extra = null) =>
        $"{period}{(string.IsNullOrWhiteSpace(extra) ? "" : " — " + extra)} — {DateTime.Now:yyyy-MM-dd HH:mm}";

    private static string Label(Enum e) => e.ToString().Replace('_', ' ');
    private static ExportResult Fail(IEnumerable<string> errors) => new(null, string.Join("; ", errors), "");

    // ------------------------------------------------------------------ order kind filter

    private sealed record KindFilter(string Mode, OrderType? Type);

    // "" / all -> every order | regular -> not maintenance | maintenance -> maintenance only | <OrderType> -> regular orders of that type
    private static bool TryParseKind(string? kind, out KindFilter k)
    {
        k = new KindFilter("all", null);
        if (string.IsNullOrWhiteSpace(kind) || kind.Trim().Equals("all", StringComparison.OrdinalIgnoreCase)) return true;

        var s = kind.Trim();
        if (s.Equals("regular", StringComparison.OrdinalIgnoreCase)) { k = new KindFilter("regular", null); return true; }
        if (s.Equals("maintenance", StringComparison.OrdinalIgnoreCase)) { k = new KindFilter("maintenance", null); return true; }
        if (Enum.TryParse<OrderType>(s, out var t) && Enum.IsDefined(typeof(OrderType), t)) { k = new KindFilter("type", t); return true; }
        return false;
    }

    private static string KindLabel(KindFilter k) => k.Mode switch
    {
        "regular" => "بدون الصيانة",
        "maintenance" => "صيانة فقط",
        "type" => "النوع: " + Label(k.Type!.Value),
        _ => "كل الانواع"
    };

    private static IQueryable<Order> ApplyKind(IQueryable<Order> q, KindFilter k)
    {
        switch (k.Mode)
        {
            case "regular": return q.Where(o => o.OldOrderId == null);
            case "maintenance": return q.Where(o => o.OldOrderId != null);
            case "type":
                {
                    var t = k.Type!.Value;
                    return q.Where(o => o.OldOrderId == null && o.Type == t);
                }
            default: return q;
        }
    }

    // an invoice takes the kind of its order; an invoice without an order is "regular"
    private static IQueryable<SaleInvoice> ApplyKind(IQueryable<SaleInvoice> q, KindFilter k)
    {
        switch (k.Mode)
        {
            case "regular": return q.Where(i => i.Order == null || i.Order.OldOrderId == null);
            case "maintenance": return q.Where(i => i.Order != null && i.Order.OldOrderId != null);
            case "type":
                {
                    var t = k.Type!.Value;
                    return q.Where(i => i.Order != null && i.Order.OldOrderId == null && i.Order.Type == t);
                }
            default: return q;
        }
    }

    private static bool TryParseMeasure(string? measure, out bool amount)
    {
        amount = false;
        if (string.IsNullOrWhiteSpace(measure) || measure.Trim().Equals("count", StringComparison.OrdinalIgnoreCase)) return true;
        if (measure.Trim().Equals("amount", StringComparison.OrdinalIgnoreCase)) { amount = true; return true; }
        return false;
    }

    // ------------------------------------------------------------------ 5 - 10: state / seller / tech x product category

    private sealed record RowKey(long Id, string Name, int Sort);
    private sealed record Fact(RowKey Row, long CatId, string CatName, decimal Value);

    public async Task<ServiceResult<CrossTabDto>> GetCategoryMatrixAsync(ReportSource source, ReportRowBy rowBy, CategoryMatrixFilterDto f)
    {
        var rangeError = CheckRange(f);
        if (rangeError is not null) return ServiceResult<CrossTabDto>.Failed(rangeError);
        if (!TryParseKind(f.Kind, out var kind)) return ServiceResult<CrossTabDto>.Failed("نوع الاوردر غير صحيح");
        if (!TryParseMeasure(f.Measure, out var amount)) return ServiceResult<CrossTabDto>.Failed("طريقة الحساب غير صحيحة");

        var docs = source == ReportSource.Orders
            ? await LoadOrderDocsAsync(f, kind)
            : await LoadInvoiceDocsAsync(f, kind, amount);

        var what = source == ReportSource.Orders ? "اوردرات" : "فواتير البيع";
        var (per, rowLabel) = rowBy switch
        {
            ReportRowBy.State => ("بالمحافظات", "المحافظة"),
            ReportRowBy.Seller => ("لكل بائع", "البائع"),
            _ => ("لكل فني", "الفني")
        };

        var dto = BuildCross(ToFacts(docs, rowBy, amount), amount);
        dto.Title = $"{what} {per} حسب تصنيف المنتجات";
        dto.RowLabel = rowLabel;
        dto.DateFrom = f.DateFrom?.Date;
        dto.DateTo = f.DateTo?.Date;
        return ServiceResult<CrossTabDto>.Ok(dto);
    }

    private async Task<List<ReportDocRow>> LoadOrderDocsAsync(CategoryMatrixFilterDto f, KindFilter kind)
    {
        var (from, toExcl) = OrderRange(f);

        var q = _db.Order.AsNoTracking().Where(o => !o.IsDeleted && o.Status != OrderStatus.محذوف);
        if (!f.IncludeCancelled) q = q.Where(o => o.Status != OrderStatus.ملغي);
        if (from.HasValue) { var a = from.Value; q = q.Where(o => o.CreatedAtUtc >= a); }
        if (toExcl.HasValue) { var b = toExcl.Value; q = q.Where(o => o.CreatedAtUtc < b); }
        q = ApplyKind(q, kind);
        if (f.SellerId is > 0) { var sid = f.SellerId.Value; q = q.Where(o => o.SellerId == sid); }

        return await q.Select(o => new ReportDocRow
        {
            Id = o.Id,
            Sign = 1,
            StateId = (long?)o.City!.StateId,
            StateName = o.City!.State!.Name,
            StateSort = (int?)o.City!.State!.OrderNumber,
            SellerId = (long?)o.SellerId,
            SellerName = o.Seller!.Name,
            TechId = o.TechHistory.Where(t => t.Status == TechStatus.current && t.TechType == OrderTechType.فني && !t.IsDeleted)
                        .OrderByDescending(t => t.Id).Select(t => (long?)t.TechId).FirstOrDefault(),
            TechName = o.TechHistory.Where(t => t.Status == TechStatus.current && t.TechType == OrderTechType.فني && !t.IsDeleted)
                        .OrderByDescending(t => t.Id).Select(t => t.Tech!.Name).FirstOrDefault(),
            Items = o.Items.Where(i => !i.IsDeleted)
                        .Select(i => new ReportItemRow
                        {
                            CategoryId = i.Product!.CategoryId,
                            CategoryName = i.Product.Category!.Name,
                            Net = i.ProductNetTotal,
                            IsEgp = true
                        }).ToList()
        }).ToListAsync();
    }

    // seller and tech of an invoice are the ones of its order (same as the commissions); the state falls back to the customer's
    private async Task<List<ReportDocRow>> LoadInvoiceDocsAsync(CategoryMatrixFilterDto f, KindFilter kind, bool amount)
    {
        var (from, toExcl) = InvoiceRange(f);

        var q = _db.SaleInvoice.AsNoTracking().Where(i => !i.IsDeleted);
        q = amount
            ? q.Where(i => i.Type == InvoiceType.Sale || i.Type == InvoiceType.Sale_Return)   // returns are taken off the amounts
            : q.Where(i => i.Type == InvoiceType.Sale);
        if (from.HasValue) { var a = from.Value; q = q.Where(i => i.Date >= a); }
        if (toExcl.HasValue) { var b = toExcl.Value; q = q.Where(i => i.Date < b); }
        q = ApplyKind(q, kind);
        if (f.SellerId is > 0) { var sid = f.SellerId.Value; q = q.Where(i => i.Order != null && i.Order.SellerId == sid); }

        return await q.Select(i => new ReportDocRow
        {
            Id = i.Id,
            Sign = i.Type == InvoiceType.Sale_Return ? -1 : 1,
            StateId = (long?)i.Order!.City!.StateId ?? (long?)i.Customer!.City!.StateId,
            StateName = i.Order!.City!.State!.Name ?? i.Customer!.City!.State!.Name,
            StateSort = (int?)i.Order!.City!.State!.OrderNumber ?? (int?)i.Customer!.City!.State!.OrderNumber,
            SellerId = (long?)i.Order!.SellerId,
            SellerName = i.Order!.Seller!.Name,
            TechId = i.Order!.TechHistory.Where(t => t.Status == TechStatus.current && t.TechType == OrderTechType.فني && !t.IsDeleted)
                        .OrderByDescending(t => t.Id).Select(t => (long?)t.TechId).FirstOrDefault(),
            TechName = i.Order!.TechHistory.Where(t => t.Status == TechStatus.current && t.TechType == OrderTechType.فني && !t.IsDeleted)
                        .OrderByDescending(t => t.Id).Select(t => t.Tech!.Name).FirstOrDefault(),
            Items = i.Items.Where(x => !x.IsDeleted)
                        .Select(x => new ReportItemRow
                        {
                            CategoryId = x.Product!.CategoryId,
                            CategoryName = x.Product.Category!.Name,
                            Net = x.ProductNetTotal,
                            IsEgp = x.PriceType == PriceType.Egyptian
                        }).ToList()
        }).ToListAsync();
    }

    private static RowKey KeyOf(ReportDocRow d, ReportRowBy by) => by switch
    {
        ReportRowBy.State => d.StateId is { } st
            ? new RowKey(st, d.StateName ?? "", d.StateSort ?? int.MaxValue - 1)
            : new RowKey(0, "بدون محافظة", int.MaxValue),
        ReportRowBy.Seller => d.SellerId is { } sl
            ? new RowKey(sl, d.SellerName ?? "", 0)
            : new RowKey(0, "بدون بائع (فواتير بدون اوردر)", int.MaxValue),
        _ => d.TechId is { } te && te > 0
            ? new RowKey(te, d.TechName ?? "", 0)
            : new RowKey(0, "بدون فني", int.MaxValue)
    };

    // count: the order / invoice is counted once, under the category of its biggest item (the system's own "items category" rule)
    // amount: every item's net total goes to its own category (EGP items only; sale returns are negative)
    private static IEnumerable<Fact> ToFacts(IEnumerable<ReportDocRow> docs, ReportRowBy by, bool amount)
    {
        foreach (var d in docs)
        {
            var key = KeyOf(d, by);
            if (amount)
            {
                foreach (var it in d.Items.Where(x => x.IsEgp))
                    yield return new Fact(key, it.CategoryId, it.CategoryName ?? NoCategory, d.Sign * it.Net);
            }
            else
            {
                var top = d.Items.OrderByDescending(x => x.IsEgp).ThenByDescending(x => x.Net).ThenBy(x => x.CategoryId).FirstOrDefault();
                yield return new Fact(key, top?.CategoryId ?? 0, top?.CategoryName ?? NoCategory, 1);
            }
        }
    }

    private static CrossTabDto BuildCross(IEnumerable<Fact> facts, bool amount)
    {
        var list = facts.ToList();
        var dto = new CrossTabDto { Measure = amount ? "amount" : "count" };

        var cats = list.GroupBy(x => x.CatId)
            .Select(g => new { Id = g.Key, Name = g.First().CatName })
            .OrderBy(c => c.Id == 0 ? 1 : 0).ThenBy(c => c.Name, StringComparer.Ordinal)
            .ToList();
        var index = new Dictionary<long, int>();
        for (var i = 0; i < cats.Count; i++) index[cats[i].Id] = i;
        dto.Columns = cats.Select(c => c.Name).ToList();

        foreach (var g in list.GroupBy(x => x.Row).OrderBy(k => k.Key.Sort).ThenBy(k => k.Key.Name, StringComparer.Ordinal))
        {
            var values = new decimal[cats.Count];
            foreach (var x in g) values[index[x.CatId]] += x.Value;
            AddRow(dto, g.Key.Id, g.Key.Name, values);
        }
        Totals(dto);
        return dto;
    }

    private static void AddRow(CrossTabDto dto, long id, string name, decimal[] values)
    {
        var v = values.Select(x => Math.Round(x, 2)).ToList();
        dto.Rows.Add(new CrossTabRowDto { Id = id, Name = name, Values = v, Total = v.Sum() });
    }

    private static void Totals(CrossTabDto dto)
    {
        dto.ColumnTotals = dto.Columns.Select((_, i) => dto.Rows.Sum(r => r.Values[i])).ToList();
        dto.GrandTotal = dto.Rows.Sum(r => r.Total);
    }

    public async Task<ExportResult> ExportCategoryMatrixAsync(ReportSource source, ReportRowBy rowBy, CategoryMatrixFilterDto f)
    {
        var r = await GetCategoryMatrixAsync(source, rowBy, f);
        if (!r.Succeeded) return Fail(r.Errors);
        var d = r.Data!;

        TryParseKind(f.Kind, out var kind);
        var extra = new List<string> { KindLabel(kind), d.Measure == "amount" ? "صافي القيمة بالجنيه" : (source == ReportSource.Orders ? "عدد الاوردرات" : "عدد الفواتير") };
        if (f.SellerId is > 0)
        {
            var sid = f.SellerId.Value;
            var name = await _db.Employee.Where(e => e.Id == sid).Select(e => e.Name).FirstOrDefaultAsync();
            extra.Add("البائع: " + (name ?? sid.ToString()));
        }
        if (source == ReportSource.Orders && f.IncludeCancelled) extra.Add("شامل الملغي");

        var sheet = (source == ReportSource.Orders ? "اوردرات" : "فواتير") + " - " + d.RowLabel;
        var slug = $"{source}-by-{rowBy}".ToLowerInvariant();
        return CrossTabFile(d, sheet, slug, string.Join(" — ", extra));
    }

    private static ExportResult CrossTabFile(CrossTabDto d, string sheet, string slug, string? extra)
    {
        var fmt = d.Measure == "amount" ? MoneyDash : TableExcel.QtyDash;

        var cols = new List<XCol> { new(d.RowLabel, 30) };
        foreach (var c in d.Columns) cols.Add(new XCol(c, 16, fmt, true));
        cols.Add(new XCol("الاجمالي", 16, fmt, true));

        var rows = new List<object?[]>();
        foreach (var r in d.Rows)
        {
            var a = new object?[d.Columns.Count + 2];
            a[0] = r.Name;
            for (var i = 0; i < r.Values.Count; i++) a[i + 1] = r.Values[i];
            a[a.Length - 1] = r.Total;
            rows.Add(a);
        }

        var totals = new object?[d.Columns.Count + 2];
        for (var i = 0; i < d.ColumnTotals.Count; i++) totals[i + 1] = d.ColumnTotals[i];
        totals[totals.Length - 1] = d.GrandTotal;

        var bytes = TableExcel.Build(sheet, d.Title, Sub(Period(d.DateFrom, d.DateTo), extra), cols, rows, totals);
        return new ExportResult(bytes, null, $"{slug}-{Stamp}.xlsx");
    }

    // ------------------------------------------------------------------ 4: monthly sales per seller

    private static readonly string[] MonthNames =
    {
        "يناير", "فبراير", "مارس", "ابريل", "مايو", "يونيو",
        "يوليو", "اغسطس", "سبتمبر", "اكتوبر", "نوفمبر", "ديسمبر"
    };

    // net sales (sale invoices minus sale returns, EGP) of each seller by invoice date; the seller is the one of the invoice's order
    public async Task<ServiceResult<CrossTabDto>> GetMonthlySalesAsync(MonthlySalesFilterDto f)
    {
        var year = f.Year ?? DateTime.Today.Year;
        if (year < 2000 || year > 2100) return ServiceResult<CrossTabDto>.Failed("السنة غير صحيحة");

        var half = string.IsNullOrWhiteSpace(f.Half) ? "first" : f.Half.Trim().ToLowerInvariant();
        if (half != "first" && half != "last") return ServiceResult<CrossTabDto>.Failed("الفترة غير صحيحة (first / last)");

        var firstMonth = half == "last" ? 7 : 1;
        var from = new DateTime(year, firstMonth, 1);
        var toExcl = from.AddMonths(6);

        var q = _db.SaleInvoice.AsNoTracking()
            .Where(i => !i.IsDeleted && (i.Type == InvoiceType.Sale || i.Type == InvoiceType.Sale_Return)
                        && i.Date >= from && i.Date < toExcl);
        if (f.SellerId is > 0) { var sid = f.SellerId.Value; q = q.Where(i => i.Order != null && i.Order.SellerId == sid); }

        var rows = await q.Select(i => new
        {
            i.Date,
            i.Type,
            i.NetTotal,
            SellerId = (long?)i.Order!.SellerId,
            SellerName = i.Order!.Seller!.Name
        }).ToListAsync();

        var dto = new CrossTabDto
        {
            Title = $"المبيعات الشهرية {year} — " + (half == "last" ? "الشهور الستة الاخيرة" : "الشهور الستة الاولى"),
            RowLabel = "البائع",
            Measure = "amount",
            DateFrom = from,
            DateTo = toExcl.AddDays(-1)
        };
        for (var m = 0; m < 6; m++) dto.Columns.Add(MonthNames[firstMonth - 1 + m]);

        var bySeller = rows.GroupBy(r => r.SellerId ?? 0).ToList();

        // a chosen seller always gets a row, even with no sales
        if (f.SellerId is > 0 && bySeller.All(g => g.Key != f.SellerId.Value))
        {
            var sid = f.SellerId.Value;
            var name = await _db.Employee.Where(e => e.Id == sid).Select(e => e.Name).FirstOrDefaultAsync();
            AddRow(dto, sid, name ?? sid.ToString(), new decimal[6]);
        }

        foreach (var g in bySeller
                     .OrderBy(k => k.Key == 0 ? 1 : 0)
                     .ThenBy(k => k.Select(x => x.SellerName).FirstOrDefault(n => !string.IsNullOrEmpty(n)) ?? "", StringComparer.Ordinal))
        {
            var values = new decimal[6];
            foreach (var r in g) values[r.Date.Month - firstMonth] += (r.Type == InvoiceType.Sale_Return ? -1 : 1) * r.NetTotal;
            var name = g.Key == 0 ? "بدون بائع (فواتير بدون اوردر)" : g.Select(x => x.SellerName).FirstOrDefault(n => !string.IsNullOrEmpty(n)) ?? g.Key.ToString();
            AddRow(dto, g.Key, name, values);
        }

        Totals(dto);
        return ServiceResult<CrossTabDto>.Ok(dto);
    }

    public async Task<ExportResult> ExportMonthlySalesAsync(MonthlySalesFilterDto f)
    {
        var r = await GetMonthlySalesAsync(f);
        if (!r.Succeeded) return Fail(r.Errors);

        var extra = "صافي المبيعات بالجنيه (المبيعات - المرتجعات)";
        if (f.SellerId is > 0)
        {
            var sid = f.SellerId.Value;
            var name = await _db.Employee.Where(e => e.Id == sid).Select(e => e.Name).FirstOrDefaultAsync();
            extra += " — البائع: " + (name ?? sid.ToString());
        }
        return CrossTabFile(r.Data!, "المبيعات الشهرية", "monthly-sales", extra);
    }
}
