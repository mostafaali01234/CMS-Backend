using CMS.Application.DTOs;
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
public class ShiftController : ApiControllerBase
{

    private readonly IShiftService _shiftService;

    public ShiftController(IShiftService shiftService)
    {
        _shiftService = shiftService;
    }

    // GET: api/shift
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _shiftService.GetAllAsync());

    // GET: api/shift/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _shiftService.GetByIdAsync(id));

    // GET: api/shift/day?date=2026-11-03
    [HttpGet("day")]
    public async Task<IActionResult> GetAllByDate([FromQuery] DateTime date) =>
        ToActionResult(await _shiftService.GetAllByDateAsync(date));

    // GET: api/shift/store/5?date=2026-11-03
    [HttpGet("store/{storeId:long}")]
    public async Task<IActionResult> GetByStoreIdAndDate(long storeId, [FromQuery] DateTime date) =>
        ToActionResult(await _shiftService.GetByStoreIdAndDateAsync(storeId, date));

    // GET: api/shift/board?storeId=5&date=2026-11-03   (the one-shift page: shift or null, plus the store's items)
    [HttpGet("board")]
    public async Task<IActionResult> Board([FromQuery] long storeId, [FromQuery] DateTime date) =>
        ToActionResult(await _shiftService.GetBoardAsync(storeId, date));

    // POST: api/shift
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ShiftDto dto) =>
        ToActionResult(await _shiftService.CreateAsync(dto));

    // PUT: api/shift/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ShiftDto dto) =>
        ToNoContentResult(await _shiftService.UpdateAsync(id, dto));

    // DELETE: api/shift/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _shiftService.DeleteAsync(id));

    // POST: api/shift/5/expense | /loan | /invoice
    [HttpPost("{id:long}/expense")]
    public async Task<IActionResult> AddExpense(long id, [FromBody] ExpenseDto dto) => ToActionResult(await _shiftService.AddExpenseAsync(id, dto));

    [HttpPost("{id:long}/loan")]
    public async Task<IActionResult> AddLoan(long id, [FromBody] EmployeeLoanDto dto) => ToActionResult(await _shiftService.AddLoanAsync(id, dto));

    [HttpPost("{id:long}/invoice")]
    public async Task<IActionResult> AddInvoice(long id, [FromBody] SaleInvoiceDto dto) => ToActionResult(await _shiftService.AddInvoiceAsync(id, dto));

    // POST: api/shift/5/custody | /settlement      DELETE: api/shift/5/transaction/9
    [HttpPost("{id:long}/custody")]
    public async Task<IActionResult> AddCustody(long id, [FromBody] ShiftTransferRequestDto dto) =>
        ToActionResult(await _shiftService.AddCustodyAsync(id, dto));

    [HttpPost("{id:long}/settlement")]
    public async Task<IActionResult> AddSettlement(long id, [FromBody] ShiftTransferRequestDto dto) =>
        ToActionResult(await _shiftService.AddSettlementAsync(id, dto));

    [HttpDelete("{id:long}/transaction/{transactionId:long}")]
    public async Task<IActionResult> DeleteTransaction(long id, long transactionId) =>
        ToNoContentResult(await _shiftService.DeleteTransactionAsync(id, transactionId));
}
