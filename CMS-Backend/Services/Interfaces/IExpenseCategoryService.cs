using CMS_Backend.Models;
using CMS_Backend.Models.DTOs.Responses;

namespace CMS_Backend.Services.Interfaces;

public interface IExpenseCategoryService
{
    Task<ServiceResult<List<ExpenseCategory>>> GetAllAsync();
    Task<ServiceResult<ExpenseCategory?>> GetByIdAsync(long id);
    Task<ServiceResult<ExpenseCategory>> CreateAsync(ExpenseCategory exCat);
    Task<ServiceResult<bool>> UpdateAsync(long id, ExpenseCategory exCat);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}