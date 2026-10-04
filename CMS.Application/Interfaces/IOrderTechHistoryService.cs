// Services/Interfaces/IOrderTechHistoryService.cs
using CMS.Application.DTOs.Responses;
using CMS.Application.DTOs;

namespace CMS.Application.Interfaces;

public interface IOrderTechHistoryService
{
    Task<ServiceResult<List<OrderTechHistoryDto>>> GetAllAsync();
    Task<ServiceResult<OrderTechHistoryDto?>> GetByIdAsync(long id);
    Task<ServiceResult<OrderTechHistoryDto>> CreateAsync(OrderTechHistoryDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, OrderTechHistoryDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}