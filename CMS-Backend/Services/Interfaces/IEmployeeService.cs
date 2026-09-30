using CMS.Domain.Models;
using CMS.Api.Models.DTOs;
using CMS.Api.Models.DTOs.Responses;

namespace CMS.Api.Services.Interfaces;

public interface IEmployeeService
{
    Task<ServiceResult<List<EmployeeDto>>> GetAllAsync();
    Task<ServiceResult<EmployeeDto?>> GetByIdAsync(long id);
    Task<ServiceResult<EmployeeDto>> CreateAsync(Employee employee);
    Task<ServiceResult<bool>> UpdateAsync(long id, Employee employee);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}