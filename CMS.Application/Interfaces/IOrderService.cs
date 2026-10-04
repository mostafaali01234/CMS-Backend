// Services/Interfaces/IOrderService.cs
using CMS.Application.DTOs.Responses;
using CMS.Application.DTOs;

namespace CMS.Application.Interfaces;

public interface IOrderService
{
    Task<ServiceResult<List<OrderDto>>> GetAllAsync();
    Task<ServiceResult<OrderDto?>> GetByIdAsync(long id);
    Task<ServiceResult<OrderDto>> CreateAsync(OrderDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, OrderDto dto);
    Task<ServiceResult<bool>> CancelAsync(long id);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}