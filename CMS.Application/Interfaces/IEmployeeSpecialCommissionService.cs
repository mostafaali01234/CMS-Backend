// Services/Interfaces/IEmployeeCommissionService.cs
using CMS.Application.DTOs.Responses;
using CMS.Domain.DTOs;

namespace CMS.Application.Interfaces;

public interface IEmployeeSpecialCommissionService
{
    Task<ServiceResult<List<EmployeeSpecialCommissionDto>>> GetAllAsync();
    Task<ServiceResult<EmployeeSpecialCommissionDto?>> GetByIdAsync(long id);
    Task<ServiceResult<List<EmployeeSpecialCommissionDto>>> GetByEmployeeIdAsync(long employeeId, int year, int month);
    Task<ServiceResult<EmployeeSpecialCommissionDto>> CreateAsync(EmployeeSpecialCommissionDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, EmployeeSpecialCommissionDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}