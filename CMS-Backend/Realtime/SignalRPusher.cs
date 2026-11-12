using CMS.Api.Hubs;
using CMS.Application.DTOs;
using CMS.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace CMS.Api.Realtime;

/// <summary>Implements the Application layer's IRealtimePusher with SignalR, so services never need to know about SignalR.</summary>
public class SignalRPusher : IRealtimePusher
{
    private readonly IHubContext<NotificationHub> _hub;
    public SignalRPusher(IHubContext<NotificationHub> hub) { _hub = hub; }

    // "notification" and "message" are the event names the browser listens for
    public Task PushNotificationAsync(string userId, NotificationDto notification) =>
        _hub.Clients.Group(NotificationHub.GroupOf(userId)).SendAsync("notification", notification);

    public Task PushMessageAsync(string userId, UserMessageDto message) =>
        _hub.Clients.Group(NotificationHub.GroupOf(userId)).SendAsync("message", message);
}
