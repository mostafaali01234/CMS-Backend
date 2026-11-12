using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;
using CMS.Application.Interfaces;
using CMS.Application.Interfaces.Configuration;
using CMS.Domain.Enums;
using CMS.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IAppDbContext _db;
    private readonly IUserDirectory _users;

    public NotificationService(IAppDbContext db, IUserDirectory users)
    {
        _db = db;
        _users = users;
    }

    // dates come out of SQL "unspecified"; mark them UTC so the JSON ends with Z and the browser shows local time
    private static DateTime Utc(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc);

    private static NotificationDto ToDto(Notification n) => new()
    {
        Id = n.Id,
        Type = n.Type,
        Title = n.Title,
        Body = n.Body,
        EntityType = n.EntityType,
        EntityId = n.EntityId,
        CreatedAt = Utc(n.CreatedAtUtc),
        ReadAt = n.ReadAtUtc.HasValue ? Utc(n.ReadAtUtc.Value) : null,
        IsRead = n.ReadAtUtc.HasValue
    };

    // ------------------------------------------------------------------ writing
    public async Task<List<(string UserId, NotificationDto Dto)>?> CreateAsync(NotificationRequest r)
    {
        var recipients = new HashSet<string>();
        if (r.UserIds is not null) recipients.UnionWith(r.UserIds);

        var title = r.Title;
        var body = r.Body;

        if (r.OrderChange is { } change)
        {
            var order = await _db.Order.AsNoTracking()
                .Where(o => o.Id == change.OrderId)
                .Select(o => new
                {
                    o.Status,
                    CustomerName = o.Customer != null ? o.Customer.Name : "",
                    SellerUserId = o.Seller != null ? o.Seller.UserId : null,
                    TechUserId = o.TechHistory
                        .Where(t => !t.IsDeleted && t.Status == Domain.Enums.TechStatus.current && t.Tech != null)
                        .Select(t => t.Tech!.UserId).FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (order is null) return new();                                  // order is gone: nothing to tell
            if (order.Status != change.New) return null;                      // save not committed yet (or rolled back)

            if (!string.IsNullOrEmpty(order.SellerUserId)) recipients.Add(order.SellerUserId);
            if (!string.IsNullOrEmpty(order.TechUserId)) recipients.Add(order.TechUserId);
            recipients.UnionWith(await _users.GetUserIdsInRolesAsync("SuperAdmin", "Admin"));

            var by = "";
            if (!string.IsNullOrEmpty(r.ExcludeUserId))
            {
                var name = await _db.Employee.Where(e => e.UserId == r.ExcludeUserId && !e.IsDeleted)
                    .Select(e => e.Name).FirstOrDefaultAsync();
                if (!string.IsNullOrEmpty(name)) by = $" بواسطة {name}";
            }

            title = $"تغيّرت حالة الطلب رقم {change.OrderId}";
            body = $"الطلب رقم {change.OrderId} ({order.CustomerName}) من {change.Old.ToString().Replace('_', ' ')} الى {change.New.ToString().Replace('_', ' ')}{by}";
        }

        // the person who did it knows already
        if (!string.IsNullOrEmpty(r.ExcludeUserId)) recipients.Remove(r.ExcludeUserId);
        recipients.RemoveWhere(string.IsNullOrWhiteSpace);
        if (recipients.Count == 0) return new();

        var now = DateTime.UtcNow;
        var rows = recipients.Select(u => new Notification
        {
            RecipientUserId = u,
            Type = r.Type,
            Title = Cut(title, 200),
            Body = Cut(body, 1000),
            EntityType = r.EntityType,
            EntityId = r.EntityId,
            CreatedAtUtc = now
        }).ToList();

        _db.Notification.AddRange(rows);
        await _db.SaveChangesAsync();

        return rows.Select(n => (n.RecipientUserId, ToDto(n))).ToList();
    }

    private static string Cut(string s, int max) => s.Length <= max ? s : s[..max];

    // ------------------------------------------------------------------ reading ("my" notifications only)
    public async Task<ServiceResult<PagedResult<NotificationDto>>> GetMineAsync(string userId, NotificationFilterDto f)
    {
        var q = _db.Notification.AsNoTracking().Where(n => n.RecipientUserId == userId);
        if (f.UnreadOnly) q = q.Where(n => n.ReadAtUtc == null);

        var page = await q.OrderByDescending(n => n.CreatedAtUtc).ThenByDescending(n => n.Id).ToPagedAsync(f);

        return ServiceResult<PagedResult<NotificationDto>>.Ok(new PagedResult<NotificationDto>
        {
            Items = page.Items.Select(ToDto).ToList(),
            Total = page.Total,
            Page = page.Page,
            PageSize = page.PageSize
        });
    }

    public async Task<ServiceResult<int>> GetUnreadCountAsync(string userId) =>
        ServiceResult<int>.Ok(await _db.Notification.CountAsync(n => n.RecipientUserId == userId && n.ReadAtUtc == null));

    public async Task<ServiceResult<bool>> MarkReadAsync(string userId, long id)
    {
        // the WHERE includes the user id, so nobody can mark (or even detect) somebody else's notification
        var n = await _db.Notification.FirstOrDefaultAsync(x => x.Id == id && x.RecipientUserId == userId);
        if (n is null) return ServiceResult<bool>.NotFound("Notification not found");
        if (n.ReadAtUtc is null)
        {
            n.ReadAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<int>> MarkAllReadAsync(string userId)
    {
        var now = DateTime.UtcNow;
        var query = _db.Notification
            .Where(x => x.RecipientUserId == userId && x.ReadAtUtc == null);

        var n = await EntityFrameworkQueryableExtensions.ExecuteUpdateAsync(
            query,
            s => s.SetProperty(x => x.ReadAtUtc, now));
        return ServiceResult<int>.Ok(n);
    }
}
