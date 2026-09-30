// Services/Interfaces/IMoneySafeTransactionService.cs
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;

namespace CMS.Application.Interfaces;

public interface IMoneySafeTransactionService
{
    Task<ServiceResult<List<MoneySafeTransactionDto>>> GetAllAsync();
    Task<ServiceResult<MoneySafeTransactionDto?>> GetByIdAsync(long id);
    Task<ServiceResult<MoneySafeTransactionDto>> CreateAsync(MoneySafeTransactionDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, MoneySafeTransactionDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}