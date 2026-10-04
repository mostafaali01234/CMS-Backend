// Services/Interfaces/IExpenseService.cs
using CMS.Application.DTOs.Responses;
using CMS.Domain.DTOs;

namespace CMS.Application.Interfaces;

public interface IExpenseService
{
    Task<ServiceResult<List<ExpenseDto>>> GetAllAsync();
    Task<ServiceResult<ExpenseDto?>> GetByIdAsync(long id);
    Task<ServiceResult<ExpenseDto>> CreateAsync(ExpenseDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, ExpenseDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}