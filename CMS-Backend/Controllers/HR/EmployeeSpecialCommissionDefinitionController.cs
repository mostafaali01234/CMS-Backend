using CMS.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS_Backend.Controllers.Base;
using CMS.Domain.DTOs;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/sp-comm-def")]
//[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
public class EmployeeSpecialCommissionDefinitionController : ApiControllerBase
{

    private readonly IEmployeeSpecialCommissionDefinitionService _commService;

    public EmployeeSpecialCommissionDefinitionController(IEmployeeSpecialCommissionDefinitionService commService)
    {
        _commService = commService;
    }

    // GET: api/sp-comm-def
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _commService.GetAllAsync());

    // GET: api/sp-comm-def/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _commService.GetByIdAsync(id));

    // POST: api/sp-comm-def
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EmployeeSpecialCommissionDefinitionDto dto) =>
        ToActionResult(await _commService.CreateAsync(dto));

    // PUT: api/sp-comm-def/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EmployeeSpecialCommissionDefinitionDto dto) =>
        ToNoContentResult(await _commService.UpdateAsync(id, dto));

    // DELETE: api/sp-comm-def/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _commService.DeleteAsync(id));
}
