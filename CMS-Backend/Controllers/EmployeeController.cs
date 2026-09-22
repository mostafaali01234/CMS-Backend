using CMS_Backend.Models;
using CMS_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
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

    // GET: api/employee/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _employeeService.GetByIdAsync(id));

    // POST: api/employee
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Employee emp) =>
        ToActionResult(await _employeeService.CreateAsync(emp));

    // PUT: api/employee/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Employee emp) =>
        ToNoContentResult(await _employeeService.UpdateAsync(id, emp));

    // DELETE: api/employee/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _employeeService.DeleteAsync(id));
}
