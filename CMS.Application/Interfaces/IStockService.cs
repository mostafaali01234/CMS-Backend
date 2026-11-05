// Services/Interfaces/IStockService.cs
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;
using CMS.Domain.DTOs;
namespace CMS.Application.Interfaces;

public interface IStockService
{
    /// <summary>null when the change is allowed, otherwise the message to show</summary>
    Task<string?> CheckChangeAsync(StockExclusion ex, IEnumerable<StockDelta> before, IEnumerable<StockDelta> after);

    Task<ServiceResult<PagedResult<StockRowDto, StockSummaryDto>>> GetStoreStockAsync(StockFilterDto f);
    Task<ServiceResult<PagedResult<StockMatrixRowDto, StockMatrixHeaderDto>>> GetMatrixAsync(StockFilterDto f);
    Task<ServiceResult<PagedResult<StockMoveDto, StockMovesSummaryDto>>> GetStoreMovesAsync(MovesFilterDto f);
    Task<ServiceResult<PagedResult<StockMoveDto, StockMovesSummaryDto>>> GetProductMovesAsync(MovesFilterDto f);
    Task<List<ShiftItemRowDto>> GetShiftItemsAsync(long storeId, DateTime date, IReadOnlyCollection<long> invoiceIds);
    Task<T> RunExclusiveAsync<T>(Func<Task<T>> work);
    Task<ExportResult> ExportStoreStockAsync(StockFilterDto f);
    Task<ExportResult> ExportMatrixAsync(StockFilterDto f);
    Task<ExportResult> ExportStoreMovesAsync(MovesFilterDto f);
    Task<ExportResult> ExportProductMovesAsync(MovesFilterDto f);
}