    using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;

namespace CMS.Application.Interfaces;

public interface IStoreService
{
    Task<ServiceResult<List<StoreDto>>> GetAllAsync();
    Task<ServiceResult<StoreDto?>> GetByIdAsync(long id);
    Task<ServiceResult<StoreDto>> CreateAsync(StoreDto store);
    Task<ServiceResult<bool>> UpdateAsync(long id, StoreDto store);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}