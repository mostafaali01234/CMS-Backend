using CMS.Application.DTOs;
using CMS.Application.Interfaces;
using CMS.Domain.DTOs;
using CMS_Backend.Controllers.Base;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/buy-invoice")]
//[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
public class BuyInvoiceController : ApiControllerBase
{

    private readonly IBuyInvoiceService _invoiceService;

    public BuyInvoiceController(IBuyInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    // GET: api/buy-invoice
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _invoiceService.GetAllAsync());

    // GET: api/buy-invoice/paged
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] BuyInvoiceFilterDto filter) =>
    ToActionResult(await _invoiceService.GetPagedAsync(filter));

    // GET: api/buy-invoice/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _invoiceService.GetByIdAsync(id));

    // POST: api/buy-invoice
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BuyInvoiceDto dto) =>
        ToActionResult(await _invoiceService.CreateAsync(dto));

    // PUT: api/buy-invoice/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] BuyInvoiceDto dto) =>
        ToNoContentResult(await _invoiceService.UpdateAsync(id, dto, User.IsInRole("SuperAdmin")));

    // DELETE: api/buy-invoice/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _invoiceService.DeleteAsync(id, User.IsInRole("SuperAdmin")));

    // BuyInvoiceController
    // GET: api/buy-invoice/unpaid/5
    [HttpGet("unpaid/{supplierId:long}")]
    public async Task<IActionResult> GetUnpaid(long supplierId) =>
        ToActionResult(await _invoiceService.GetUnpaidBySupplierAsync(supplierId));

    // GET: api/buy-invoice/by-supplier/5
    [HttpGet("by-supplier/{supplierId:long}")]
    public async Task<IActionResult> GetBuyBySupplier(long supplierId) =>
        ToActionResult(await _invoiceService.GetBuyBySupplierAsync(supplierId));

    // GET: api/buy-invoice/5/returnable?excludeReturnId=9
    [HttpGet("{id:long}/returnable")]
    public async Task<IActionResult> GetReturnable(long id, [FromQuery] long? excludeReturnId) =>
        ToActionResult(await _invoiceService.GetReturnableAsync(id, excludeReturnId));

    // POST: api/buy-invoice/return
    [HttpPost("return")]
    public async Task<IActionResult> CreateReturn([FromBody] BuyReturnRequestDto request) =>
        ToActionResult(await _invoiceService.CreateReturnAsync(request));

    // PUT: api/buy-invoice/return/5
    [HttpPut("return/{id:long}")]
    public async Task<IActionResult> UpdateReturn(long id, [FromBody] BuyReturnRequestDto request) =>
        ToNoContentResult(await _invoiceService.UpdateReturnAsync(id, request));

    // GET: api/buy-invoice/5/export 
    [HttpGet("{id:long}/export")]
    public async Task<IActionResult> Export(long id)
    {
        var file = await _invoiceService.ExportAsync(id);
        if (file is null) return NotFound(new { message = "Invoice not found" });

        return File(file.Value.Bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            file.Value.FileName);
    }
}
