// Services/Interfaces/IShiftService.cs
using CMS.Application.DTOs.Responses;
using CMS.Domain.DTOs;

namespace CMS.Application.Interfaces;

public interface IShiftService
{
    Task<ServiceResult<List<ShiftDto>>> GetAllAsync();
    Task<ServiceResult<List<ShiftDto>>> GetAllByDateAsync(DateTime date);
    Task<ServiceResult<ShiftDto?>> GetByIdAsync(long id);
    Task<ServiceResult<ShiftDto?>> GetByStoreIdAndDateAsync(long storeId, DateTime date);
    Task<ServiceResult<ShiftDto>> CreateAsync(ShiftDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, ShiftDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}