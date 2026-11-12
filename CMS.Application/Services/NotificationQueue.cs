using CMS.Application.Interfaces;
using System.Threading.Channels;

namespace CMS.Application.Services;

/// <summary>
/// In-memory hand-off between the request (which only drops a note in) and the background worker
/// (which does the database work and the live push). Registered as a singleton.
/// </summary>
public class NotificationQueue : INotificationQueue
{
    private readonly Channel<NotificationRequest> _channel =
        Channel.CreateBounded<NotificationRequest>(new BoundedChannelOptions(5_000)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.DropWrite   // a full queue must never slow a request down
        });

    public void Enqueue(NotificationRequest request) => _channel.Writer.TryWrite(request);
    public ChannelReader<NotificationRequest> Reader => _channel.Reader;
}

public class OrderNotifier : IOrderNotifier
{
    private readonly INotificationQueue _queue;
    private readonly Interfaces.Configuration.ICurrentUser _currentUser;

    public OrderNotifier(INotificationQueue queue, Interfaces.Configuration.ICurrentUser currentUser)
    {
        _queue = queue;
        _currentUser = currentUser;
    }

    public void OrderStatusChanged(long orderId, Domain.Enums.OrderStatus oldStatus, Domain.Enums.OrderStatus newStatus)
    {
        if (oldStatus == newStatus) return;
        _queue.Enqueue(new NotificationRequest(
            Domain.Enums.NotificationType.OrderStatusChanged,
            Title: $"الطلب رقم {orderId}",
            Body: "",                                   // the worker writes the text (it knows the customer, who did it...)
            EntityType: "order",
            EntityId: orderId,
            OrderChange: new OrderStatusChange(orderId, oldStatus, newStatus),
            ExcludeUserId: _currentUser.UserId));
    }
}
