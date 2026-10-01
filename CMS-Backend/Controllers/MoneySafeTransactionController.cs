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
public class MoneySafeTransactionController : ApiControllerBase
{

    private readonly IMoneySafeTransactionService _msTransactionService;

    public MoneySafeTransactionController(IMoneySafeTransactionService catService)
    {
        _msTransactionService = catService;
    }

    // GET: api/MoneySafeTransaction
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _msTransactionService.GetAllAsync());

    // GET: api/MoneySafeTransaction/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _msTransactionService.GetByIdAsync(id));

    // POST: api/MoneySafeTransaction
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MoneySafeTransactionDto mst) =>
        ToActionResult(await _msTransactionService.CreateAsync(mst));

    // PUT: api/MoneySafeTransaction/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] MoneySafeTransactionDto ms) =>
        ToNoContentResult(await _msTransactionService.UpdateAsync(id, ms));

    // DELETE: api/MoneySafeTransaction/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _msTransactionService.DeleteAsync(id));
}
