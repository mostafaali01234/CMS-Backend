using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Domain.Enums;

namespace CMS.Application.Interfaces;

/// <summary>"order N went from Old to New": the audience is worked out later, by the worker</summary>
public record OrderStatusChange(long OrderId, OrderStatus Old, OrderStatus New);

/// <summary>A job on the notification queue.</summary>
public record NotificationRequest(
    NotificationType Type,
    string Title,
    string Body,
    string? EntityType = null,
    long? EntityId = null,
    IReadOnlyList<string>? UserIds = null,        // explicit recipients...
    OrderStatusChange? OrderChange = null,        // ...or: seller + current tech + admins of this order
    string? ExcludeUserId = null);                // the person who did it: they don't need telling

public interface INotificationQueue
{
    void Enqueue(NotificationRequest request);
    System.Threading.Channels.ChannelReader<NotificationRequest> Reader { get; }
}

/// <summary>Called from the code that changes an order. Never blocks, never throws.</summary>
public interface IOrderNotifier
{
    void OrderStatusChanged(long orderId, OrderStatus oldStatus, OrderStatus newStatus);
}

/// <summary>Sends something to a browser that is connected right now (implemented with SignalR in the API project).</summary>
public interface IRealtimePusher
{
    Task PushNotificationAsync(string userId, NotificationDto notification);
    Task PushMessageAsync(string userId, UserMessageDto message);
}

public interface INotificationService
{
    // ---- used by the background worker ----
    /// <summary>saves one row per recipient. null = the order is not (yet) in the expected status: try again shortly</summary>
    Task<List<(string UserId, NotificationDto Dto)>?> CreateAsync(NotificationRequest request);

    // ---- used by the controller (always "my" notifications) ----
    Task<ServiceResult<PagedResult<NotificationDto>>> GetMineAsync(string userId, NotificationFilterDto filter);
    Task<ServiceResult<int>> GetUnreadCountAsync(string userId);
    Task<ServiceResult<bool>> MarkReadAsync(string userId, long id);
    Task<ServiceResult<int>> MarkAllReadAsync(string userId);
}

public interface IUserMessageService
{
    Task<ServiceResult<UserMessageDto>> SendAsync(string senderUserId, SendMessageDto dto);
    Task<ServiceResult<List<ConversationDto>>> GetConversationsAsync(string userId);
    Task<ServiceResult<PagedResult<UserMessageDto>>> GetThreadAsync(string userId, MessageThreadFilterDto filter);
    Task<ServiceResult<int>> MarkThreadReadAsync(string userId, string withUserId);
    Task<ServiceResult<List<ContactDto>>> GetContactsAsync(string userId);
}
