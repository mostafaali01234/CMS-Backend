using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Services;

// 12: audit log (api_data_change_log) by user / operation / date range
//
// The table has no "operation" column, so it is worked out from what the audit interceptor writes:
//   Add    = a "(created)" marker row for the record
//   Delete = the IsDeleted flag changed to True (records are soft deleted)
//   Update = any other field change
// Rows of one record written by one request are shown as one line with the list of changed fields.
// Hard deletes (child rows such as order items / installments) are not written to this table.
public partial class ReportService
{
    private const string AuditCreatedField = "(created)";
    private const int AuditMaxRows = 20000;
    private const int AuditValueMax = 300;

    // written on every save, they say nothing about what the user changed
    private static readonly HashSet<string> AuditNoiseFields = new(StringComparer.OrdinalIgnoreCase)
        { "UpdatedAtUtc", "UpdatedBy", "DeletedAtUtc" };

    private static string? CutValue(string? s) =>
        s is { Length: > AuditValueMax } ? s[..AuditValueMax] + "..." : s;

    private static string OperationLabel(AuditOperation o) => o switch
    {
        AuditOperation.Add => "اضافة",
        AuditOperation.Delete => "حذف",
        _ => "تعديل"
    };

    // one page of the log, with the add / update / delete totals of the whole filter
    public async Task<ServiceResult<PagedResult<AuditLogRowDto, AuditLogSummaryDto>>> GetAuditLogAsync(AuditLogFilterDto f)
    {
        var r = await BuildAuditLogAsync(f);
        if (!r.Succeeded) return ServiceResult<PagedResult<AuditLogRowDto, AuditLogSummaryDto>>.Failed(r.Errors);
        var all = r.Data!;

        var page = all.PageOf(f);
        return ServiceResult<PagedResult<AuditLogRowDto, AuditLogSummaryDto>>.Ok(new PagedResult<AuditLogRowDto, AuditLogSummaryDto>
        {
            Items = page.Items,
            Total = page.Total,
            Page = page.Page,
            PageSize = page.PageSize,
            Summary = new AuditLogSummaryDto
            {
                Add = all.Count(x => x.Operation == AuditOperation.Add),
                Update = all.Count(x => x.Operation == AuditOperation.Update),
                Delete = all.Count(x => x.Operation == AuditOperation.Delete)
            }
        });
    }

    // every line of the filter (the Excel export is not paged)
    private async Task<ServiceResult<List<AuditLogRowDto>>> BuildAuditLogAsync(AuditLogFilterDto f)
    {
        var range = new ReportDateFilterDto { DateFrom = f.DateFrom, DateTo = f.DateTo };
        var rangeError = CheckRange(range);
        if (rangeError is not null) return ServiceResult<List<AuditLogRowDto>>.Failed(rangeError);
        var (from, toExcl) = OrderRange(range);          // changed_date is stored in UTC

        var q = _db.ApiDataChangeLog.AsNoTracking().AsQueryable();
        if (from.HasValue) { var a = from.Value; q = q.Where(x => x.ChangedDate >= a); }
        if (toExcl.HasValue) { var b = toExcl.Value; q = q.Where(x => x.ChangedDate < b); }

        if (f.EmployeeId.HasValue)
        {
            var empId = f.EmployeeId.Value;
            var uid = await _db.Employee.AsNoTracking().Where(e => e.Id == empId).Select(e => e.UserId).FirstOrDefaultAsync();
            if (string.IsNullOrEmpty(uid)) return ServiceResult<List<AuditLogRowDto>>.Failed("الموظف غير موجود او ليس له مستخدم");
            q = q.Where(x => x.UserId == uid);
        }

        // cheap pre-filters: only the rows that can decide the operation
        if (f.Operation == AuditOperation.Add) q = q.Where(x => x.FieldName == AuditCreatedField);
        else if (f.Operation == AuditOperation.Delete) q = q.Where(x => x.FieldName == "IsDeleted" && x.NewValue == "True");
        else if (f.Operation == AuditOperation.Update) q = q.Where(x => x.FieldName != AuditCreatedField);

        if (await q.CountAsync() > AuditMaxRows)
            return ServiceResult<List<AuditLogRowDto>>.Failed($"النتيجة اكبر من {AuditMaxRows} سجل، قلل الفترة او اختر مستخدم او عملية");

        var raw = await q.OrderBy(x => x.ChangedDate).ThenBy(x => x.Id).Select(x => new
        {
            x.LogId,
            x.TableName,
            x.PrimaryKeyValue,
            x.FieldName,
            x.OldValue,
            x.NewValue,
            x.UserId,
            x.ChangedDate,
            Endpoint = x.ApiActivityLog != null ? x.ApiActivityLog.Endpoint : null,
            Method = x.ApiActivityLog != null ? x.ApiActivityLog.HttpMethod : null
        }).ToListAsync();

        var uids = raw.Select(x => x.UserId).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
        var names = (await _db.Employee.AsNoTracking().Where(e => uids.Contains(e.UserId))
                .Select(e => new { e.UserId, e.Name }).ToListAsync())
            .GroupBy(e => e.UserId).ToDictionary(g => g.Key, g => g.First().Name);

        string UserName(string? id) =>
            string.IsNullOrEmpty(id) ? "-" : names.TryGetValue(id, out var n) ? n : id == "system" ? "النظام" : id;

        // rows of one request + table + record = one line
        var groups = raw.GroupBy(x =>
            $"{(x.LogId?.ToString() ?? x.UserId + "@" + x.ChangedDate.ToString("yyyyMMddHHmmss"))}|{x.TableName}|{x.PrimaryKeyValue}");

        var list = new List<AuditLogRowDto>();
        foreach (var g in groups)
        {
            var first = g.First();
            var op = g.Any(x => x.FieldName == AuditCreatedField) ? AuditOperation.Add
                   : g.Any(x => x.FieldName == "IsDeleted" && x.NewValue == "True") ? AuditOperation.Delete
                   : AuditOperation.Update;
            if (f.Operation.HasValue && op != f.Operation.Value) continue;

            list.Add(new AuditLogRowDto
            {
                ChangedAt = g.Min(x => x.ChangedDate).ToLocalTime(),
                UserName = UserName(first.UserId),
                Operation = op,
                TableName = first.TableName,
                RecordId = first.PrimaryKeyValue,
                HttpMethod = first.Method ?? "",
                Endpoint = first.Endpoint ?? "",
                Changes = op == AuditOperation.Update
                    ? g.Where(x => x.FieldName != AuditCreatedField && !AuditNoiseFields.Contains(x.FieldName))
                       .Select(x => new AuditLogChangeDto { Field = x.FieldName, OldValue = CutValue(x.OldValue), NewValue = CutValue(x.NewValue) })
                       .ToList()
                    : new List<AuditLogChangeDto>()
            });
        }

        return ServiceResult<List<AuditLogRowDto>>.Ok(list.OrderByDescending(x => x.ChangedAt).ToList());
    }

    public async Task<ExportResult> ExportAuditLogAsync(AuditLogFilterDto f)
    {
        var r = await BuildAuditLogAsync(f);
        if (!r.Succeeded) return Fail(r.Errors);
        var d = r.Data!;

        var cols = new List<XCol>
        {
            new("التاريخ والوقت", 18, "yyyy-MM-dd HH:mm", true), new("المستخدم", 20), new("العملية", 10, null, true),
            new("الجدول", 22), new("رقم السجل", 12, null, true), new("التغييرات", 60, null, false, true), new("الطلب", 34),
        };
        var rows = d.Select(x => new object?[]
        {
            x.ChangedAt, x.UserName, OperationLabel(x.Operation), x.TableName, x.RecordId,
            string.Join("\n", x.Changes.Select(c => $"{c.Field}: {c.OldValue ?? "-"} ← {c.NewValue ?? "-"}")),
            $"{x.HttpMethod} {x.Endpoint}".Trim()
        }).ToList();

        var extra = $"{d.Count} سجل — اضافة {d.Count(x => x.Operation == AuditOperation.Add)}"
                  + $" / تعديل {d.Count(x => x.Operation == AuditOperation.Update)}"
                  + $" / حذف {d.Count(x => x.Operation == AuditOperation.Delete)}";
        var bytes = TableExcel.Build("سجل التغييرات", "سجل التغييرات", Sub(Period(f.DateFrom, f.DateTo), extra), cols, rows);
        return new ExportResult(bytes, null, $"audit-log-{Stamp}.xlsx");
    }
}