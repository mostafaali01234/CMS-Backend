using CMS.Application.DTOs;
using CMS.Application.Interfaces;
using CMS.Domain.DTOs;
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
public class ShiftController : ApiControllerBase
{

    private readonly IShiftService _shiftService;

    public ShiftController(IShiftService shiftService)
    {
        _shiftService = shiftService;
    }

    // GET: api/shift
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _shiftService.GetAllAsync());

    // GET: api/shift/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _shiftService.GetByIdAsync(id));

    // GET: api/shift/day?date=2023-06-01
    [HttpGet("/day")]
    public async Task<IActionResult> GetAllByDate(DateTime date) =>
        ToActionResult(await _shiftService.GetAllByDateAsync(date));
    

    // GET: api/shift/store/5?date=2023-06-01
    [HttpGet("/store/{id:int}")]
    public async Task<IActionResult> GetByStoreIdAndDate(int storeId, DateTime date) =>
        ToActionResult(await _shiftService.GetByStoreIdAndDateAsync(storeId, date));

    // POST: api/shift
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ShiftDto dto) =>
        ToActionResult(await _shiftService.CreateAsync(dto));

    // PUT: api/shift/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ShiftDto dto) =>
        ToNoContentResult(await _shiftService.UpdateAsync(id, dto));

    // DELETE: api/shift/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _shiftService.DeleteAsync(id));
}
