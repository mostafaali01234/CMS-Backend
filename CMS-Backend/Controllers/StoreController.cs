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
public class StoreController : ApiControllerBase
{

    private readonly IStoreService _storeService;

    public StoreController(IStoreService storeService)
    {
        _storeService = storeService;
    }

    // GET: api/store
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _storeService.GetAllAsync());

    // GET: api/store/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _storeService.GetByIdAsync(id));

    // POST: api/store
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] StoreDto store) =>
        ToActionResult(await _storeService.CreateAsync(store));

    // PUT: api/store/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] StoreDto store) =>
        ToNoContentResult(await _storeService.UpdateAsync(id, store));

    // DELETE: api/store/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _storeService.DeleteAsync(id));
}
