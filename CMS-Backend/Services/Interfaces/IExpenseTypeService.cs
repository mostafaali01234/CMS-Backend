using CMS.Domain.Models;
using CMS.Api.Models.DTOs.Responses;

namespace CMS.Api.Services.Interfaces;

public interface IExpenseTypeService
{
    Task<ServiceResult<List<ExpenseType>>> GetAllAsync();
    Task<ServiceResult<ExpenseType?>> GetByIdAsync(long id);
    Task<ServiceResult<ExpenseType>> CreateAsync(ExpenseType exType);
    Task<ServiceResult<bool>> UpdateAsync(long id, ExpenseType exType);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}