    using CMS.Api.Models.DTOs;
using CMS.Api.Models.DTOs.Responses;

namespace CMS.Api.Services.Interfaces;

public interface IStoreTransactionService
{
    Task<ServiceResult<List<StoreTransactionDto>>> GetAllAsync();
    Task<ServiceResult<StoreTransactionDto?>> GetByIdAsync(long id);
    Task<ServiceResult<StoreTransactionDto>> CreateAsync(StoreTransactionDto storeTransaction);
    Task<ServiceResult<bool>> UpdateAsync(long id, StoreTransactionDto storeTransaction);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}