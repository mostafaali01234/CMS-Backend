using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;
using CMS.Application.Interfaces;
using CMS.Application.Interfaces.Configuration;
using CMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Services;

public class ProjectReviewService : IProjectReviewService
{
    private readonly IAppDbContext _db;
    public ProjectReviewService(IAppDbContext db) { _db = db; }

    private static string Stamp => DateTime.Now.ToString("yyyyMMdd-HHmm");

    // inclusive day range -> [from, toExclusive)
    private static (DateTime? From, DateTime? ToExcl) Range(ProjectReviewFilterDto f) =>
        (f.DateFrom?.Date, f.DateTo?.Date.AddDays(1));

    // ------------------------------------------------------------ summaries
    public async Task<ServiceResult<List<ProjectReviewSummaryDto>>> GetSummariesAsync(ProjectReviewFilterDto f)
    {
        var projects = await _db.Project.Where(p => !p.IsDeleted).AsNoTracking()
            .OrderBy(p => p.Name).Select(p => new { p.Id, p.Name }).ToListAsync();

        var rows = await SummarizeAsync(projects.Select(p => p.Id).ToList(), f);
        foreach (var p in projects) rows[p.Id].ProjectName = p.Name;

        return ServiceResult<List<ProjectReviewSummaryDto>>.Ok(projects.Select(p => rows[p.Id]).ToList());
    }

    private async Task<Dictionary<long, ProjectReviewSummaryDto>> SummarizeAsync(List<long> ids, ProjectReviewFilterDto f)
    {
        var (from, toExcl) = Range(f);
        var res = ids.ToDictionary(i => i, i => new ProjectReviewSummaryDto { ProjectId = i });

        var sale = _db.SaleInvoiceItem.Where(i => !i.IsDeleted && !i.Invoice!.IsDeleted && i.ProjectId != null && ids.Contains(i.ProjectId.Value));
        if (from.HasValue) sale = sale.Where(i => i.Invoice!.Date >= from);
        if (toExcl.HasValue) sale = sale.Where(i => i.Invoice!.Date < toExcl);
        foreach (var g in await sale.GroupBy(i => new { P = i.ProjectId!.Value, i.Invoice!.Type })
                     .Select(g => new { g.Key.P, g.Key.Type, Sum = g.Sum(x => x.ProductNetTotal) }).ToListAsync())
        {
            if (g.Type == InvoiceType.Sale) res[g.P].SalesTotal += g.Sum;
            else if (g.Type == InvoiceType.Sale_Return) res[g.P].SalesReturnsTotal += g.Sum;
        }

        var buy = _db.BuyInvoiceItem.Where(i => !i.IsDeleted && !i.Invoice!.IsDeleted && i.ProjectId != null && ids.Contains(i.ProjectId.Value));
        if (from.HasValue) buy = buy.Where(i => i.Invoice!.Date >= from);
        if (toExcl.HasValue) buy = buy.Where(i => i.Invoice!.Date < toExcl);
        foreach (var g in await buy.GroupBy(i => new { P = i.ProjectId!.Value, i.Invoice!.Type })
                     .Select(g => new { g.Key.P, g.Key.Type, Sum = g.Sum(x => x.ProductNetTotal) }).ToListAsync())
        {
            if (g.Type == InvoiceType.Buy) res[g.P].PurchasesTotal += g.Sum;
            else if (g.Type == InvoiceType.Buy_Return) res[g.P].PurchasesReturnsTotal += g.Sum;
        }

        var exp = _db.Expense.Where(e => !e.IsDeleted && e.ProjectId != null && ids.Contains(e.ProjectId.Value));
        if (from.HasValue) exp = exp.Where(e => e.Date >= from);
        if (toExcl.HasValue) exp = exp.Where(e => e.Date < toExcl);
        foreach (var g in await exp.GroupBy(e => e.ProjectId!.Value)
                     .Select(g => new { P = g.Key, Egp = g.Sum(x => x.Amount), Cur = g.Sum(x => x.AmountCurrency) }).ToListAsync())
        { res[g.P].ExpensesTotal = g.Egp; res[g.P].ExpensesTotalCurrency = g.Cur; }

        var loan = _db.EmployeeLoan.Where(l => !l.IsDeleted && l.ProjectId != null && ids.Contains(l.ProjectId.Value));
        if (from.HasValue) loan = loan.Where(l => l.CreatedAtUtc >= from);
        if (toExcl.HasValue) loan = loan.Where(l => l.CreatedAtUtc < toExcl);
        foreach (var g in await loan.GroupBy(l => l.ProjectId!.Value)
                     .Select(g => new { P = g.Key, Sum = g.Sum(x => x.Amount) }).ToListAsync())
            res[g.P].LoansTotal = g.Sum;

        var tx = _db.MoneySafeTransaction.Where(t => !t.IsDeleted && t.ProjectId != null && ids.Contains(t.ProjectId.Value));
        if (from.HasValue) tx = tx.Where(t => t.TransactionDate >= from);
        if (toExcl.HasValue) tx = tx.Where(t => t.TransactionDate < toExcl);
        foreach (var g in await tx.GroupBy(t => t.ProjectId!.Value)
                     .Select(g => new { P = g.Key, Sum = g.Sum(x => x.TransactionAmount) }).ToListAsync())
            res[g.P].TransfersTotal = g.Sum;

        var sh = _db.Shift.Where(s => !s.IsDeleted && s.ProjectId != null && ids.Contains(s.ProjectId.Value));
        if (from.HasValue) sh = sh.Where(s => s.Date >= from);
        if (toExcl.HasValue) sh = sh.Where(s => s.Date < toExcl);
        foreach (var g in await sh.GroupBy(s => s.ProjectId!.Value)
                     .Select(g => new { P = g.Key, Count = g.Count(), Km = g.Sum(x => x.KmEnd - x.KmStart) }).ToListAsync())
        { res[g.P].ShiftsCount = g.Count; res[g.P].KmTotal = g.Km; }

        foreach (var s in res.Values)
        {
            s.NetSales = s.SalesTotal - s.SalesReturnsTotal;
            s.NetPurchases = s.PurchasesTotal - s.PurchasesReturnsTotal;
            s.TotalCost = s.NetPurchases + s.ExpensesTotal;
            s.Profit = s.NetSales - s.TotalCost;
            s.MarginPercent = s.NetSales == 0 ? 0 : Math.Round(s.Profit / s.NetSales * 100, 2);
        }
        return res;
    }

    // --------------------------------------------------------------- review
    public async Task<ServiceResult<ProjectReviewDto>> GetReviewAsync(long projectId, ProjectReviewFilterDto f)
    {
        if (f.DateFrom.HasValue && f.DateTo.HasValue && f.DateFrom.Value.Date > f.DateTo.Value.Date)
            return ServiceResult<ProjectReviewDto>.Failed("DateFrom cannot be after DateTo");

        var project = await _db.Project.AsNoTracking().FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);
        if (project is null) return ServiceResult<ProjectReviewDto>.NotFound("Project not found");

        var (from, toExcl) = Range(f);

        var summary = (await SummarizeAsync(new List<long> { projectId }, f))[projectId];
        summary.ProjectName = project.Name;

        // ---- sales (returns negative so a column sum is the net)
        var sq = _db.SaleInvoiceItem.Where(i => !i.IsDeleted && !i.Invoice!.IsDeleted && i.ProjectId == projectId);
        if (from.HasValue) sq = sq.Where(i => i.Invoice!.Date >= from);
        if (toExcl.HasValue) sq = sq.Where(i => i.Invoice!.Date < toExcl);
        var sales = await sq.AsNoTracking().OrderBy(i => i.Invoice!.Date).ThenBy(i => i.InvoiceId).ThenBy(i => i.Id)
            .Select(i => new ProjectInvoiceLineDto
            {
                InvoiceId = i.InvoiceId, InvoiceNumber = i.Invoice!.Number, Date = i.Invoice.Date, Type = i.Invoice.Type,
                Party = i.Invoice.Customer != null ? i.Invoice.Customer.Name : "",
                Product = i.Product != null ? i.Product.Name : "", Notes = i.ProductNotes,
                Quantity = i.Invoice.Type == InvoiceType.Sale_Return ? -i.ProductQuantity : i.ProductQuantity,
                Price = i.ProductPrice, Discount = i.ProductDiscount,
                NetTotal = i.Invoice.Type == InvoiceType.Sale_Return ? -i.ProductNetTotal : i.ProductNetTotal
            }).ToListAsync();

        // ---- purchases
        var bq = _db.BuyInvoiceItem.Where(i => !i.IsDeleted && !i.Invoice!.IsDeleted && i.ProjectId == projectId);
        if (from.HasValue) bq = bq.Where(i => i.Invoice!.Date >= from);
        if (toExcl.HasValue) bq = bq.Where(i => i.Invoice!.Date < toExcl);
        var purchases = await bq.AsNoTracking().OrderBy(i => i.Invoice!.Date).ThenBy(i => i.InvoiceId).ThenBy(i => i.Id)
            .Select(i => new ProjectInvoiceLineDto
            {
                InvoiceId = i.InvoiceId, InvoiceNumber = i.Invoice!.Number, Date = i.Invoice.Date, Type = i.Invoice.Type,
                Party = i.Invoice.Supplier != null ? i.Invoice.Supplier.Name : "",
                Product = i.Product != null ? i.Product.Name : "", Notes = i.ProductNotes,
                Quantity = i.Invoice.Type == InvoiceType.Buy_Return ? -i.ProductQuantity : i.ProductQuantity,
                Price = i.ProductPrice, Discount = i.ProductDiscount,
                NetTotal = i.Invoice.Type == InvoiceType.Buy_Return ? -i.ProductNetTotal : i.ProductNetTotal
            }).ToListAsync();

        // ---- expenses
        var eq = _db.Expense.Where(e => !e.IsDeleted && e.ProjectId == projectId);
        if (from.HasValue) eq = eq.Where(e => e.Date >= from);
        if (toExcl.HasValue) eq = eq.Where(e => e.Date < toExcl);
        var expenses = await eq.AsNoTracking().OrderBy(e => e.Date).ThenBy(e => e.Id)
            .Select(e => new ProjectExpenseLineDto
            {
                Id = e.Id, Date = e.Date, ExpenseType = e.ExpenseType != null ? e.ExpenseType.Name : "", Notes = e.Notes,
                MoneySafe = e.MoneySafe != null ? e.MoneySafe.Name : "", Amount = e.Amount, AmountCurrency = e.AmountCurrency
            }).ToListAsync();

        // ---- loans
        var lq = _db.EmployeeLoan.Where(l => !l.IsDeleted && l.ProjectId == projectId);
        if (from.HasValue) lq = lq.Where(l => l.CreatedAtUtc >= from);
        if (toExcl.HasValue) lq = lq.Where(l => l.CreatedAtUtc < toExcl);
        var loans = await lq.AsNoTracking().OrderBy(l => l.CreatedAtUtc).ThenBy(l => l.Id)
            .Select(l => new ProjectLoanLineDto
            {
                Id = l.Id, Date = l.CreatedAtUtc, Employee = l.Employee != null ? l.Employee.Name : "",
                MoneySafe = l.MoneySafe != null ? l.MoneySafe.Name : "", InstallmentCount = l.InstallmentCount,
                Notes = l.Notes, Amount = l.Amount
            }).ToListAsync();

        // ---- safe transfers
        var tq = _db.MoneySafeTransaction.Where(t => !t.IsDeleted && t.ProjectId == projectId);
        if (from.HasValue) tq = tq.Where(t => t.TransactionDate >= from);
        if (toExcl.HasValue) tq = tq.Where(t => t.TransactionDate < toExcl);
        var transactions = await tq.AsNoTracking().OrderBy(t => t.TransactionDate).ThenBy(t => t.Id)
            .Select(t => new ProjectTransactionLineDto
            {
                Id = t.Id, Date = t.TransactionDate, TransactionType = t.TransactionType,
                OutMoneySafe = t.OutMoneySafe != null ? t.OutMoneySafe.Name : "",
                InMoneySafe = t.InMoneySafe != null ? t.InMoneySafe.Name : "",
                Notes = t.TransactionNotes, Amount = t.TransactionAmount, AmountCurrency = t.TransactionAmountCurrency
            }).ToListAsync();

        // ---- shifts
        var shq = _db.Shift.Where(s => !s.IsDeleted && s.ProjectId == projectId);
        if (from.HasValue) shq = shq.Where(s => s.Date >= from);
        if (toExcl.HasValue) shq = shq.Where(s => s.Date < toExcl);
        var shifts = await shq.AsNoTracking().OrderBy(s => s.Date).ThenBy(s => s.Id)
            .Select(s => new ProjectShiftLineDto
            {
                Id = s.Id, Date = s.Date, State = s.CountryState != null ? s.CountryState.Name : "",
                Car = s.Car != null ? s.Car.Name : "", Store = s.Store != null ? s.Store.Name : "",
                KmStart = s.KmStart, KmEnd = s.KmEnd, Km = s.KmEnd - s.KmStart, Notes = s.Notes
            }).ToListAsync();

        var byType = expenses.GroupBy(e => e.ExpenseType)
            .Select(g => new ProjectExpenseTypeTotalDto
            {
                ExpenseType = g.Key, Count = g.Count(), Amount = g.Sum(x => x.Amount), AmountCurrency = g.Sum(x => x.AmountCurrency)
            })
            .OrderByDescending(x => x.Amount).ToList();

        return ServiceResult<ProjectReviewDto>.Ok(new ProjectReviewDto
        {
            ProjectId = project.Id, ProjectName = project.Name, ProjectNotes = project.Notes,
            DateFrom = f.DateFrom?.Date, DateTo = f.DateTo?.Date,
            Summary = summary, ExpensesByType = byType,
            Sales = sales, Purchases = purchases, Expenses = expenses, Loans = loans, Transactions = transactions, Shifts = shifts
        });
    }

    // ---------------------------------------------------------------- excel
    public async Task<ExportResult> ExportAsync(long projectId, ProjectReviewFilterDto f)
    {
        var r = await GetReviewAsync(projectId, f);
        if (!r.Succeeded) return new ExportResult(null, string.Join("; ", r.Errors), "");
        var d = r.Data!;
        var s = d.Summary;

        var period = d.DateFrom.HasValue || d.DateTo.HasValue
            ? $"الفترة: {d.DateFrom?.ToString("yyyy-MM-dd") ?? "..."} الى {d.DateTo?.ToString("yyyy-MM-dd") ?? "..."}"
            : "كل الفترات";
        var sub = $"{period} — {DateTime.Now:yyyy-MM-dd HH:mm}";

        var sheets = new List<TableExcel.XSheet>();

        // summary: label / value
        var sumCols = new List<XCol> { new("البند", 34), new("القيمة", 20, TableExcel.Money) };
        var sumRows = new List<object?[]>
        {
            new object?[] { "المبيعات", s.SalesTotal },
            new object?[] { "مرتجع المبيعات", -s.SalesReturnsTotal },
            new object?[] { "صافي المبيعات", s.NetSales },
            new object?[] { "المشتريات", s.PurchasesTotal },
            new object?[] { "مرتجع المشتريات", -s.PurchasesReturnsTotal },
            new object?[] { "صافي المشتريات", s.NetPurchases },
            new object?[] { "المصروفات", s.ExpensesTotal },
            new object?[] { "المصروفات (عملة)", s.ExpensesTotalCurrency },
            new object?[] { "اجمالي التكلفة", s.TotalCost },
            new object?[] { "الربح", s.Profit },
            new object?[] { "هامش الربح %", s.MarginPercent },
            new object?[] { "السلف (خارج الربح)", s.LoansTotal },
            new object?[] { "التحويلات (خارج الربح)", s.TransfersTotal },
            new object?[] { "عدد الورديات", (decimal)s.ShiftsCount },
            new object?[] { "اجمالي الكيلومترات", s.KmTotal },
        };
        foreach (var t in d.ExpensesByType)
            sumRows.Add(new object?[] { $"مصروفات: {t.ExpenseType} ({t.Count})", t.Amount });
        sheets.Add(new("ملخص", $"مراجعة المشروع: {d.ProjectName}", sub, sumCols, sumRows));

        var invCols = new List<XCol>
        {
            new("التاريخ", 13, TableExcel.Day, true), new("رقم الفاتورة", 12, null, true), new("النوع", 14, null, true),
            new("الطرف", 24), new("المنتج", 32), new("ملاحظات", 26),
            new("الكمية", 12, TableExcel.Qty, true), new("السعر", 12, TableExcel.Money), new("الخصم", 12, TableExcel.Money),
            new("الصافي", 14, TableExcel.Money),
        };
        object?[] InvRow(ProjectInvoiceLineDto i) => new object?[]
            { i.Date, i.InvoiceNumber, TypeName(i.Type), i.Party, i.Product, i.Notes, i.Quantity, i.Price, i.Discount, i.NetTotal };
        object?[] InvTotal(List<ProjectInvoiceLineDto> l) => new object?[]
            { null, null, null, null, null, null, l.Sum(x => x.Quantity), null, l.Sum(x => x.Discount), l.Sum(x => x.NetTotal) };
        sheets.Add(new("مبيعات", $"مبيعات: {d.ProjectName}", sub, invCols, d.Sales.Select(InvRow), InvTotal(d.Sales)));
        sheets.Add(new("مشتريات", $"مشتريات: {d.ProjectName}", sub, invCols, d.Purchases.Select(InvRow), InvTotal(d.Purchases)));

        sheets.Add(new("مصروفات", $"مصروفات: {d.ProjectName}", sub,
            new List<XCol>
            {
                new("التاريخ", 13, TableExcel.Day, true), new("النوع", 24), new("الخزنة", 22), new("ملاحظات", 34),
                new("المبلغ", 14, TableExcel.Money), new("المبلغ (عملة)", 14, TableExcel.Money)
            },
            d.Expenses.Select(e => new object?[] { e.Date, e.ExpenseType, e.MoneySafe, e.Notes, e.Amount, e.AmountCurrency }),
            new object?[] { null, null, null, null, d.Expenses.Sum(x => x.Amount), d.Expenses.Sum(x => x.AmountCurrency) }));

        sheets.Add(new("سلف", $"سلف: {d.ProjectName}", sub,
            new List<XCol>
            {
                new("التاريخ", 13, TableExcel.Day, true), new("الموظف", 24), new("الخزنة", 22),
                new("الاقساط", 10, null, true), new("ملاحظات", 34), new("المبلغ", 14, TableExcel.Money)
            },
            d.Loans.Select(l => new object?[] { l.Date, l.Employee, l.MoneySafe, l.InstallmentCount, l.Notes, l.Amount }),
            new object?[] { null, null, null, null, null, d.Loans.Sum(x => x.Amount) }));

        sheets.Add(new("تحويلات", $"تحويلات الخزن: {d.ProjectName}", sub,
            new List<XCol>
            {
                new("التاريخ", 13, TableExcel.Day, true), new("النوع", 12, null, true), new("من خزنة", 22), new("الى خزنة", 22),
                new("ملاحظات", 30), new("المبلغ", 14, TableExcel.Money), new("المبلغ (عملة)", 14, TableExcel.Money)
            },
            d.Transactions.Select(t => new object?[] { t.Date, t.TransactionType.ToString(), t.OutMoneySafe, t.InMoneySafe, t.Notes, t.Amount, t.AmountCurrency }),
            new object?[] { null, null, null, null, null, d.Transactions.Sum(x => x.Amount), d.Transactions.Sum(x => x.AmountCurrency) }));

        sheets.Add(new("ورديات", $"ورديات: {d.ProjectName}", sub,
            new List<XCol>
            {
                new("التاريخ", 13, TableExcel.Day, true), new("المحافظة", 18), new("السيارة", 18), new("المخزن", 18),
                new("من كم", 12, TableExcel.Qty, true), new("الى كم", 12, TableExcel.Qty, true),
                new("المسافة", 12, TableExcel.Qty, true), new("ملاحظات", 30)
            },
            d.Shifts.Select(x => new object?[] { x.Date, x.State, x.Car, x.Store, x.KmStart, x.KmEnd, x.Km, x.Notes }),
            new object?[] { null, null, null, null, null, null, d.Shifts.Sum(x => x.Km), null }));

        var bytes = TableExcel.BuildBook(sheets);
        var safeName = string.Concat(d.ProjectName.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
        return new ExportResult(bytes, null, $"project-{(string.IsNullOrWhiteSpace(safeName) ? d.ProjectId.ToString() : safeName)}-{Stamp}.xlsx");
    }

    private static string TypeName(InvoiceType t) => t switch
    {
        InvoiceType.Sale => "بيع",
        InvoiceType.Sale_Return => "مرتجع بيع",
        InvoiceType.Buy => "شراء",
        InvoiceType.Buy_Return => "مرتجع شراء",
        _ => t.ToString()
    };
}
