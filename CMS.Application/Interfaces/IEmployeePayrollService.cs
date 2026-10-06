// Services/Interfaces/IEmployeePayrollService.cs
using CMS.Application.DTOs.Responses;
using CMS.Domain.DTOs;

namespace CMS.Application.Interfaces;

public interface IEmployeePayrollService
{
    Task<ServiceResult<List<EmployeePayrollDto>>> GetAllAsync();
    Task<ServiceResult<EmployeePayrollDto?>> GetByIdAsync(long id);
    Task<ServiceResult<List<EmployeePayrollDto>>> GetByEmployeeIdAsync(long employeeId);
    Task<ServiceResult<EmployeePayrollDto?>> GetByEmployeeIdMonthYearAsync(long employeeId, int month, int year);
    Task<ServiceResult<EmployeePayrollDto>> CreateAsync(EmployeePayrollDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, EmployeePayrollDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}