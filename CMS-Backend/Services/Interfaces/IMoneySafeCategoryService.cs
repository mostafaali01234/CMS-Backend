using CMS.Domain.Models;
using CMS.Api.Models.DTOs.Responses;

namespace CMS.Api.Services.Interfaces;

public interface IMoneySafeCategoryService
{
    Task<ServiceResult<List<MoneySafeCategory>>> GetAllAsync();
    Task<ServiceResult<MoneySafeCategory?>> GetByIdAsync(long id);
    Task<ServiceResult<MoneySafeCategory>> CreateAsync(MoneySafeCategory cat);
    Task<ServiceResult<bool>> UpdateAsync(long id, MoneySafeCategory cat);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}