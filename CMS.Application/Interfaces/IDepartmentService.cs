using CMS.Domain.Models;
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;

namespace CMS.Application.Interfaces;

public interface IDepartmentService
{
    Task<ServiceResult<List<DepartmentDto>>> GetAllAsync();
    Task<ServiceResult<DepartmentDto?>> GetByIdAsync(long id);
    Task<ServiceResult<DepartmentDto>> CreateAsync(Department department);
    Task<ServiceResult<bool>> UpdateAsync(long id, Department department);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}