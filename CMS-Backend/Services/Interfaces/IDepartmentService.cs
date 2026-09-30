using CMS.Domain.Models;
using CMS.Api.Models.DTOs;
using CMS.Api.Models.DTOs.Responses;

namespace CMS.Api.Services.Interfaces;

public interface IDepartmentService
{
    Task<ServiceResult<List<DepartmentDto>>> GetAllAsync();
    Task<ServiceResult<DepartmentDto?>> GetByIdAsync(long id);
    Task<ServiceResult<DepartmentDto>> CreateAsync(Department department);
    Task<ServiceResult<bool>> UpdateAsync(long id, Department department);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}