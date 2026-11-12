using CMS.Application.DTOs;
using CMS.Application.Interfaces;
using CMS_Backend.Controllers.Base;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CMS.Api.Controllers;

/// <summary>The bell icon. Every endpoint only ever sees the caller's own notifications.</summary>
[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]   // any signed-in user
public class NotificationController : ApiControllerBase
{
    private readonly INotificationService _service;
    public NotificationController(INotificationService service) { _service = service; }

    private string? Me => User.FindFirstValue("UserId");

    // GET api/Notification?unreadOnly=true&page=1&pageSize=20
    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] NotificationFilterDto filter) =>
        Me is null ? Unauthorized() : ToActionResult(await _service.GetMineAsync(Me, filter));

    // GET api/Notification/unread-count  -> the number on the bell
    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount() =>
        Me is null ? Unauthorized() : ToActionResult(await _service.GetUnreadCountAsync(Me));

    // POST api/Notification/5/read
    [HttpPost("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id) =>
        Me is null ? Unauthorized() : ToActionResult(await _service.MarkReadAsync(Me, id));

    // POST api/Notification/read-all
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead() =>
        Me is null ? Unauthorized() : ToActionResult(await _service.MarkAllReadAsync(Me));
}
