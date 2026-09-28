// Services/Interfaces/IMoneySafeService.cs
using CMS_Backend.Models.DTOs;
using CMS_Backend.Models.DTOs.Responses;

namespace CMS_Backend.Services.Interfaces;

public interface IMoneySafeService
{
    Task<ServiceResult<List<MoneySafeDto>>> GetAllAsync();
    Task<ServiceResult<MoneySafeDto?>> GetByIdAsync(long id);
    Task<ServiceResult<MoneySafeDto>> CreateAsync(MoneySafeDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, MoneySafeDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}