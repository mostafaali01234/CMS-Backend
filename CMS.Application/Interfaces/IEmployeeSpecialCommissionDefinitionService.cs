// Services/Interfaces/IEmployeeCommissionService.cs
using CMS.Application.DTOs.Responses;
using CMS.Domain.DTOs;

namespace CMS.Application.Interfaces;

public interface IEmployeeSpecialCommissionDefinitionService
{
    Task<ServiceResult<List<EmployeeSpecialCommissionDefinitionDto>>> GetAllAsync();
    Task<ServiceResult<EmployeeSpecialCommissionDefinitionDto?>> GetByIdAsync(long id);
    Task<ServiceResult<EmployeeSpecialCommissionDefinitionDto>> CreateAsync(EmployeeSpecialCommissionDefinitionDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, EmployeeSpecialCommissionDefinitionDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}