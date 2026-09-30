// Services/Interfaces/IMoneySafeService.cs
using CMS.Api.Models.DTOs;
using CMS.Api.Models.DTOs.Responses;

namespace CMS.Api.Services.Interfaces;

public interface IMoneySafeService
{
    Task<ServiceResult<List<MoneySafeDto>>> GetAllAsync();
    Task<ServiceResult<MoneySafeDto?>> GetByIdAsync(long id);
    Task<ServiceResult<MoneySafeDto>> CreateAsync(MoneySafeDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, MoneySafeDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}