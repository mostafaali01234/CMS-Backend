// Services/Interfaces/IMoneySafeTransactionService.cs
using CMS.Api.Models.DTOs;
using CMS.Api.Models.DTOs.Responses;

namespace CMS.Api.Services.Interfaces;

public interface IMoneySafeTransactionService
{
    Task<ServiceResult<List<MoneySafeTransactionDto>>> GetAllAsync();
    Task<ServiceResult<MoneySafeTransactionDto?>> GetByIdAsync(long id);
    Task<ServiceResult<MoneySafeTransactionDto>> CreateAsync(MoneySafeTransactionDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, MoneySafeTransactionDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}