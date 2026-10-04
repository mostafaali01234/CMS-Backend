// Services/Interfaces/IEmployeeLoanInstallmentService.cs
using CMS.Application.DTOs.Responses;
using CMS.Domain.Models.DTOs;

namespace CMS.Application.Interfaces;

public interface IEmployeeLoanInstallmentService
{
    Task<ServiceResult<List<EmployeeLoanInstallmentDto>>> GetAllAsync();
    Task<ServiceResult<EmployeeLoanInstallmentDto?>> GetByIdAsync(long id);
    Task<ServiceResult<EmployeeLoanInstallmentDto>> CreateAsync(EmployeeLoanInstallmentDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, EmployeeLoanInstallmentDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}