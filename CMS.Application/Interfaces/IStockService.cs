// Services/Interfaces/IStockService.cs
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;
namespace CMS.Application.Interfaces;

public interface IStockService
{
    /// <summary>null when the change is allowed, otherwise the message to show</summary>
    Task<string?> CheckChangeAsync(StockExclusion ex, IEnumerable<StockDelta> before, IEnumerable<StockDelta> after);

    Task<ServiceResult<PagedResult<StockRowDto, StockSummaryDto>>> GetStoreStockAsync(StockFilterDto f);
    Task<ServiceResult<PagedResult<StockMatrixRowDto, StockMatrixHeaderDto>>> GetMatrixAsync(StockFilterDto f);
    Task<ServiceResult<PagedResult<StockMoveDto, StockMovesSummaryDto>>> GetStoreMovesAsync(MovesFilterDto f);
    Task<ServiceResult<PagedResult<StockMoveDto, StockMovesSummaryDto>>> GetProductMovesAsync(MovesFilterDto f);
}