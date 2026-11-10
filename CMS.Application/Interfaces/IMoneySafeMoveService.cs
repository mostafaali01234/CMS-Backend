using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;

namespace CMS.Application.Interfaces;

public interface IMoneySafeMoveService
{
    // admins: every safe. everyone else: only the safes they manage
    Task<ServiceResult<List<MoneySafeDto>>> GetAccessibleSafesAsync(string userId, bool isAdmin);

    Task<ServiceResult<PagedResult<MoneySafeMoveDto, MoneySafeMovesSummaryDto>>> GetMovesAsync(
        MoneySafeMoveFilterDto filter, string userId, bool isAdmin);

    // current balance (opening + all movements) per safe; null ids => every safe
    Task<Dictionary<long, (decimal Balance, decimal BalanceCurrency)>> GetBalancesAsync(IReadOnlyCollection<long>? ids = null);

    Task<ExportResult> ExportAsync(MoneySafeMoveFilterDto f, string userId, bool isAdmin);
}
