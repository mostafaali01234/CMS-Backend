using CMS.Domain.Models;
using CMS.Application.DTOs.Responses;

namespace CMS.Application.Interfaces;

public interface IExpenseCategoryService
{
    Task<ServiceResult<List<ExpenseCategory>>> GetAllAsync();
    Task<ServiceResult<ExpenseCategory?>> GetByIdAsync(long id);
    Task<ServiceResult<ExpenseCategory>> CreateAsync(ExpenseCategory exCat);
    Task<ServiceResult<bool>> UpdateAsync(long id, ExpenseCategory exCat);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}