using CMS.Application.DTOs;
using CMS.Application.Helpers;
using CMS.Application.Interfaces;
using CMS_Backend.Controllers.Base;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/report")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
public class ReportController : ApiControllerBase
{
    private const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IReportService _service;
    public ReportController(IReportService service) { _service = service; }

    private IActionResult ToFile(ExportResult r) =>
        r.Error is not null
            ? BadRequest(new { message = r.Error })
            : File(r.Bytes!, Xlsx, r.FileName);

    // ---- 1: GET api/report/unfinished-orders?dateFrom=&dateTo=      (dates = order creation date, both optional)
    [HttpGet("unfinished-orders")]
    public async Task<IActionResult> UnfinishedOrders([FromQuery] ReportDateFilterDto f) =>
        ToActionResult(await _service.GetUnfinishedOrdersAsync(f));

    [HttpGet("unfinished-orders/export")]
    public async Task<IActionResult> ExportUnfinishedOrders([FromQuery] ReportDateFilterDto f) =>
        ToFile(await _service.ExportUnfinishedOrdersAsync(f));

    // ---- 2: GET api/report/advance-orders?dateFrom=&dateTo=
    [HttpGet("advance-orders")]
    public async Task<IActionResult> AdvanceOrders([FromQuery] ReportDateFilterDto f) =>
        ToActionResult(await _service.GetAdvanceOrdersAsync(f));

    [HttpGet("advance-orders/export")]
    public async Task<IActionResult> ExportAdvanceOrders([FromQuery] ReportDateFilterDto f) =>
        ToFile(await _service.ExportAdvanceOrdersAsync(f));

    // ---- 3: GET api/report/unpaid-invoices?dateFrom=&dateTo=        (dates = invoice date)
    [HttpGet("unpaid-invoices")]
    public async Task<IActionResult> UnpaidInvoices([FromQuery] ReportDateFilterDto f) =>
        ToActionResult(await _service.GetUnpaidInvoicesAsync(f));

    [HttpGet("unpaid-invoices/export")]
    public async Task<IActionResult> ExportUnpaidInvoices([FromQuery] ReportDateFilterDto f) =>
        ToFile(await _service.ExportUnpaidInvoicesAsync(f));

    // ---- 4: GET api/report/monthly-sales?year=2026&half=first|last&sellerId=
    [HttpGet("monthly-sales")]
    public async Task<IActionResult> MonthlySales([FromQuery] MonthlySalesFilterDto f) =>
        ToActionResult(await _service.GetMonthlySalesAsync(f));

    [HttpGet("monthly-sales/export")]
    public async Task<IActionResult> ExportMonthlySales([FromQuery] MonthlySalesFilterDto f) =>
        ToFile(await _service.ExportMonthlySalesAsync(f));

    // ---- 5 - 10: GET api/report/matrix/{orders|invoices}/{state|seller|tech}?dateFrom=&dateTo=&kind=&sellerId=&measure=&includeCancelled=
    //   orders/state   5      invoices/state   6
    //   orders/seller  7      invoices/seller  8
    //   orders/tech    9      invoices/tech   10
    [HttpGet("matrix/{source}/{rowBy}")]
    public async Task<IActionResult> Matrix(ReportSource source, ReportRowBy rowBy, [FromQuery] CategoryMatrixFilterDto f) =>
        ToActionResult(await _service.GetCategoryMatrixAsync(source, rowBy, f));

    [HttpGet("matrix/{source}/{rowBy}/export")]
    public async Task<IActionResult> ExportMatrix(ReportSource source, ReportRowBy rowBy, [FromQuery] CategoryMatrixFilterDto f) =>
        ToFile(await _service.ExportCategoryMatrixAsync(source, rowBy, f));

    // ---- 11: GET api/report/maintenance?dateFrom=&dateTo=           (dates = creation date of the original order)
    [HttpGet("maintenance")]
    public async Task<IActionResult> Maintenance([FromQuery] ReportDateFilterDto f) =>
        ToActionResult(await _service.GetMaintenanceAsync(f));

    [HttpGet("maintenance/export")]
    public async Task<IActionResult> ExportMaintenance([FromQuery] ReportDateFilterDto f) =>
        ToFile(await _service.ExportMaintenanceAsync(f));
}
