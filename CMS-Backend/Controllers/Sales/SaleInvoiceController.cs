using CMS.Application.Interfaces;
using CMS.Domain.DTOs;
using CMS_Backend.Controllers.Base;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/sale-invoice")]
//[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
public class SaleInvoiceController : ApiControllerBase
{

    private readonly ISaleInvoiceService _invoiceService;

    public SaleInvoiceController(ISaleInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    // GET: api/sale-invoice
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _invoiceService.GetAllAsync());

    // GET: api/sale-invoice/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _invoiceService.GetByIdAsync(id));

    // POST: api/sale-invoice
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaleInvoiceDto dto) =>
        ToActionResult(await _invoiceService.CreateAsync(dto));

    // PUT: api/sale-invoice/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SaleInvoiceDto dto) =>
        ToNoContentResult(await _invoiceService.UpdateAsync(id, dto));

    // DELETE: api/sale-invoice/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _invoiceService.DeleteAsync(id));
}
