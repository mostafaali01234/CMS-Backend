using CMS.Domain.Models;
using CMS.Application.DTOs.Responses;

namespace CMS.Application.Interfaces;

public interface IMoneySafeCategoryService
{
    Task<ServiceResult<List<MoneySafeCategory>>> GetAllAsync();
    Task<ServiceResult<MoneySafeCategory?>> GetByIdAsync(long id);
    Task<ServiceResult<MoneySafeCategory>> CreateAsync(MoneySafeCategory cat);
    Task<ServiceResult<bool>> UpdateAsync(long id, MoneySafeCategory cat);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}