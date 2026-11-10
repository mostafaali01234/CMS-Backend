using ClosedXML.Excel;
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;
using CMS.Application.Interfaces;
using CMS.Application.Interfaces.Configuration;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Services;

public class EmployeeBalanceService : IEmployeeBalanceService
{
    private readonly IAppDbContext _db;
    public EmployeeBalanceService(IAppDbContext db) { _db = db; }

    private record Mv(DateTime Date, string Kind, string Description, decimal Amount, long Ref);
    private static decimal R(decimal v) => Math.Round(v, 2);
    private static int Rank(string kind) => kind switch { "salary" => 1, "installments" => 2, "withdraw" => 3, _ => 4 };

    private async Task<(EmployeeBalanceDto? Dto, string? Error)> BuildAsync(long employeeId, DateTime? from, DateTime? to)
    {
        if (employeeId <= 0) return (null, "EmployeeId is required");
        var f = from?.Date;
        var t = to?.Date;
        if (f.HasValue && t.HasValue && t < f) return (null, "The end date is before the start date");

        var emp = await _db.Employee.AsNoTracking().Where(e => e.Id == employeeId && !e.IsDeleted)
            .Select(e => new { e.Name, e.OpeningBalance }).FirstOrDefaultAsync();
        if (emp is null) return (null, $"Employee with Id {employeeId} was not found");

        var payrolls = await _db.EmployeePayroll.AsNoTracking()
            .Where(p => !p.IsDeleted && p.EmployeeId == employeeId)
            .Select(p => new
            {
                p.Id,
                p.Month,
                p.Year,
                p.NetTotal,
                p.LoansTotal,
                p.RevisedAt,
                p.CreatedAtUtc,
                p.WithdrawnAt,
                Safe = p.MoneySafe != null ? p.MoneySafe.Name : ""
            }).ToListAsync();

        // EmployeeLoan has no date of its own: the shift date, or the day it was created
        var loans = await _db.EmployeeLoan.AsNoTracking()
            .Where(l => !l.IsDeleted && l.EmployeeId == employeeId)
            .Select(l => new
            {
                l.Id,
                l.Amount,
                l.Notes,
                l.InstallmentCount,
                Date = l.Shift != null ? l.Shift.Date : l.CreatedAtUtc
            }).ToListAsync();

        // every movement of this employee, signed: + owed to him, - paid to him or lent to him
        var all = new List<Mv>();
        foreach (var p in payrolls)
        {
            var reviewed = (p.RevisedAt ?? p.CreatedAtUtc).Date;
            all.Add(new(reviewed, "salary", $"اعتماد مرتب {p.Month}/{p.Year}", p.NetTotal, p.Id));
            if (p.LoansTotal > 0)
                all.Add(new(reviewed, "installments", $"اقساط سلف مخصومة من مرتب {p.Month}/{p.Year}", p.LoansTotal, p.Id));
            if (p.WithdrawnAt.HasValue)
                all.Add(new(p.WithdrawnAt.Value.Date, "withdraw",
                    $"صرف مرتب {p.Month}/{p.Year}" + (p.Safe == "" ? "" : $" - {p.Safe}"), -p.NetTotal, p.Id));
        }
        foreach (var l in loans)
            all.Add(new(l.Date.Date, "loan",
                "سلفة" + (l.InstallmentCount > 0 ? $" ({l.InstallmentCount} قسط)" : "")
                       + (string.IsNullOrWhiteSpace(l.Notes) ? "" : $" - {l.Notes}"), -l.Amount, l.Id));

        var opening = emp.OpeningBalance + (f.HasValue ? all.Where(m => m.Date < f.Value).Sum(m => m.Amount) : 0);
        var inRange = all
            .Where(m => (!f.HasValue || m.Date >= f.Value) && (!t.HasValue || m.Date < t.Value.AddDays(1)))
            .OrderBy(m => m.Date).ThenBy(m => Rank(m.Kind)).ThenBy(m => m.Ref).ToList();

        var run = opening;
        var rows = inRange.Select(m =>
        {
            run += m.Amount;
            return new BalanceMovementDto { Date = m.Date, Kind = m.Kind, Description = m.Description, Amount = R(m.Amount), Balance = R(run) };
        }).ToList();

        decimal Sum(string k) => R(inRange.Where(m => m.Kind == k).Sum(m => m.Amount));

        return (new EmployeeBalanceDto
        {
            EmployeeId = employeeId,
            EmployeeName = emp.Name,
            DateFrom = f,
            DateTo = t,
            OpeningBalance = R(opening),
            SalariesTotal = Sum("salary"),
            InstallmentsTotal = Sum("installments"),
            WithdrawnTotal = -Sum("withdraw"),
            LoansTotal = -Sum("loan"),
            ClosingBalance = R(opening + inRange.Sum(m => m.Amount)),
            Movements = rows
        }, null);
    }

    public async Task<ServiceResult<EmployeeBalanceDto>> GetAsync(long employeeId, DateTime? from, DateTime? to)
    {
        var (dto, error) = await BuildAsync(employeeId, from, to);
        return error is not null ? ServiceResult<EmployeeBalanceDto>.Failed(error) : ServiceResult<EmployeeBalanceDto>.Ok(dto!);
    }

    private static string KindAr(string k) => k switch
    {
        "salary" => "اعتماد مرتب",
        "installments" => "اقساط سلف",
        "withdraw" => "صرف مرتب",
        _ => "سلفة"
    };

    public async Task<ExportResult> ExportAsync(long employeeId, DateTime? from, DateTime? to)
    {
        var (d, error) = await BuildAsync(employeeId, from, to);
        if (error is not null) return new ExportResult(null, error, "");

        var period = d!.DateFrom == null && d.DateTo == null ? "كل الفترات"
                   : $"{d.DateFrom:yyyy-MM-dd} → {d.DateTo:yyyy-MM-dd}";
        var title = $"رصيد الموظف {d.EmployeeName}";
        var sub = $"الفترة: {period}   |   التاريخ: {DateTime.Now:yyyy-MM-dd HH:mm}";
        const string signed = "#,##0.00;[Red]-#,##0.00";

        using var wb = new XLWorkbook();

        TableExcel.AddSheet(wb, "الملخص", title, sub,
            new List<XCol> { new("البند", 40), new("القيمة", 18, signed, true) },
            new List<object?[]>
            {
                new object?[] { "رصيد اول المدة", d.OpeningBalance },
                new object?[] { "مرتبات تم اعتمادها", d.SalariesTotal },
                new object?[] { "اقساط السلف المخصومة من المرتبات", d.InstallmentsTotal },
                new object?[] { "مرتبات تم صرفها", -d.WithdrawnTotal },
                new object?[] { "السلف", -d.LoansTotal },
            },
            new object?[] { null, d.ClosingBalance }, "الاجمالي (الرصيد)");

        TableExcel.AddSheet(wb, "الحركات", title, sub,
            new List<XCol>
            {
                new("التاريخ", 14, TableExcel.Day, true), new("النوع", 16), new("البيان", 52),
                new("المبلغ", 16, signed), new("الرصيد", 16, signed),
            },
            d.Movements.Select(m => new object?[] { m.Date, KindAr(m.Kind), m.Description, m.Amount, m.Balance }),
            new object?[] { null, null, null, d.Movements.Sum(m => m.Amount), d.ClosingBalance },
            $"الاجمالي ({d.Movements.Count} حركة)");

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return new ExportResult(ms.ToArray(), null, $"employee-balance-{employeeId}-{DateTime.Now:yyyyMMdd}.xlsx");
    }
}