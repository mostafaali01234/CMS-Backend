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
public class OrderLineController : ApiControllerBase
{

    private readonly IOrderLineService _orderLineService;

    public OrderLineController(IOrderLineService orderLineService)
    {
        _orderLineService = orderLineService;
    }

    // GET: api/OrderLine
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _orderLineService.GetAllAsync());

    // GET: api/OrderLine/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _orderLineService.GetByIdAsync(id));

    // POST: api/OrderLine
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OrderLine line) =>
        ToActionResult(await _orderLineService.CreateAsync(line));

    // POST: api/OrderLine/5/cities
    [HttpPost("{id:long}/cities")]
    public async Task<IActionResult> AddCities(long id, [FromBody] List<long> cityIds) =>
        ToNoContentResult(await _orderLineService.AddCitiesAsync(id, cityIds));

    // POST: api/OrderLine/5/categories
    [HttpPost("{id:long}/categories")]
    public async Task<IActionResult> AddCategories(long id, [FromBody] List<long> categoryIds) =>
        ToNoContentResult(await _orderLineService.AddCategoriesAsync(id, categoryIds));

    // PUT: api/OrderLine/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] OrderLine line) =>
        ToNoContentResult(await _orderLineService.UpdateAsync(id, line));

    // DELETE: api/OrderLine/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _orderLineService.DeleteAsync(id));
}
