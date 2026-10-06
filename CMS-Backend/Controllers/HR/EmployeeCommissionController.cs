using CMS.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS_Backend.Controllers.Base;
using CMS.Domain.DTOs;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/employee-commissions")]
//[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
public class EmployeeCommissionController : ApiControllerBase
{

    private readonly IEmployeeCommissionService _commService;

    public EmployeeCommissionController(IEmployeeCommissionService commService)
    {
        _commService = commService;
    }

    // GET: api/employee-commissions
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _commService.GetAllAsync());

    [HttpGet("employee/{employeeId:long}")]
    public async Task<IActionResult> GetByEmployeeAndMonth(long employeeId, [FromQuery] int year, [FromQuery] int month) =>
        ToActionResult(await _commService.GetByEmployeeAndMonthAsync(employeeId, year, month));

    // GET: api/employee-commissions/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _commService.GetByIdAsync(id));

    // POST: api/employee-commissions
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EmployeeCommissionDto dto) =>
        ToActionResult(await _commService.CreateAsync(dto));

    // PUT: api/employee-commissions/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EmployeeCommissionDto dto) =>
        ToNoContentResult(await _commService.UpdateAsync(id, dto));

    // DELETE: api/employee-commissions/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _commService.DeleteAsync(id));
}
