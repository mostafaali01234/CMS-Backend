using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;
using CMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Services;

// 11: maintenance orders created within 30 days of their original order, per tech
public partial class ReportService
{
    private const int MaintenanceWindowDays = 30;

    // The date range is the creation date of the ORIGINAL order. For every tech (the original order's current tech):
    //   done      = his done installation orders (تام, not maintenance, category تركيب, type تركيب) in the range
    //   original  = those of them that came back for maintenance
    //   maint     = maintenance orders (not cancelled) created less than 30 days after their original order
    //   percent   = maintenance count / done count * 100
    // Only installation (تركيب) orders can get maintenance, so wholesale / shipping orders are not in "done".
    public async Task<ServiceResult<MaintenanceReportDto>> GetMaintenanceAsync(ReportDateFilterDto f)
    {
        var rangeError = CheckRange(f);
        if (rangeError is not null) return ServiceResult<MaintenanceReportDto>.Failed(rangeError);
        var (from, toExcl) = OrderRange(f);

        var cohort = _db.Order.AsNoTracking().Where(o => !o.IsDeleted && o.OldOrderId == null
            && o.Status == OrderStatus.تام && o.Category == OrderCategory.تركيب && o.Type == OrderType.تركيب);
        if (from.HasValue) { var a = from.Value; cohort = cohort.Where(o => o.CreatedAtUtc >= a); }
        if (toExcl.HasValue) { var b = toExcl.Value; cohort = cohort.Where(o => o.CreatedAtUtc < b); }

        var originals = await cohort.Select(o => new
        {
            o.Id,
            o.CreatedAtUtc,
            TechId = o.TechHistory.Where(t => t.Status == TechStatus.current && t.TechType == OrderTechType.فني && !t.IsDeleted)
                        .OrderByDescending(t => t.Id).Select(t => (long?)t.TechId).FirstOrDefault(),
            TechName = o.TechHistory.Where(t => t.Status == TechStatus.current && t.TechType == OrderTechType.فني && !t.IsDeleted)
                        .OrderByDescending(t => t.Id).Select(t => t.Tech!.Name).FirstOrDefault()
        }).ToListAsync();

        var maintenance = await _db.Order.AsNoTracking()
            .Where(m => !m.IsDeleted && m.OldOrderId != null && m.Status != OrderStatus.ملغي && m.Status != OrderStatus.محذوف
                        && cohort.Any(o => o.Id == m.OldOrderId))
            .Select(m => new { m.Id, OldId = m.OldOrderId!.Value, m.CreatedAtUtc })
            .ToListAsync();

        var originalById = originals.ToDictionary(o => o.Id);
        var valid = maintenance
            .Where(m => originalById.TryGetValue(m.OldId, out var o)
                        && m.CreatedAtUtc >= o.CreatedAtUtc
                        && (m.CreatedAtUtc - o.CreatedAtUtc).TotalDays < MaintenanceWindowDays)
            .ToList();

        var rows = new List<MaintenanceTechRowDto>();
        foreach (var g in originals.GroupBy(o => o.TechId ?? 0))
        {
            var done = g.Select(o => o.Id).OrderBy(x => x).ToList();
            var doneSet = done.ToHashSet();
            var mine = valid.Where(m => doneSet.Contains(m.OldId)).OrderBy(m => m.Id).ToList();

            rows.Add(new MaintenanceTechRowDto
            {
                TechId = g.Key,
                TechName = g.Key == 0 ? "بدون فني" : g.Select(o => o.TechName).FirstOrDefault(n => !string.IsNullOrEmpty(n)) ?? g.Key.ToString(),
                MaintenanceCount = mine.Count,
                MaintenanceOrderIds = mine.Select(m => m.Id).ToList(),
                OriginalOrderIds = mine.Select(m => m.OldId).Distinct().OrderBy(x => x).ToList(),
                DoneCount = done.Count,
                DoneOrderIds = done,
                Percent = Percent(mine.Count, done.Count)
            });
        }

        var dto = new MaintenanceReportDto
        {
            DateFrom = f.DateFrom?.Date,
            DateTo = f.DateTo?.Date,
            WindowDays = MaintenanceWindowDays,
            Rows = rows.OrderBy(r => r.TechId == 0 ? 1 : 0).ThenBy(r => r.TechName, StringComparer.Ordinal).ToList()
        };
        var maintTotal = dto.Rows.Sum(r => r.MaintenanceCount);
        var doneTotal = dto.Rows.Sum(r => r.DoneCount);
        dto.Total = new MaintenanceTechRowDto
        {
            TechName = "الاجمالي",
            MaintenanceCount = maintTotal,
            DoneCount = doneTotal,
            Percent = Percent(maintTotal, doneTotal)
        };
        return ServiceResult<MaintenanceReportDto>.Ok(dto);
    }

    private static decimal Percent(int maintenance, int done) =>
        done == 0 ? 0 : Math.Round(maintenance * 100m / done, 2);

    public async Task<ExportResult> ExportMaintenanceAsync(ReportDateFilterDto f)
    {
        var r = await GetMaintenanceAsync(f);
        if (!r.Succeeded) return Fail(r.Errors);
        var d = r.Data!;

        var cols = new List<XCol>
        {
            new("الفني", 24),
            new("عدد الصيانات", 12, null, true),
            new("ارقام اوردرات الصيانة", 34, null, false, true),
            new("ارقام الاوردرات الاصلية", 34, null, false, true),
            new("اجمالي الاوردرات التامة", 14, null, true),
            new("ارقام الاوردرات التامة", 50, null, false, true),
            new("نسبة الصيانة %", 13, TableExcel.Money, true),
        };
        var rows = d.Rows.Select(x => new object?[]
        {
            x.TechName, x.MaintenanceCount, Ids(x.MaintenanceOrderIds), Ids(x.OriginalOrderIds), x.DoneCount, Ids(x.DoneOrderIds), x.Percent
        }).ToList();
        var totals = new object?[] { null, d.Total.MaintenanceCount, null, null, d.Total.DoneCount, null, d.Total.Percent };

        var sub = Sub(Period(f.DateFrom, f.DateTo),
            $"الصيانات خلال اقل من {d.WindowDays} يوم من انشاء الاوردر الاصلي — الفترة بتاريخ انشاء الاوردر الاصلي");
        var bytes = TableExcel.Build("الصيانات", "تقرير الصيانات اقل من شهر للتركيب", sub, cols, rows, totals);
        return new ExportResult(bytes, null, $"maintenance-{Stamp}.xlsx");
    }
}
