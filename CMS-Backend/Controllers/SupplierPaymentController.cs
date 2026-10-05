using CMS.Domain.Models;
using CMS.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS_Backend.Controllers.Base;
using CMS.Domain.DTOs;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/supplier-payment")]
//[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
public class SupplierPaymentController : ApiControllerBase
{

    private readonly ISupplierPaymentService _paymentService;

    public SupplierPaymentController(ISupplierPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    // GET: api/supplier-payment
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _paymentService.GetAllAsync());

    // GET: api/supplier-payment/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _paymentService.GetByIdAsync(id));

    // POST: api/supplier-payment
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SupplierPaymentDto dto) =>
        ToActionResult(await _paymentService.CreateAsync(dto));

    // PUT: api/supplier/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SupplierPaymentDto dto) =>
        ToNoContentResult(await _paymentService.UpdateAsync(id, dto));

    // DELETE: api/supplier-payment/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _paymentService.DeleteAsync(id));
}
