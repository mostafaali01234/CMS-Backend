using CMS.Application.DTOs;
using CMS.Application.Interfaces;
using CMS_Backend.Controllers.Base;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/sales-manager-groups")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
public class SalesManagerGroupController : ApiControllerBase
{
    private readonly ISalesManagerGroupService _service;
    public SalesManagerGroupController(ISalesManagerGroupService service) { _service = service; }

    [HttpGet] public async Task<IActionResult> GetAll() => ToActionResult(await _service.GetAllAsync());
    [HttpGet("{id:long}")] public async Task<IActionResult> GetById(long id) => ToActionResult(await _service.GetByIdAsync(id));
    [HttpPost] public async Task<IActionResult> Create([FromBody] SalesManagerGroupDto dto) => ToActionResult(await _service.CreateAsync(dto));
    [HttpPut("{id:long}")] public async Task<IActionResult> Update(long id, [FromBody] SalesManagerGroupDto dto) => ToNoContentResult(await _service.UpdateAsync(id, dto));
    [HttpDelete("{id:long}")] public async Task<IActionResult> Delete(long id) => ToNoContentResult(await _service.DeleteAsync(id));
}