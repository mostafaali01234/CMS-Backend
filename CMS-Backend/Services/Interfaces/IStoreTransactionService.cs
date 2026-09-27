    using CMS_Backend.Models.DTOs;
using CMS_Backend.Models.DTOs.Responses;

namespace CMS_Backend.Services.Interfaces;

public interface IStoreTransactionService
{
    Task<ServiceResult<List<StoreTransactionDto>>> GetAllAsync();
    Task<ServiceResult<StoreTransactionDto?>> GetByIdAsync(long id);
    Task<ServiceResult<StoreTransactionDto>> CreateAsync(StoreTransactionDto storeTransaction);
    Task<ServiceResult<bool>> UpdateAsync(long id, StoreTransactionDto storeTransaction);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}