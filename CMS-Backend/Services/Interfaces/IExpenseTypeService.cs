using CMS_Backend.Models;
using CMS_Backend.Models.DTOs.Responses;

namespace CMS_Backend.Services.Interfaces;

public interface IExpenseTypeService
{
    Task<ServiceResult<List<ExpenseType>>> GetAllAsync();
    Task<ServiceResult<ExpenseType?>> GetByIdAsync(long id);
    Task<ServiceResult<ExpenseType>> CreateAsync(ExpenseType exType);
    Task<ServiceResult<bool>> UpdateAsync(long id, ExpenseType exType);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}