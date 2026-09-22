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
public class CityController : ApiControllerBase
{

    private readonly ICityService _cityService;

    public CityController(ICityService cityService)
    {
        _cityService = cityService;
    }

    // GET: api/city
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _cityService.GetAllAsync());

    // GET: api/city/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _cityService.GetByIdAsync(id));

    // POST: api/city
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] City city) =>
        ToActionResult(await _cityService.CreateAsync(city));

    // PUT: api/city/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] City city) =>
        ToNoContentResult(await _cityService.UpdateAsync(id, city));

    // DELETE: api/city/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _cityService.DeleteAsync(id));
}
