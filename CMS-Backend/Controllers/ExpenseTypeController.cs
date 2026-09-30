using CMS.Domain.Models;
using CMS.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
public class ExpenseTypeController : ApiControllerBase
{

    private readonly IExpenseTypeService _expenseTypeService;

    public ExpenseTypeController(IExpenseTypeService expenseTypeService)
    {
        _expenseTypeService = expenseTypeService;
    }

    // GET: api/expenseType
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _expenseTypeService.GetAllAsync());

    // GET: api/expenseType/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _expenseTypeService.GetByIdAsync(id));

    // POST: api/expenseType
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ExpenseType type) =>
        ToActionResult(await _expenseTypeService.CreateAsync(type));

    // PUT: api/expenseType/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ExpenseType type) =>
        ToNoContentResult(await _expenseTypeService.UpdateAsync(id, type));

    // DELETE: api/expenseType/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _expenseTypeService.DeleteAsync(id));
}
