using CMS_Backend.Controllers.Base;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Api.Controllers;

// CMS.Api/Controllers/EmployeeBalanceController.cs
[ApiController]
[Route("api/employee-balance")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class EmployeeBalanceController : ApiControllerBase
{
    private readonly IEmployeeBalanceService _service;
    public EmployeeBalanceController(IEmployeeBalanceService service) { _service = service; }

    private bool IsAdmin => User.IsInRole("SuperAdmin") || User.IsInRole("Admin");
    // same "EmployeeId" claim assumption as PayrollReviewController
    private long CurrentEmployeeId => long.TryParse(User.FindFirst("EmployeeId")?.Value, out var id) ? id : 0;
    private long Target(long requested) => IsAdmin ? requested : CurrentEmployeeId;   // an employee always gets his own

    // GET api/employee-balance?employeeId=12&dateFrom=2026-11-01&dateTo=2026-11-30
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] long employeeId, [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo)
    {
        var target = Target(employeeId);
        if (target == 0) return Forbid();
        return ToActionResult(await _service.GetAsync(target, dateFrom, dateTo));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] long employeeId, [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo)
    {
        var target = Target(employeeId);
        if (target == 0) return Forbid();

        var r = await _service.ExportAsync(target, dateFrom, dateTo);
        return r.Error is not null
            ? BadRequest(new { message = r.Error })
            : File(r.Bytes!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", r.FileName);
    }
}
