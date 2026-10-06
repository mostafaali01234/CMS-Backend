using CMS.Application.DTOs;
using CMS.Application.Interfaces;
using CMS_Backend.Controllers.Base;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
public class ProductAssemblyOperationController : ApiControllerBase
{

    private readonly IProductAssemblyOperationService _assemblyOpService;

    public ProductAssemblyOperationController(IProductAssemblyOperationService assemblyOpService)
    {
        _assemblyOpService = assemblyOpService;
    }

    // GET: api/ProductAssemblyOperation
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _assemblyOpService.GetAllAsync());

    // GET: api/ProductAssemblyOperation/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _assemblyOpService.GetByIdAsync(id));

    // POST: api/ProductAssemblyOperation
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductAssemblyOperationDto op) =>
        ToActionResult(await _assemblyOpService.CreateAsync(op));

    // PUT: api/ProductAssemblyOperation/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductAssemblyOperationDto op) =>
        ToNoContentResult(await _assemblyOpService.UpdateAsync(id, op));

    // DELETE: api/ProductAssemblyOperation/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _assemblyOpService.DeleteAsync(id));
}
