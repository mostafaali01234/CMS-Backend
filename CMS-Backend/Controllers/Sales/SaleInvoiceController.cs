using CMS.Application.DTOs;
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

    // GET: api/sale-invoice/paged
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] SaleInvoiceFilterDto filter) =>
    ToActionResult(await _invoiceService.GetPagedAsync(filter));

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
        ToNoContentResult(await _invoiceService.UpdateAsync(id, dto, User.IsInRole("SuperAdmin")));

    // DELETE: api/sale-invoice/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _invoiceService.DeleteAsync(id, User.IsInRole("SuperAdmin")));

    // GET: api/sale-invoice/unpaid/5          (customer)
    [HttpGet("unpaid/{customerId:long}")]
    public async Task<IActionResult> GetUnpaid(long customerId) =>
        ToActionResult(await _invoiceService.GetUnpaidByCustomerAsync(customerId));

    // GET: api/sale-invoice/by-order/5        (unpaid invoices of an order)
    [HttpGet("by-order/{orderId:long}")]
    public async Task<IActionResult> GetByOrder(long orderId) =>
        ToActionResult(await _invoiceService.GetUnpaidByOrderAsync(orderId));

    // GET: api/sale-invoice/by-customer/5     (for the return picker)
    [HttpGet("by-customer/{customerId:long}")]
    public async Task<IActionResult> GetByCustomer(long customerId) =>
        ToActionResult(await _invoiceService.GetSaleByCustomerAsync(customerId));

    // GET: api/sale-invoice/from-order/5
    [HttpGet("from-order/{orderId:long}")]
    public async Task<IActionResult> GetFromOrder(long orderId) =>
        ToActionResult(await _invoiceService.GetDraftFromOrderAsync(orderId));

    // GET: api/sale-invoice/5/returnable?excludeReturnId=9
    [HttpGet("{id:long}/returnable")]
    public async Task<IActionResult> GetReturnable(long id, [FromQuery] long? excludeReturnId) =>
        ToActionResult(await _invoiceService.GetReturnableAsync(id, excludeReturnId));

    [HttpPost("return")]
    public async Task<IActionResult> CreateReturn([FromBody] SaleReturnRequestDto request) =>
        ToActionResult(await _invoiceService.CreateReturnAsync(request));

    [HttpPut("return/{id:long}")]
    public async Task<IActionResult> UpdateReturn(long id, [FromBody] SaleReturnRequestDto request) =>
        ToNoContentResult(await _invoiceService.UpdateReturnAsync(id, request, User.IsInRole("SuperAdmin")));

    // GET: api/sale-invoice/5/export
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
