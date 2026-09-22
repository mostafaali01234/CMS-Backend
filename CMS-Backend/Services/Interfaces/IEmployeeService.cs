using CMS_Backend.Models;
using CMS_Backend.Models.DTOs;
using CMS_Backend.Models.DTOs.Responses;

namespace CMS_Backend.Services.Interfaces;

public interface IEmployeeService
{
    Task<ServiceResult<List<EmployeeDto>>> GetAllAsync();
    Task<ServiceResult<EmployeeDto?>> GetByIdAsync(long id);
    Task<ServiceResult<EmployeeDto>> CreateAsync(Employee employee);
    Task<ServiceResult<bool>> UpdateAsync(long id, Employee employee);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}