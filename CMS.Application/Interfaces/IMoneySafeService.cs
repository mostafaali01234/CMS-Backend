// Services/Interfaces/IMoneySafeService.cs
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;

namespace CMS.Application.Interfaces;

public interface IMoneySafeService
{
    Task<ServiceResult<List<MoneySafeDto>>> GetAllAsync();
    Task<ServiceResult<MoneySafeDto?>> GetByIdAsync(long id);
    Task<ServiceResult<MoneySafeDto>> CreateAsync(MoneySafeDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, MoneySafeDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}