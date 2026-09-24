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
public class SupplierController : ApiControllerBase
{

    private readonly ISupplierService _supplierService;

    public SupplierController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    // GET: api/supplier
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _supplierService.GetAllAsync());

    // GET: api/supplier/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _supplierService.GetByIdAsync(id));

    // POST: api/supplier
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Supplier supplier) =>
        ToActionResult(await _supplierService.CreateAsync(supplier));

    // PUT: api/supplier/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Supplier supplier) =>
        ToNoContentResult(await _supplierService.UpdateAsync(id, supplier));

    // DELETE: api/supplier/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _supplierService.DeleteAsync(id));
}
