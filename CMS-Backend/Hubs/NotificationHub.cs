using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CMS.Api.Hubs;

/// <summary>
/// The "live line" between the server and each open browser tab.
/// The browser connects once after login; the server never waits for it to ask: it pushes.
/// The hub has no methods the browser can call on purpose: everything flows server -> browser,
/// and every write (sending a message, marking as read) still goes through the normal REST controllers
/// where all the validation and authorization already live.
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class NotificationHub : Hub
{
    // every connection of one user (several tabs / devices) joins the group named after the user id,
    // so "send to user X" is simply "send to group X"
    public static string GroupOf(string userId) => "user:" + userId;

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("UserId")?.Value;
        if (!string.IsNullOrEmpty(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupOf(userId));
        await base.OnConnectedAsync();
    }
    // no OnDisconnected needed: SignalR removes the connection from its groups by itself
}
