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
public class CarController : ApiControllerBase
{

    private readonly ICarService _carService;

    public CarController(ICarService carService)
    {
        _carService = carService;
    }

    // GET: api/car
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _carService.GetAllAsync());

    // GET: api/car/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _carService.GetByIdAsync(id));

    // POST: api/car
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CarDto car) =>
        ToActionResult(await _carService.CreateAsync(car));

    // PUT: api/car/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CarDto car) =>
        ToNoContentResult(await _carService.UpdateAsync(id, car));

    // DELETE: api/car/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _carService.DeleteAsync(id));
}
