using CMS.Api.Models.DTOs;
using CMS.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
public class StoreTransactionController : ApiControllerBase
{

    private readonly IStoreTransactionService _storeTransactionService;

    public StoreTransactionController(IStoreTransactionService storeTransactionService)
    {
        _storeTransactionService = storeTransactionService;
    }

    // GET: api/storeTransaction
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _storeTransactionService.GetAllAsync());

    // GET: api/storeTransaction/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _storeTransactionService.GetByIdAsync(id));

    // POST: api/storeTransaction
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] StoreTransactionDto transaction) =>
        ToActionResult(await _storeTransactionService.CreateAsync(transaction));

    // PUT: api/storeTransaction/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] StoreTransactionDto transaction) =>
        ToNoContentResult(await _storeTransactionService.UpdateAsync(id, transaction));

    // DELETE: api/storeTransaction/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _storeTransactionService.DeleteAsync(id));
}
