using CMS.Domain.Models;
using CMS.Api.Models.DTOs.Responses;

namespace CMS.Api.Services.Interfaces;

public interface IExpenseCategoryService
{
    Task<ServiceResult<List<ExpenseCategory>>> GetAllAsync();
    Task<ServiceResult<ExpenseCategory?>> GetByIdAsync(long id);
    Task<ServiceResult<ExpenseCategory>> CreateAsync(ExpenseCategory exCat);
    Task<ServiceResult<bool>> UpdateAsync(long id, ExpenseCategory exCat);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}