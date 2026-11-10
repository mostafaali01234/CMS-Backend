using CMS.Application.DTOs;
using CMS.Application.Interfaces;
using CMS.Domain.DTOs;
using CMS_Backend.Controllers.Base;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/employee-payrolls")]
//[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
//[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
public class EmployeePayrollController : ApiControllerBase
{

    private readonly IEmployeePayrollService _epService;
    private bool IsAdmin => User.IsInRole("SuperAdmin") || User.IsInRole("Admin");

    // ASSUMPTION: the token has an "EmployeeId" claim. Change this to wherever your login stores it.
    private long CurrentEmployeeId => long.TryParse(User.FindFirst("EmployeeId")?.Value, out var id) ? id : 0;

    private long Target(long requested) => IsAdmin ? requested : CurrentEmployeeId;


    public EmployeePayrollController(IEmployeePayrollService epService)
    {
        _epService = epService;
    }

    // GET: api/employee-payrolls
    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _epService.GetAllAsync());

    // GET: api/employee-payrolls/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _epService.GetByIdAsync(id));

    // GET api/employee-payrolls/employee/12
    [HttpGet("employee/{employeeId:long}")]
    public async Task<IActionResult> GetByEmployeeId(long employeeId) =>
        ToActionResult(await _epService.GetByEmployeeIdAsync(employeeId));

    // GET api/employee-payrolls/employee/12/period?month=10&year=2026
    [HttpGet("employee/{employeeId:long}/period")]
    public async Task<IActionResult> GetByEmployeeIdMonthYear(
        long employeeId, [FromQuery] int month, [FromQuery] int year) => 
        ToActionResult(await _epService.GetByEmployeeIdMonthYearAsync(employeeId, month, year));

    // POST: api/employee-payrolls
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] EmployeePayrollDto dto) =>
        ToActionResult(await _epService.CreateAsync(dto));

    // PUT: api/employee-payrolls/5
    [HttpPut("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] EmployeePayrollDto dto) =>
        ToNoContentResult(await _epService.UpdateAsync(id, dto));

    // DELETE: api/employee-payrolls/5
    [HttpDelete("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _epService.DeleteAsync(id));

    [HttpPost("recalculate-commissions")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> RecalculateCommissions([FromBody] RecalcRequestDto r) =>
    ToActionResult(await _epService.RecalculateCommissionsAsync(r.EmployeeId, r.Month, r.Year));

    // GET api/employee-payrolls/preview?employeeId=12&month=10&year=2026
    [HttpGet("preview")]
    public async Task<IActionResult> Preview([FromQuery] long employeeId, [FromQuery] int month, [FromQuery] int year)
    //=> ToActionResult(await _epService.PreviewAsync(employeeId, month, year));
    {
        var target = Target(employeeId);
        if (target == 0) return Forbid();
        return ToActionResult(await _epService.PreviewAsync(target, month, year));
    }

    // GET api/employee-payrolls/paged?page=1&pageSize=20&employeeId=&month=&year=&status=Revised
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] PayrollFilterDto f) => ToActionResult(await _epService.GetPagedAsync(f));

    [HttpPost("review")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Review([FromBody] PayrollReviewRequestDto r) => ToActionResult(await _epService.ReviewAsync(r));

    [HttpPost("{id:long}/withdraw")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Withdraw(long id, [FromBody] PayrollWithdrawRequestDto r) => ToNoContentResult(await _epService.WithdrawAsync(id, r));

    [HttpPut("commissions/{commissionId:long}/extra")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> SetExtra(long commissionId, [FromBody] ExtraRequest r) =>
        ToNoContentResult(await _epService.SetCommissionExtraAsync(commissionId, r.ExtraAmount));

    [HttpPost("adjustments")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddAdjustment([FromBody] EmployeeAdjustmentDto dto) => ToActionResult(await _epService.AddAdjustmentAsync(dto));

    [HttpDelete("adjustments/{id:long}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteAdjustment(long id) => ToNoContentResult(await _epService.DeleteAdjustmentAsync(id));

    public class ExtraRequest { [JsonPropertyName("extra_amount")] public decimal ExtraAmount { get; set; } }

    // GET api/payroll-review/export?employeeId=12&month=10&year=2026
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] long employeeId, [FromQuery] int month, [FromQuery] int year)
    {
        var target = Target(employeeId);
        if (target == 0) return Forbid();

        var r = await _epService.ExportReviewAsync(target, month, year);
        return r.Error is not null
            ? BadRequest(new { message = r.Error })
            : File(r.Bytes!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", r.FileName);
    }
}
