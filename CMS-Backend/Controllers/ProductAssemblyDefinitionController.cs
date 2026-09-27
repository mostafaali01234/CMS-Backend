using CMS_Backend.Models.DTOs;
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
public class ProductAssemblyDefinitionController : ApiControllerBase
{

    private readonly IProductAssemblyDefinitionService _assemblyDefService;

    public ProductAssemblyDefinitionController(IProductAssemblyDefinitionService assemblyDefService)
    {
        _assemblyDefService = assemblyDefService;
    }

    // GET: api/ProductAssemblyDefinition
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _assemblyDefService.GetAllAsync());

    // GET: api/ProductAssemblyDefinition/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _assemblyDefService.GetByIdAsync(id));

    // POST: api/ProductAssemblyDefinition
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductAssemblyDefinitionDto op) =>
        ToActionResult(await _assemblyDefService.CreateAsync(op));

    // PUT: api/ProductAssemblyDefinition/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductAssemblyDefinitionDto op) =>
        ToNoContentResult(await _assemblyDefService.UpdateAsync(id, op));

    // DELETE: api/ProductAssemblyDefinition/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _assemblyDefService.DeleteAsync(id));
}
