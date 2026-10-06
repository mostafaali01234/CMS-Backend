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
public class ExpenseController : ApiControllerBase
{

    private readonly IExpenseService _expenseService;

    public ExpenseController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    // GET: api/expense
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _expenseService.GetAllAsync());

    // GET: api/expense/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _expenseService.GetByIdAsync(id));

    // POST: api/expense
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ExpenseDto dto) =>
        ToActionResult(await _expenseService.CreateAsync(dto));

    // PUT: api/expense/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ExpenseDto dto) =>
        ToNoContentResult(await _expenseService.UpdateAsync(id, dto));

    // DELETE: api/expense/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _expenseService.DeleteAsync(id));
}
