using ClosedXML.Excel;
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;
using CMS.Application.Interfaces;
using CMS.Application.Interfaces.Configuration;
using CMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Services;

public class MoneySafeMoveService : IMoneySafeMoveService
{
    private readonly IAppDbContext _db;
    public MoneySafeMoveService(IAppDbContext db) { _db = db; }

    // Flat shape every source is projected into so they can be UNION ALL'ed in SQL.
    private class MoveRow
    {
        public string Kind { get; set; } = "";
        public long Id { get; set; }
        public DateTime Date { get; set; }
        public string Party { get; set; } = "";
        public string Notes { get; set; } = "";
        public decimal In { get; set; }
        public decimal Out { get; set; }
        public decimal InC { get; set; }
        public decimal OutC { get; set; }
    }

    private class Agg { public decimal In { get; set; } public decimal Out { get; set; } public decimal InC { get; set; } public decimal OutC { get; set; } }

    // ---------------------------------------------------------------- safes
    private IQueryable<Domain.Models.MoneySafe> AccessibleSafes(string userId, bool isAdmin)
    {
        var q = _db.MoneySafe.Where(m => !m.IsDeleted);
        if (!isAdmin)
            q = q.Where(m => m.Manager != null && !m.Manager.IsDeleted && m.Manager.UserId == userId);
        return q;
    }

    public async Task<ServiceResult<List<MoneySafeDto>>> GetAccessibleSafesAsync(string userId, bool isAdmin)
    {
        var safes = await AccessibleSafes(userId, isAdmin)
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new MoneySafeDto
            {
                Id = m.Id,
                Cod = m.Cod,
                Name = m.Name,
                Notes = m.Notes,
                Active = m.Active,
                IsMultiCurrency = m.IsMultiCurrency,
                OpeningBalance = m.OpeningBalance,
                OpeningBalanceCurrency = m.OpeningBalanceCurrency,
                ManagerId = m.ManagerId,
                ManagerName = m.Manager != null ? m.Manager.Name : "",
                CategoryId = m.CategoryId,
                CategoryName = m.Category != null ? m.Category.Name : "",
                TypeId = m.TypeId,
                TypeName = m.Type != null ? m.Type.Name : ""
            })
            .ToListAsync();

        var balances = await GetBalancesAsync(safes.Select(s => s.Id).ToList());
        foreach (var s in safes)
            if (balances.TryGetValue(s.Id, out var b)) { s.Balance = b.Balance; s.BalanceCurrency = b.BalanceCurrency; }

        return ServiceResult<List<MoneySafeDto>>.Ok(safes);
    }

    // ------------------------------------------------------------- balances
    public async Task<Dictionary<long, (decimal Balance, decimal BalanceCurrency)>> GetBalancesAsync(IReadOnlyCollection<long>? ids = null)
    {
        var idList = ids?.ToList();

        var safeQ = _db.MoneySafe.Where(m => !m.IsDeleted);
        if (idList != null) safeQ = safeQ.Where(m => idList.Contains(m.Id));
        var result = (await safeQ.AsNoTracking().Select(m => new { m.Id, m.OpeningBalance, m.OpeningBalanceCurrency }).ToListAsync())
            .ToDictionary(m => m.Id, m => (Balance: m.OpeningBalance, BalanceCurrency: m.OpeningBalanceCurrency));

        void Add(IEnumerable<(long Id, decimal Egp, decimal Cur)> rows)
        {
            foreach (var r in rows)
                if (result.TryGetValue(r.Id, out var cur))
                    result[r.Id] = (cur.Balance + r.Egp, cur.BalanceCurrency + r.Cur);
        }

        // customers pay us (in); refunds go back out
        var cp = _db.CustomerPayment.Where(p => !p.IsDeleted);
        if (idList != null) cp = cp.Where(p => idList.Contains(p.MoneySafeId));
        Add((await cp.GroupBy(p => p.MoneySafeId).Select(g => new
        {
            Id = g.Key,
            Egp = g.Sum(x => x.Type == PaymentType.Refund_Payment ? -x.Amount : x.Amount),
            Cur = g.Sum(x => x.Type == PaymentType.Refund_Payment ? -x.AmountCurrency : x.AmountCurrency)
        }).ToListAsync()).Select(x => (x.Id, x.Egp, x.Cur)));

        // we pay suppliers (out); a supplier refund comes back in
        var sp = _db.SupplierPayment.Where(p => !p.IsDeleted);
        if (idList != null) sp = sp.Where(p => idList.Contains(p.MoneySafeId));
        Add((await sp.GroupBy(p => p.MoneySafeId).Select(g => new
        {
            Id = g.Key,
            Egp = g.Sum(x => x.Type == PaymentType.Refund_Payment ? x.Amount : -x.Amount),
            Cur = g.Sum(x => x.Type == PaymentType.Refund_Payment ? x.AmountCurrency : -x.AmountCurrency)
        }).ToListAsync()).Select(x => (x.Id, x.Egp, x.Cur)));

        var ex = _db.Expense.Where(e => !e.IsDeleted);
        if (idList != null) ex = ex.Where(e => idList.Contains(e.MoneySafeId));
        Add((await ex.GroupBy(e => e.MoneySafeId).Select(g => new
        {
            Id = g.Key, Egp = -g.Sum(x => x.Amount), Cur = -g.Sum(x => x.AmountCurrency)
        }).ToListAsync()).Select(x => (x.Id, x.Egp, x.Cur)));

        // salaries paid out of a safe
        var pr = _db.EmployeePayroll.Where(p => !p.IsDeleted && p.WithdrawnAt != null && p.MoneySafeId != null);
        if (idList != null) pr = pr.Where(p => idList.Contains(p.MoneySafeId!.Value));
        Add((await pr.GroupBy(p => p.MoneySafeId!.Value).Select(g => new
        {
            Id = g.Key, Egp = -g.Sum(x => x.NetTotal)
        }).ToListAsync()).Select(x => (x.Id, x.Egp, 0m)));

        // loans paid out of a safe (installment loans never touch a safe)
        var ln = _db.EmployeeLoan.Where(l => !l.IsDeleted && l.MoneySafeId != null);
        if (idList != null) ln = ln.Where(l => idList.Contains(l.MoneySafeId!.Value));
        Add((await ln.GroupBy(l => l.MoneySafeId!.Value).Select(g => new
        {
            Id = g.Key, Egp = -g.Sum(x => x.Amount)
        }).ToListAsync()).Select(x => (x.Id, x.Egp, 0m)));

        // transfers: out of one safe, into another
        var tOut = _db.MoneySafeTransaction.Where(t => !t.IsDeleted);
        if (idList != null) tOut = tOut.Where(t => idList.Contains(t.OutMoneySafeId));
        Add((await tOut.GroupBy(t => t.OutMoneySafeId).Select(g => new
        {
            Id = g.Key, Egp = -g.Sum(x => x.TransactionAmount), Cur = -g.Sum(x => x.TransactionAmountCurrency)
        }).ToListAsync()).Select(x => (x.Id, x.Egp, x.Cur)));

        var tIn = _db.MoneySafeTransaction.Where(t => !t.IsDeleted);
        if (idList != null) tIn = tIn.Where(t => idList.Contains(t.InMoneySafeId));
        Add((await tIn.GroupBy(t => t.InMoneySafeId).Select(g => new
        {
            Id = g.Key, Egp = g.Sum(x => x.TransactionAmount), Cur = g.Sum(x => x.TransactionAmountCurrency)
        }).ToListAsync()).Select(x => (x.Id, x.Egp, x.Cur)));

        return result;
    }

    // ---------------------------------------------------------------- moves
    private IQueryable<MoveRow> MovesOf(long safeId)
    {
        var customer = _db.CustomerPayment.Where(p => !p.IsDeleted && p.MoneySafeId == safeId)
            .Select(p => new MoveRow
            {
                Kind = p.Type == PaymentType.Refund_Payment ? "customer_refund"
                     : p.Type == PaymentType.Advance_Payment ? "customer_advance" : "customer_payment",
                Id = p.Id,
                Date = p.Date,
                Party = p.Customer!.Name,
                Notes = p.Notes,
                In = p.Type == PaymentType.Refund_Payment ? 0m : p.Amount,
                InC = p.Type == PaymentType.Refund_Payment ? 0m : p.AmountCurrency,
                Out = p.Type == PaymentType.Refund_Payment ? p.Amount : 0m,
                OutC = p.Type == PaymentType.Refund_Payment ? p.AmountCurrency : 0m
            });

        var supplier = _db.SupplierPayment.Where(p => !p.IsDeleted && p.MoneySafeId == safeId)
            .Select(p => new MoveRow
            {
                Kind = p.Type == PaymentType.Refund_Payment ? "supplier_refund" : "supplier_payment",
                Id = p.Id,
                Date = p.Date,
                Party = p.Supplier!.Name,
                Notes = p.Notes,
                In = p.Type == PaymentType.Refund_Payment ? p.Amount : 0m,
                InC = p.Type == PaymentType.Refund_Payment ? p.AmountCurrency : 0m,
                Out = p.Type == PaymentType.Refund_Payment ? 0m : p.Amount,
                OutC = p.Type == PaymentType.Refund_Payment ? 0m : p.AmountCurrency
            });

        var expense = _db.Expense.Where(e => !e.IsDeleted && e.MoneySafeId == safeId)
            .Select(e => new MoveRow
            {
                Kind = "expense", Id = e.Id, Date = e.Date, Party = e.ExpenseType!.Name, Notes = e.Notes,
                In = 0m, InC = 0m, Out = e.Amount, OutC = e.AmountCurrency
            });

        var payroll = _db.EmployeePayroll.Where(p => !p.IsDeleted && p.WithdrawnAt != null && p.MoneySafeId == safeId)
            .Select(p => new MoveRow
            {
                Kind = "payroll", Id = p.Id, Date = p.WithdrawnAt!.Value, Party = p.Employee!.Name,
                Notes = "Payroll " + p.Month + "/" + p.Year,
                In = 0m, InC = 0m, Out = p.NetTotal, OutC = 0m
            });

        var loan = _db.EmployeeLoan.Where(l => !l.IsDeleted && l.MoneySafeId == safeId)
            .Select(l => new MoveRow
            {
                Kind = "loan", Id = l.Id, Date = l.CreatedAtUtc, Party = l.Employee!.Name, Notes = l.Notes,
                In = 0m, InC = 0m, Out = l.Amount, OutC = 0m
            });

        var tOut = _db.MoneySafeTransaction.Where(t => !t.IsDeleted && t.OutMoneySafeId == safeId)
            .Select(t => new MoveRow
            {
                Kind = "transfer_out", Id = t.Id, Date = t.TransactionDate, Party = t.InMoneySafe!.Name, Notes = t.TransactionNotes,
                In = 0m, InC = 0m, Out = t.TransactionAmount, OutC = t.TransactionAmountCurrency
            });

        var tIn = _db.MoneySafeTransaction.Where(t => !t.IsDeleted && t.InMoneySafeId == safeId)
            .Select(t => new MoveRow
            {
                Kind = "transfer_in", Id = t.Id, Date = t.TransactionDate, Party = t.OutMoneySafe!.Name, Notes = t.TransactionNotes,
                In = t.TransactionAmount, InC = t.TransactionAmountCurrency, Out = 0m, OutC = 0m
            });

        return customer.Concat(supplier).Concat(expense).Concat(payroll).Concat(loan).Concat(tOut).Concat(tIn);
    }

    private static async Task<Agg> SumAsync(IQueryable<MoveRow> q) =>
        await q.GroupBy(_ => 1).Select(g => new Agg
        {
            In = g.Sum(x => x.In), Out = g.Sum(x => x.Out), InC = g.Sum(x => x.InC), OutC = g.Sum(x => x.OutC)
        }).FirstOrDefaultAsync() ?? new Agg();

    public async Task<ServiceResult<PagedResult<MoneySafeMoveDto, MoneySafeMovesSummaryDto>>> GetMovesAsync(
        MoneySafeMoveFilterDto f, string userId, bool isAdmin)
    {
        // the safe must be one the caller is allowed to see; asking for any other looks like "not found"
        var accessible = AccessibleSafes(userId, isAdmin);
        var safe = await (f.MoneySafeId.HasValue
                ? accessible.Where(m => m.Id == f.MoneySafeId.Value)
                : accessible.OrderBy(m => m.Name))
            .AsNoTracking()
            .Select(m => new { m.Id, m.Name, m.IsMultiCurrency, m.OpeningBalance, m.OpeningBalanceCurrency })
            .FirstOrDefaultAsync();

        if (safe is null)
            return ServiceResult<PagedResult<MoneySafeMoveDto, MoneySafeMovesSummaryDto>>.NotFound("Money safe not found");

        if (f.DateFrom.HasValue && f.DateTo.HasValue && f.DateFrom.Value.Date > f.DateTo.Value.Date)
            return ServiceResult<PagedResult<MoneySafeMoveDto, MoneySafeMovesSummaryDto>>.Failed("DateFrom cannot be after DateTo");

        var all = MovesOf(safe.Id);
        var range = all;
        if (f.DateFrom.HasValue) { var from = f.DateFrom.Value.Date; range = range.Where(r => r.Date >= from); }
        if (f.DateTo.HasValue) { var to = f.DateTo.Value.Date.AddDays(1); range = range.Where(r => r.Date < to); }

        // totals: everything, everything before the range, and the range itself
        var allAgg = await SumAsync(all);
        var before = new Agg();
        if (f.DateFrom.HasValue) { var from = f.DateFrom.Value.Date; before = await SumAsync(all.Where(r => r.Date < from)); }
        var inRange = await SumAsync(range);

        var openEgp = safe.OpeningBalance + before.In - before.Out;
        var openCur = safe.OpeningBalanceCurrency + before.InC - before.OutC;
        var closeEgp = openEgp + inRange.In - inRange.Out;
        var closeCur = openCur + inRange.InC - inRange.OutC;

        var ordered = range.OrderByDescending(r => r.Date).ThenByDescending(r => r.Kind).ThenByDescending(r => r.Id);
        var page = await ordered.ToPagedAsync(f);

        // newest-first list: the first row's balance is the closing balance minus everything newer than this page
        decimal newerEgp = 0, newerCur = 0;
        var skip = (page.Page - 1) * page.PageSize;
        if (skip > 0)
        {
            newerEgp = await ordered.Take(skip).Select(r => r.In - r.Out).SumAsync();
            newerCur = await ordered.Take(skip).Select(r => r.InC - r.OutC).SumAsync();
        }

        var bal = closeEgp - newerEgp;
        var balC = closeCur - newerCur;
        var items = new List<MoneySafeMoveDto>(page.Items.Count);
        foreach (var r in page.Items)
        {
            items.Add(new MoneySafeMoveDto
            {
                Kind = r.Kind, Id = r.Id, Date = r.Date, Party = r.Party, Notes = r.Notes ?? "",
                In = r.In, Out = r.Out, InCurrency = r.InC, OutCurrency = r.OutC,
                Balance = bal, BalanceCurrency = balC
            });
            bal -= r.In - r.Out;
            balC -= r.InC - r.OutC;
        }

        return ServiceResult<PagedResult<MoneySafeMoveDto, MoneySafeMovesSummaryDto>>.Ok(
            new PagedResult<MoneySafeMoveDto, MoneySafeMovesSummaryDto>
            {
                Items = items,
                Total = page.Total,
                Page = page.Page,
                PageSize = page.PageSize,
                Summary = new MoneySafeMovesSummaryDto
                {
                    MoneySafeId = safe.Id,
                    MoneySafeName = safe.Name,
                    IsMultiCurrency = safe.IsMultiCurrency,
                    OpeningBalance = openEgp,
                    OpeningBalanceCurrency = openCur,
                    TotalIn = inRange.In, TotalOut = inRange.Out,
                    TotalInCurrency = inRange.InC, TotalOutCurrency = inRange.OutC,
                    ClosingBalance = closeEgp,
                    ClosingBalanceCurrency = closeCur,
                    CurrentBalance = safe.OpeningBalance + allAgg.In - allAgg.Out,
                    CurrentBalanceCurrency = safe.OpeningBalanceCurrency + allAgg.InC - allAgg.OutC
                }
            });
    }


    // ---------------------------------------------------------------- export
    // MoneySafeMoveService: add usings ClosedXML.Excel and CMS.Application.Common
    private const int MaxExportRows = 50_000;

    private static string KindAr(string k) => k switch
    {
        "customer_payment" => "دفعة عميل",
        "customer_advance" => "دفعة مقدمة من عميل",
        "customer_refund" => "مرتجع نقدي لعميل",
        "supplier_payment" => "دفعة مورد",
        "supplier_refund" => "استرداد من مورد",
        "expense" => "مصروف",
        "payroll" => "صرف مرتب",
        "loan" => "سلفة",
        "transfer_in" => "تحويل وارد",
        "transfer_out" => "تحويل صادر",
        _ => k
    };

    public async Task<ExportResult> ExportAsync(MoneySafeMoveFilterDto f, string userId, bool isAdmin)
    {
        // same access rule as the page: a safe the caller can't see looks like "not found"
        var accessible = AccessibleSafes(userId, isAdmin);
        var safe = await (f.MoneySafeId.HasValue
                ? accessible.Where(m => m.Id == f.MoneySafeId.Value)
                : accessible.OrderBy(m => m.Name))
            .AsNoTracking()
            .Select(m => new { m.Id, m.Name, m.IsMultiCurrency, m.OpeningBalance, m.OpeningBalanceCurrency })
            .FirstOrDefaultAsync();
        if (safe is null) return new ExportResult(null, "Money safe not found", "");

        if (f.DateFrom.HasValue && f.DateTo.HasValue && f.DateFrom.Value.Date > f.DateTo.Value.Date)
            return new ExportResult(null, "DateFrom cannot be after DateTo", "");

        var all = MovesOf(safe.Id);
        var range = all;
        if (f.DateFrom.HasValue) { var from = f.DateFrom.Value.Date; range = range.Where(r => r.Date >= from); }
        if (f.DateTo.HasValue) { var to = f.DateTo.Value.Date.AddDays(1); range = range.Where(r => r.Date < to); }

        var allAgg = await SumAsync(all);
        var before = new Agg();
        if (f.DateFrom.HasValue) { var from = f.DateFrom.Value.Date; before = await SumAsync(all.Where(r => r.Date < from)); }
        var inRange = await SumAsync(range);

        var openEgp = safe.OpeningBalance + before.In - before.Out;
        var openCur = safe.OpeningBalanceCurrency + before.InC - before.OutC;
        var closeEgp = openEgp + inRange.In - inRange.Out;
        var closeCur = openCur + inRange.InC - inRange.OutC;
        var currentEgp = safe.OpeningBalance + allAgg.In - allAgg.Out;
        var currentCur = safe.OpeningBalanceCurrency + allAgg.InC - allAgg.OutC;

        var rows = await range.OrderByDescending(r => r.Date).ThenByDescending(r => r.Kind).ThenByDescending(r => r.Id)
            .Take(MaxExportRows + 1).ToListAsync();
        if (rows.Count > MaxExportRows)
            return new ExportResult(null, $"أكثر من {MaxExportRows} حركة، قلل الفترة ثم حاول مرة اخرى", "");

        // newest first: the first row's balance is the closing balance, then count down
        var bal = closeEgp; var balC = closeCur;
        var lines = new List<(MoveRow R, decimal Bal, decimal BalC)>(rows.Count);
        foreach (var r in rows)
        {
            lines.Add((r, bal, balC));
            bal -= r.In - r.Out;
            balC -= r.InC - r.OutC;
        }

        var multi = safe.IsMultiCurrency;
        var period = f.DateFrom == null && f.DateTo == null ? "كل الفترات"
                   : $"{f.DateFrom:yyyy-MM-dd} → {f.DateTo:yyyy-MM-dd}";
        var title = $"حركة الخزنة {safe.Name}";
        var sub = $"الفترة: {period}   |   التاريخ: {DateTime.Now:yyyy-MM-dd HH:mm}";

        using var wb = new XLWorkbook();

        // ---- summary ----
        var sumCols = new List<XCol> { new("البند", 30), new("جنيه", 18, TableExcel.Money, true) };
        if (multi) sumCols.Add(new("عملة", 18, TableExcel.Money, true));
        object?[] Line(string label, decimal egp, decimal cur) =>
            multi ? new object?[] { label, egp, cur } : new object?[] { label, egp };

        TableExcel.AddSheet(wb, "الملخص", title, sub, sumCols,
            new List<object?[]>
            {
            Line("الرصيد الحالي", currentEgp, currentCur),
            Line("رصيد اول المدة", openEgp, openCur),
            Line("اجمالي الوارد", inRange.In, inRange.InC),
            Line("اجمالي الصادر", inRange.Out, inRange.OutC),
            },
            Line("", closeEgp, closeCur).Select((v, i) => i == 0 ? null : v).ToArray(), "رصيد نهاية المدة");

        // ---- moves ----
        var cols = new List<XCol>
    {
        new("التاريخ", 14, TableExcel.Day, true), new("العملية", 20), new("الطرف", 26), new("ملاحظات", 36),
        new("وارد", 14, TableExcel.Money), new("صادر", 14, TableExcel.Money),
    };
        if (multi) { cols.Add(new("وارد (عملة)", 14, TableExcel.Money)); cols.Add(new("صادر (عملة)", 14, TableExcel.Money)); }
        cols.Add(new("الرصيد", 16, TableExcel.Money));
        if (multi) cols.Add(new("الرصيد (عملة)", 16, TableExcel.Money));

        var data = lines.Select(x =>
        {
            var row = new List<object?> { x.R.Date, KindAr(x.R.Kind), x.R.Party, x.R.Notes, x.R.In, x.R.Out };
            if (multi) { row.Add(x.R.InC); row.Add(x.R.OutC); }
            row.Add(x.Bal);
            if (multi) row.Add(x.BalC);
            return row.ToArray();
        });

        var totals = new List<object?> { null, null, null, null, inRange.In, inRange.Out };
        if (multi) { totals.Add(inRange.InC); totals.Add(inRange.OutC); }
        totals.Add(closeEgp);
        if (multi) totals.Add(closeCur);

        TableExcel.AddSheet(wb, "الحركات", title, sub, cols, data, totals.ToArray(), $"الاجمالي ({lines.Count} حركة)");

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return new ExportResult(ms.ToArray(), null, $"money-safe-{safe.Id}-{DateTime.Now:yyyyMMdd}.xlsx");
    }
}
