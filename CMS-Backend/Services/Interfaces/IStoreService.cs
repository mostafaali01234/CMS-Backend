    using CMS.Api.Models.DTOs;
using CMS.Api.Models.DTOs.Responses;

namespace CMS.Api.Services.Interfaces;

public interface IStoreService
{
    Task<ServiceResult<List<StoreDto>>> GetAllAsync();
    Task<ServiceResult<StoreDto?>> GetByIdAsync(long id);
    Task<ServiceResult<StoreDto>> CreateAsync(StoreDto store);
    Task<ServiceResult<bool>> UpdateAsync(long id, StoreDto store);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}