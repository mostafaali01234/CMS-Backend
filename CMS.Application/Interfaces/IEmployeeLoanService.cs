// Services/Interfaces/IEmployeeLoanService.cs
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Domain.DTOs;

namespace CMS.Application.Interfaces;

public interface IEmployeeLoanService
{
    Task<ServiceResult<List<EmployeeLoanDto>>> GetAllAsync();
    Task<ServiceResult<PagedResult<EmployeeLoanListItemDto>>> GetPagedAsync(EmployeeLoanFilterDto filter);
    Task<ServiceResult<EmployeeLoanDto?>> GetByIdAsync(long id);
    Task<ServiceResult<EmployeeLoanDto>> CreateAsync(EmployeeLoanDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, EmployeeLoanDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}