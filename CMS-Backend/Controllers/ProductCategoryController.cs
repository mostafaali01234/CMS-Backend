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
public class ProductCategoryController : ApiControllerBase
{

    private readonly IProductCategoryService _productCategoryService;

    public ProductCategoryController(IProductCategoryService productCategoryService)
    {
        _productCategoryService = productCategoryService;
    }

    // GET: api/productCategory
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _productCategoryService.GetAllAsync());

    // GET: api/productCategory/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _productCategoryService.GetByIdAsync(id));

    // POST: api/productCategory
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductCategory cat) =>
        ToActionResult(await _productCategoryService.CreateAsync(cat));

    // PUT: api/productCategory/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductCategory cat) =>
        ToNoContentResult(await _productCategoryService.UpdateAsync(id, cat));

    // DELETE: api/productCategory/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _productCategoryService.DeleteAsync(id));
}
