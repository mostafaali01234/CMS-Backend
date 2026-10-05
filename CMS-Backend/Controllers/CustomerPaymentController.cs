using CMS.Domain.Models;
using CMS.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS_Backend.Controllers.Base;
using CMS.Domain.DTOs;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/customer-payment")]
//[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
public class CustomerPaymentController : ApiControllerBase
{

    private readonly ICustomerPaymentService _paymentService;

    public CustomerPaymentController(ICustomerPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    // GET: api/customer-payment
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _paymentService.GetAllAsync());

    // GET: api/customer-payment/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _paymentService.GetByIdAsync(id));

    // POST: api/customer-payment
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CustomerPaymentDto dto) =>
        ToActionResult(await _paymentService.CreateAsync(dto));

    // PUT: api/customer/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CustomerPaymentDto dto) =>
        ToNoContentResult(await _paymentService.UpdateAsync(id, dto));

    // DELETE: api/customer-payment/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _paymentService.DeleteAsync(id));
}
