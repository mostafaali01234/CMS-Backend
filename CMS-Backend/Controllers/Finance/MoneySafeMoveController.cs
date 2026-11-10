using CMS.Application.DTOs;
using CMS.Application.Interfaces;
using CMS_Backend.Controllers.Base;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class MoneySafeMoveController : ApiControllerBase
{
    private readonly IMoneySafeMoveService _service;
    public MoneySafeMoveController(IMoneySafeMoveService service) { _service = service; }

    private bool IsAdmin => User.IsInRole("SuperAdmin") || User.IsInRole("Admin");

    // GET: api/MoneySafeMove/safes
    // the "select" on the page: all safes for admins, only the safes you manage otherwise (with current balance)
    [HttpGet("safes")]
    public async Task<IActionResult> GetSafes()
    {
        var userId = User.FindFirstValue("UserId");
        if (userId is null) return Unauthorized();
        return ToActionResult(await _service.GetAccessibleSafesAsync(userId, IsAdmin));
    }

    // GET: api/MoneySafeMove?moneySafeId=3&dateFrom=2026-11-01&dateTo=2026-11-10&page=1&pageSize=20
    [HttpGet]
    public async Task<IActionResult> GetMoves([FromQuery] MoneySafeMoveFilterDto filter)
    {
        var userId = User.FindFirstValue("UserId");
        if (userId is null) return Unauthorized();
        return ToActionResult(await _service.GetMovesAsync(filter, userId, IsAdmin));
    }

    // GET: api/MoneySafeMove/export?moneySafeId=3&dateFrom=2026-11-01&dateTo=2026-11-10
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] MoneySafeMoveFilterDto filter)
    {
        var userId = User.FindFirstValue("UserId");
        if (userId is null) return Unauthorized();

        var r = await _service.ExportAsync(filter, userId, IsAdmin);
        return r.Error is not null
            ? BadRequest(new { message = r.Error })
            : File(r.Bytes!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", r.FileName);
    }
}
