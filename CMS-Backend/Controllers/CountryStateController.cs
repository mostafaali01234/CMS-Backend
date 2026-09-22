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
public class CountryStateController : ApiControllerBase
{

    private readonly IStateService _stateService;

    public CountryStateController(IStateService stateService)
    {
        _stateService = stateService;
    }

    // GET: api/countryState
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _stateService.GetAllAsync());

    // GET: api/countryState/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _stateService.GetByIdAsync(id));

    // POST: api/countryState
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CountryState state) =>
        ToActionResult(await _stateService.CreateAsync(state));

    // PUT: api/countryState/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CountryState state) =>
        ToNoContentResult(await _stateService.UpdateAsync(id, state));

    // DELETE: api/countryState/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _stateService.DeleteAsync(id));
}
