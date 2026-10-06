using CMS.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS_Backend.Controllers.Base;
using CMS.Domain.DTOs;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/emp-sp-comm")]
//[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
public class EmployeeSpecialCommissionController : ApiControllerBase
{

    private readonly IEmployeeSpecialCommissionService _commService;

    public EmployeeSpecialCommissionController(IEmployeeSpecialCommissionService commService)
    {
        _commService = commService;
    }

    // GET: api/emp-sp-comm
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _commService.GetAllAsync());

    // GET: api/emp-sp-comm/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _commService.GetByIdAsync(id));

    // POST: api/emp-sp-comm
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EmployeeSpecialCommissionDto dto) =>
        ToActionResult(await _commService.CreateAsync(dto));

    // PUT: api/emp-sp-comm/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EmployeeSpecialCommissionDto dto) =>
        ToNoContentResult(await _commService.UpdateAsync(id, dto));

    // DELETE: api/emp-sp-comm/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _commService.DeleteAsync(id));
}
