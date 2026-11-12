using CMS.Application.DTOs;
using CMS.Application.Interfaces;
using CMS_Backend.Controllers.Base;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CMS.Api.Controllers;

/// <summary>Direct messages between users. The sender is always taken from the token, never from the request body.</summary>
[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class UserMessageController : ApiControllerBase
{
    private readonly IUserMessageService _service;
    public UserMessageController(IUserMessageService service) { _service = service; }

    private string? Me => User.FindFirstValue("UserId");

    // GET api/UserMessage/contacts  -> people I can start a chat with
    [HttpGet("contacts")]
    public async Task<IActionResult> Contacts() =>
        Me is null ? Unauthorized() : ToActionResult(await _service.GetContactsAsync(Me));

    // GET api/UserMessage/conversations  -> my chats list (last message + unread count each)
    [HttpGet("conversations")]
    public async Task<IActionResult> Conversations() =>
        Me is null ? Unauthorized() : ToActionResult(await _service.GetConversationsAsync(Me));

    // GET api/UserMessage?withUserId=...&orderId=&page=1&pageSize=30  -> one chat, newest first
    [HttpGet]
    public async Task<IActionResult> Thread([FromQuery] MessageThreadFilterDto filter) =>
        Me is null ? Unauthorized() : ToActionResult(await _service.GetThreadAsync(Me, filter));

    // POST api/UserMessage   { "recipient_user_id": "...", "body": "...", "order_id": 12 }
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendMessageDto dto) =>
        Me is null ? Unauthorized() : ToActionResult(await _service.SendAsync(Me, dto));

    // POST api/UserMessage/read?withUserId=...  -> I opened that chat: mark their messages as read
    [HttpPost("read")]
    public async Task<IActionResult> MarkRead([FromQuery] string withUserId) =>
        Me is null ? Unauthorized() : ToActionResult(await _service.MarkThreadReadAsync(Me, withUserId));
}
