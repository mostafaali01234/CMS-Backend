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
public class DepartmentController : ApiControllerBase
{

    private readonly IDepartmentService _departmentService;

    public DepartmentController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    // GET: api/departments
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _departmentService.GetAllAsync());

    // GET: api/departments/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _departmentService.GetByIdAsync(id));

    // POST: api/departments
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Department department) =>
        ToActionResult(await _departmentService.CreateAsync(department));

    // PUT: api/departments/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Department department) =>
        ToNoContentResult(await _departmentService.UpdateAsync(id, department));

    // DELETE: api/departments/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _departmentService.DeleteAsync(id));
}
