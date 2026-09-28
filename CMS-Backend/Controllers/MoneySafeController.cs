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
public class MoneySafeController : ApiControllerBase
{

    private readonly IMoneySafeService _msService;

    public MoneySafeController(IMoneySafeService catService)
    {
        _msService = catService;
    }

    // GET: api/MoneySafe
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _msService.GetAllAsync());

    // GET: api/MoneySafe/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _msService.GetByIdAsync(id));

    // POST: api/MoneySafe
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MoneySafeDto ms) =>
        ToActionResult(await _msService.CreateAsync(ms));

    // PUT: api/MoneySafe/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] MoneySafeDto ms) =>
        ToNoContentResult(await _msService.UpdateAsync(id, ms));

    // DELETE: api/MoneySafe/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _msService.DeleteAsync(id));
}
