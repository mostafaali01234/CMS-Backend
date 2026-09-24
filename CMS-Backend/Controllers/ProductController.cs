using CMS_Backend.Models;
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
public class ProductController : ApiControllerBase
{

    private readonly IProductService _productService;
    private readonly IProductUnitCommissionService _commService;

    public ProductController(IProductService productService, IProductUnitCommissionService commService)
    {
        _productService = productService;
        _commService = commService;
    }

    // GET: api/product
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _productService.GetAllAsync());

    // GET: api/product/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _productService.GetByIdAsync(id));

    // POST: api/product
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductDto product) =>
        ToActionResult(await _productService.CreateAsync(product));

    // PUT: api/product/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductDto product) =>
        ToNoContentResult(await _productService.UpdateAsync(id, product));

    // DELETE: api/product/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _productService.DeleteAsync(id));


    //--------------------------------------------------------------------------------------


    // GET: api/{productID}/unit-comm
    [HttpGet("{productId:long}/unit-comm")]
    public async Task<IActionResult> GetAllCommissions(long productId) =>
        ToActionResult(await _commService.GetByProductIdAsync(productId));


    // POST: api/{productID}/unit-comm
    [HttpPost("{productId:long}/unit-comm")]
    public async Task<IActionResult> AddProductComms([FromBody] List<ProductUnitCommission> commList) =>
        ToActionResult(await _commService.CreateForProductAsync(commList));
}
