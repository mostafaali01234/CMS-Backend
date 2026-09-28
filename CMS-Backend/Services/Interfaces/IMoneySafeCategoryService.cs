using CMS_Backend.Models;
using CMS_Backend.Models.DTOs.Responses;

namespace CMS_Backend.Services.Interfaces;

public interface IMoneySafeCategoryService
{
    Task<ServiceResult<List<MoneySafeCategory>>> GetAllAsync();
    Task<ServiceResult<MoneySafeCategory?>> GetByIdAsync(long id);
    Task<ServiceResult<MoneySafeCategory>> CreateAsync(MoneySafeCategory cat);
    Task<ServiceResult<bool>> UpdateAsync(long id, MoneySafeCategory cat);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}