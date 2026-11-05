using CMS.Application.DTOs;
using CMS.Application.Interfaces;
using CMS.Domain.Models;
using CMS_Backend.Controllers.Base;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class EmployeeController : ApiControllerBase
{

    private readonly IEmployeeService _employeeService;

    public EmployeeController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    // GET: api/employee
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _employeeService.GetAllAsync());

    // GET: api/employee/paged?page=1&pageSize=20&search=&departmentId=&jobId=&active=
    [HttpGet("paged")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetPaged([FromQuery] EmployeeFilterDto filter) =>
        ToActionResult(await _employeeService.GetPagedAsync(filter));

    // GET: api/employee/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _employeeService.GetByIdAsync(id));

    // POST: api/employee
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] EmployeeSaveDto dto) =>
    ToActionResult(await _employeeService.CreateAsync(dto.ToEntity()));

    // PUT: api/employee/5
    [HttpPut("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] EmployeeSaveDto dto) =>
        ToNoContentResult(await _employeeService.UpdateAsync(id, dto.ToEntity()));

    // DELETE: api/employee/5
    [HttpDelete("{id:int}")]

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _employeeService.DeleteAsync(id));
}
