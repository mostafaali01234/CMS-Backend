// Services/Interfaces/IBalanceService.cs
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
namespace CMS.Application.Interfaces;

public interface IBalanceService
{
    Task<ServiceResult<List<PartyBalanceDto>>> GetSupplierBalancesAsync();
    Task<ServiceResult<List<PartyBalanceDto>>> GetCustomerBalancesAsync();
    Task<ServiceResult<StatementDto>> GetSupplierStatementAsync(long id, DateTime? from, DateTime? to);
    Task<ServiceResult<StatementDto>> GetCustomerStatementAsync(long id, DateTime? from, DateTime? to);
}