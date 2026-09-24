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
public class CustomerController : ApiControllerBase
{

    private readonly ICustomerService _customerService;

    public CustomerController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    // GET: api/customer
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _customerService.GetAllAsync());

    // GET: api/customer/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _customerService.GetByIdAsync(id));

    // POST: api/customer
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CustomerDto customer) =>
        ToActionResult(await _customerService.CreateAsync(customer));

    // PUT: api/customer/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CustomerDto customer) =>
        ToNoContentResult(await _customerService.UpdateAsync(id, customer));

    // DELETE: api/customer/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _customerService.DeleteAsync(id));
}
