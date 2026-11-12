using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;
using CMS.Application.Interfaces;
using CMS.Application.Interfaces.Configuration;
using CMS.Domain.Enums;
using CMS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMS.Application.Services;

public class UserMessageService : IUserMessageService
{
    private const int MaxBodyLength = 2000;

    private readonly IAppDbContext _db;
    private readonly IUserDirectory _users;
    private readonly INotificationQueue _queue;
    private readonly IRealtimePusher _pusher;
    private readonly ILogger<UserMessageService> _logger;

    public UserMessageService(IAppDbContext db, IUserDirectory users, INotificationQueue queue,
                              IRealtimePusher pusher, ILogger<UserMessageService> logger)
    {
        _db = db;
        _users = users;
        _queue = queue;
        _pusher = pusher;
        _logger = logger;
    }

    private static DateTime Utc(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc);

    // user id -> the employee's name ("" when the account has no employee card, e.g. the super admin)
    private async Task<Dictionary<string, string>> NamesAsync(IEnumerable<string> userIds)
    {
        var ids = userIds.Distinct().ToList();
        return (await _db.Employee.AsNoTracking()
                .Where(e => !e.IsDeleted && ids.Contains(e.UserId))
                .Select(e => new { e.UserId, e.Name }).ToListAsync())
            .GroupBy(e => e.UserId).ToDictionary(g => g.Key, g => g.First().Name);
    }

    private static UserMessageDto ToDto(UserMessage m, string me, IReadOnlyDictionary<string, string> names) => new()
    {
        Id = m.Id,
        SenderUserId = m.SenderUserId,
        SenderName = names.GetValueOrDefault(m.SenderUserId, ""),
        RecipientUserId = m.RecipientUserId,
        OrderId = m.OrderId,
        Body = m.Body,
        CreatedAt = Utc(m.CreatedAtUtc),
        ReadAt = m.ReadAtUtc.HasValue ? Utc(m.ReadAtUtc.Value) : null,
        IsMine = m.SenderUserId == me
    };

    // -------------------------------------------------------------- send
    public async Task<ServiceResult<UserMessageDto>> SendAsync(string senderUserId, SendMessageDto dto)
    {
        var body = (dto.Body ?? "").Trim();
        if (body.Length == 0) return ServiceResult<UserMessageDto>.Failed("Message cannot be empty");
        if (body.Length > MaxBodyLength) return ServiceResult<UserMessageDto>.Failed($"Message cannot be longer than {MaxBodyLength} characters");
        if (string.IsNullOrWhiteSpace(dto.RecipientUserId)) return ServiceResult<UserMessageDto>.Failed("RecipientUserId is required");
        if (dto.RecipientUserId == senderUserId) return ServiceResult<UserMessageDto>.Failed("You cannot message yourself");
        if (!await _users.UserExistsAsync(dto.RecipientUserId)) return ServiceResult<UserMessageDto>.NotFound("Recipient not found");

        if (dto.OrderId.HasValue && !await _db.Order.AnyAsync(o => o.Id == dto.OrderId.Value))
            return ServiceResult<UserMessageDto>.Failed($"Order {dto.OrderId} was not found");

        var message = new UserMessage
        {
            SenderUserId = senderUserId,
            RecipientUserId = dto.RecipientUserId,
            OrderId = dto.OrderId,
            Body = body,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.UserMessage.Add(message);
        await _db.SaveChangesAsync();

        var names = await NamesAsync(new[] { senderUserId, dto.RecipientUserId });
        var result = ToDto(message, senderUserId, names);

        // 1) the open chat window of the recipient gets the message itself, instantly
        // 2) their bell gets a "new message" notification (through the queue, like every other notification)
        try
        {
            await _pusher.PushMessageAsync(dto.RecipientUserId, ToDto(message, dto.RecipientUserId, names));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live push of message {MessageId} failed (it is saved, the recipient will see it on refresh)", message.Id);
        }

        var senderName = names.GetValueOrDefault(senderUserId, "");
        _queue.Enqueue(new NotificationRequest(
            NotificationType.NewMessage,
            Title: string.IsNullOrEmpty(senderName) ? "رسالة جديدة" : $"رسالة جديدة من {senderName}",
            Body: body.Length <= 100 ? body : body[..100] + "...",
            EntityType: "message",
            EntityId: message.Id,
            UserIds: new[] { dto.RecipientUserId }));

        return ServiceResult<UserMessageDto>.Ok(result);
    }

    // -------------------------------------------------------------- chats list
    public async Task<ServiceResult<List<ConversationDto>>> GetConversationsAsync(string me)
    {
        var mine = _db.UserMessage.AsNoTracking()
            .Where(m => !m.IsDeleted && (m.SenderUserId == me || m.RecipientUserId == me));

        // one row per other person: id of the last message + how many of theirs I have not read
        var groups = await mine
            .GroupBy(m => m.SenderUserId == me ? m.RecipientUserId : m.SenderUserId)
            .Select(g => new
            {
                Other = g.Key,
                LastId = g.Max(m => m.Id),
                Unread = g.Count(m => m.RecipientUserId == me && m.ReadAtUtc == null)
            })
            .ToListAsync();

        var lastIds = groups.Select(g => g.LastId).ToList();
        var lastMessages = await mine.Where(m => lastIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id);
        var names = await NamesAsync(groups.Select(g => g.Other).Append(me));

        var list = groups
            .Select(g => new ConversationDto
            {
                UserId = g.Other,
                UserName = names.GetValueOrDefault(g.Other, ""),
                UnreadCount = g.Unread,
                LastMessage = lastMessages.TryGetValue(g.LastId, out var m) ? ToDto(m, me, names) : null
            })
            .OrderByDescending(c => c.LastMessage?.CreatedAt)
            .ToList();

        return ServiceResult<List<ConversationDto>>.Ok(list);
    }

    // -------------------------------------------------------------- one chat
    public async Task<ServiceResult<PagedResult<UserMessageDto>>> GetThreadAsync(string me, MessageThreadFilterDto f)
    {
        if (string.IsNullOrWhiteSpace(f.WithUserId))
            return ServiceResult<PagedResult<UserMessageDto>>.Failed("WithUserId is required");

        var other = f.WithUserId;
        var q = _db.UserMessage.AsNoTracking().Where(m => !m.IsDeleted &&
            ((m.SenderUserId == me && m.RecipientUserId == other) || (m.SenderUserId == other && m.RecipientUserId == me)));
        if (f.OrderId.HasValue) q = q.Where(m => m.OrderId == f.OrderId.Value);

        // newest first (the UI reverses it for display, and "load older" is simply the next page)
        var page = await q.OrderByDescending(m => m.Id).ToPagedAsync(f);
        var names = await NamesAsync(new[] { me, other });

        return ServiceResult<PagedResult<UserMessageDto>>.Ok(new PagedResult<UserMessageDto>
        {
            Items = page.Items.Select(m => ToDto(m, me, names)).ToList(),
            Total = page.Total,
            Page = page.Page,
            PageSize = page.PageSize
        });
    }

    public async Task<ServiceResult<int>> MarkThreadReadAsync(string me, string withUserId)
    {
        var now = DateTime.UtcNow;

        var unreadMessages = _db.UserMessage
            .Where(m => m.RecipientUserId == me && m.SenderUserId == withUserId && m.ReadAtUtc == null);

        var n = await EntityFrameworkQueryableExtensions.ExecuteUpdateAsync(
            unreadMessages,
            s => s.SetProperty(m => m.ReadAtUtc, now));

        // their "new message" bells are no longer news either
        var unreadBells = _db.Notification
            .Where(x => x.RecipientUserId == me && x.ReadAtUtc == null && x.EntityType == "message"
                     && _db.UserMessage.Any(m => m.Id == x.EntityId && m.SenderUserId == withUserId));

        await EntityFrameworkQueryableExtensions.ExecuteUpdateAsync(
            unreadBells,
            s => s.SetProperty(x => x.ReadAtUtc, now));

        return ServiceResult<int>.Ok(n);
    }

    // -------------------------------------------------------------- who can I write to
    public async Task<ServiceResult<List<ContactDto>>> GetContactsAsync(string me)
    {
        var list = await _db.Employee.AsNoTracking()
            .Where(e => !e.IsDeleted && e.UserId != null && e.UserId != "" && e.UserId != me)
            .OrderBy(e => e.Name)
            .Select(e => new ContactDto { UserId = e.UserId, Name = e.Name })
            .ToListAsync();
        return ServiceResult<List<ContactDto>>.Ok(list);
    }
}
