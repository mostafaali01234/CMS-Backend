using CMS_Backend.Models;
using CMS_Backend.Models.DTOs;
using CMS_Backend.Models.DTOs.Responses;

namespace CMS_Backend.Services.Interfaces;

public interface IDepartmentService
{
    Task<ServiceResult<List<DepartmentDto>>> GetAllAsync();
    Task<ServiceResult<DepartmentDto?>> GetByIdAsync(long id);
    Task<ServiceResult<DepartmentDto>> CreateAsync(Department department);
    Task<ServiceResult<bool>> UpdateAsync(long id, Department department);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}