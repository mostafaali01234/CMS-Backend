// Services/Interfaces/IEmployeePayrollAdjustmentService.cs
using CMS.Application.DTOs.Responses;
using CMS.Domain.DTOs;

namespace CMS.Application.Interfaces;

public interface IEmployeePayrollAdjustmentService
{
    Task<ServiceResult<List<EmployeePayrollAdjustmentDto>>> GetAllAsync();
    Task<ServiceResult<EmployeePayrollAdjustmentDto?>> GetByIdAsync(long id);
    Task<ServiceResult<EmployeePayrollAdjustmentDto>> CreateAsync(EmployeePayrollAdjustmentDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, EmployeePayrollAdjustmentDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}