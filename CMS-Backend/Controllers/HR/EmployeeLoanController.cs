using CMS.Domain.Models;
using CMS.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS_Backend.Controllers.Base;
using CMS.Domain.DTOs;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/employee-loans")]
//[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
public class EmployeeLoanController : ApiControllerBase
{

    private readonly IEmployeeLoanService _loanService;

    public EmployeeLoanController(IEmployeeLoanService loanService)
    {
        _loanService = loanService;
    }

    // GET: api/employee-loans
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _loanService.GetAllAsync());

    // GET: api/employee-loans/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _loanService.GetByIdAsync(id));

    // POST: api/employee-loans
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EmployeeLoanDto dto) =>
        ToActionResult(await _loanService.CreateAsync(dto));

    // PUT: api/employee-loans/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EmployeeLoanDto dto) =>
        ToNoContentResult(await _loanService.UpdateAsync(id, dto));

    // DELETE: api/employee-loans/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _loanService.DeleteAsync(id));
}
