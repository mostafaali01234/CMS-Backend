using CMS.Application.DTOs;
using CMS.Application.Interfaces;
using CMS.Domain.DTOs;
using CMS_Backend.Controllers.Base;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/payroll-adjustments")]
//[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
public class EmployeePayrollAdjustmentsController : ApiControllerBase
{

    private readonly IEmployeePayrollAdjustmentService _epService;

    public EmployeePayrollAdjustmentsController(IEmployeePayrollAdjustmentService epService)
    {
        _epService = epService;
    }

    // GET: api/payroll-adjustments
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _epService.GetAllAsync());

    // GET: api/payroll-adjustments/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _epService.GetByIdAsync(id));

    // POST: api/payroll-adjustments
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EmployeePayrollAdjustmentDto dto) =>
        ToActionResult(await _epService.CreateAsync(dto));

    // PUT: api/payroll-adjustments/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EmployeePayrollAdjustmentDto dto) =>
        ToNoContentResult(await _epService.UpdateAsync(id, dto));

    // DELETE: api/payroll-adjustments/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _epService.DeleteAsync(id));
}
