using CMS.Domain.Models;
using CMS.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS_Backend.Controllers.Base;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
public class ExpenseCategoryController : ApiControllerBase
{

    private readonly IExpenseCategoryService _expenseCategoryService;

    public ExpenseCategoryController(IExpenseCategoryService expenseCategoryService)
    {
        _expenseCategoryService = expenseCategoryService;
    }

    // GET: api/expenseCategory
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _expenseCategoryService.GetAllAsync());

    // GET: api/expenseCategory/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _expenseCategoryService.GetByIdAsync(id));

    // POST: api/expenseCategory
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ExpenseCategory cat) =>
        ToActionResult(await _expenseCategoryService.CreateAsync(cat));

    // PUT: api/expenseCategory/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ExpenseCategory cat) =>
        ToNoContentResult(await _expenseCategoryService.UpdateAsync(id, cat));

    // DELETE: api/expenseCategory/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _expenseCategoryService.DeleteAsync(id));
}
