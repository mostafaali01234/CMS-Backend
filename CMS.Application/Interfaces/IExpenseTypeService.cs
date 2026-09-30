using CMS.Domain.Models;
using CMS.Application.DTOs.Responses;

namespace CMS.Application.Interfaces;

public interface IExpenseTypeService
{
    Task<ServiceResult<List<ExpenseType>>> GetAllAsync();
    Task<ServiceResult<ExpenseType?>> GetByIdAsync(long id);
    Task<ServiceResult<ExpenseType>> CreateAsync(ExpenseType exType);
    Task<ServiceResult<bool>> UpdateAsync(long id, ExpenseType exType);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}