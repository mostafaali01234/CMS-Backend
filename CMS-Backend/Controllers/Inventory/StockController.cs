// CMS.Api/Controllers/StockController.cs
using CMS.Application.DTOs;
using CMS.Application.Helpers;
using CMS.Application.Interfaces;
using CMS_Backend.Controllers.Base;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/stock")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
public class StockController : ApiControllerBase
{
    private readonly IStockService _stock;
    public StockController(IStockService stock) { _stock = stock; }

    // GET: api/stock/store?storeId=3&page=1&pageSize=20&search=&categoryId=&includeZero=false
    [HttpGet("store")]
    public async Task<IActionResult> Store([FromQuery] StockFilterDto f) => ToActionResult(await _stock.GetStoreStockAsync(f));

    // GET: api/stock/matrix
    [HttpGet("matrix")]
    public async Task<IActionResult> Matrix([FromQuery] StockFilterDto f) => ToActionResult(await _stock.GetMatrixAsync(f));

    // GET: api/stock/store-moves?storeId=3&dateFrom=2026-10-27&dateTo=2026-10-27&productId=&operationType=
    [HttpGet("store-moves")]
    public async Task<IActionResult> StoreMoves([FromQuery] MovesFilterDto f) => ToActionResult(await _stock.GetStoreMovesAsync(f));

    // GET: api/stock/product-moves
    [HttpGet("product-moves")]
    public async Task<IActionResult> ProductMoves([FromQuery] MovesFilterDto f) => ToActionResult(await _stock.GetProductMovesAsync(f));

    private IActionResult Excel(ExportResult r) =>
        r.Error is not null
            ? BadRequest(new { message = r.Error })
            : File(r.Bytes!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", r.FileName);

    // GET: api/stock/store/export?storeId=3&search=&categoryId=&includeZero=false
    [HttpGet("store/export")]
    public async Task<IActionResult> ExportStore([FromQuery] StockFilterDto f) => Excel(await _stock.ExportStoreStockAsync(f));

    [HttpGet("matrix/export")]
    public async Task<IActionResult> ExportMatrix([FromQuery] StockFilterDto f) => Excel(await _stock.ExportMatrixAsync(f));

    [HttpGet("store-moves/export")]
    public async Task<IActionResult> ExportStoreMoves([FromQuery] MovesFilterDto f) => Excel(await _stock.ExportStoreMovesAsync(f));

    [HttpGet("product-moves/export")]
    public async Task<IActionResult> ExportProductMoves([FromQuery] MovesFilterDto f) => Excel(await _stock.ExportProductMovesAsync(f));
}