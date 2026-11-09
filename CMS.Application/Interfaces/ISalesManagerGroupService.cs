// Services/Interfaces/IBalanceService.cs
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
namespace CMS.Application.Interfaces;

public interface ISalesManagerGroupService
{
    Task<ServiceResult<List<SalesManagerGroupDto>>> GetAllAsync();
    Task<ServiceResult<SalesManagerGroupDto?>> GetByIdAsync(long id);
    Task<ServiceResult<SalesManagerGroupDto>> CreateAsync(SalesManagerGroupDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, SalesManagerGroupDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}