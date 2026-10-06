// Services/Interfaces/IEmployeeCommissionService.cs
using CMS.Application.DTOs.Responses;
using CMS.Domain.DTOs;

namespace CMS.Application.Interfaces;

public interface IEmployeeCommissionService
{
    Task<ServiceResult<List<EmployeeCommissionDto>>> GetAllAsync();
    Task<ServiceResult<CommissionPageDto>> GetByEmployeeAndMonthAsync(long employeeId, int year, int month);
    Task<ServiceResult<EmployeeCommissionDto?>> GetByIdAsync(long id);
    Task<ServiceResult<EmployeeCommissionDto>> CreateAsync(EmployeeCommissionDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, EmployeeCommissionDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}