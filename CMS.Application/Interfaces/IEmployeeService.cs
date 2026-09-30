using CMS.Domain.Models;
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;

namespace CMS.Application.Interfaces;

public interface IEmployeeService
{
    Task<ServiceResult<List<EmployeeDto>>> GetAllAsync();
    Task<ServiceResult<EmployeeDto?>> GetByIdAsync(long id);
    Task<ServiceResult<EmployeeDto>> CreateAsync(Employee employee);
    Task<ServiceResult<bool>> UpdateAsync(long id, Employee employee);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}