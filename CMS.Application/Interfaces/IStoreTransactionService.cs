    using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;

namespace CMS.Application.Interfaces;

public interface IStoreTransactionService
{
    Task<ServiceResult<List<StoreTransactionDto>>> GetAllAsync();
    Task<ServiceResult<StoreTransactionDto?>> GetByIdAsync(long id);
    Task<ServiceResult<StoreTransactionDto>> CreateAsync(StoreTransactionDto storeTransaction);
    Task<ServiceResult<bool>> UpdateAsync(long id, StoreTransactionDto storeTransaction);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}