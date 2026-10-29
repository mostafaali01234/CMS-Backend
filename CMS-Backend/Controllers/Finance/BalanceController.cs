using CMS.Application.Interfaces;
using CMS_Backend.Controllers.Base;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/balance")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
public class BalanceController : ApiControllerBase
{
    private readonly IBalanceService _service;
    public BalanceController(IBalanceService service) { _service = service; }

    // GET: api/balance/supplier
    [HttpGet("supplier")]
    public async Task<IActionResult> Suppliers() => ToActionResult(await _service.GetSupplierBalancesAsync());

    // GET: api/balance/supplier/5?from=2026-01-01&to=2026-12-31
    [HttpGet("supplier/{id:long}")]
    public async Task<IActionResult> SupplierStatement(long id, [FromQuery] DateTime? from, [FromQuery] DateTime? to) =>
        ToActionResult(await _service.GetSupplierStatementAsync(id, from, to));

    [HttpGet("customer")]
    public async Task<IActionResult> Customers() => ToActionResult(await _service.GetCustomerBalancesAsync());

    [HttpGet("customer/{id:long}")]
    public async Task<IActionResult> CustomerStatement(long id, [FromQuery] DateTime? from, [FromQuery] DateTime? to) =>
        ToActionResult(await _service.GetCustomerStatementAsync(id, from, to));
}