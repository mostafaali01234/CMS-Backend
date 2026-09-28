// Services/Interfaces/IMoneySafeTransactionService.cs
using CMS_Backend.Models.DTOs;
using CMS_Backend.Models.DTOs.Responses;

namespace CMS_Backend.Services.Interfaces;

public interface IMoneySafeTransactionService
{
    Task<ServiceResult<List<MoneySafeTransactionDto>>> GetAllAsync();
    Task<ServiceResult<MoneySafeTransactionDto?>> GetByIdAsync(long id);
    Task<ServiceResult<MoneySafeTransactionDto>> CreateAsync(MoneySafeTransactionDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, MoneySafeTransactionDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}