using CMS.Domain.Models;
using CMS.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS_Backend.Controllers.Base;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ProductUnitController : ApiControllerBase
{

    private readonly IProductUnitService _productUnitService;

    public ProductUnitController(IProductUnitService productUnitService)
    {
        _productUnitService = productUnitService;
    }

    // GET: api/productUnit
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _productUnitService.GetAllAsync());

    // GET: api/productUnit/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _productUnitService.GetByIdAsync(id));

    // POST: api/productUnit
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] ProductUnit unit) =>
        ToActionResult(await _productUnitService.CreateAsync(unit));

    // PUT: api/productUnit/5
    [HttpPut("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductUnit unit) =>
        ToNoContentResult(await _productUnitService.UpdateAsync(id, unit));

    // DELETE: api/productUnit/5
    [HttpDelete("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _productUnitService.DeleteAsync(id));
}
