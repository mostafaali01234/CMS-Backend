// Services/Interfaces/IOrderService.cs
using CMS.Application.DTOs.Responses;
using CMS.Application.DTOs;

namespace CMS.Application.Interfaces;

public interface IOrderService
{
    Task<ServiceResult<List<OrderDto>>> GetAllAsync();
    Task<ServiceResult<PagedResult<OrderDto>>> GetPagedAsync(OrderQueryDto query);
    Task<ServiceResult<OrderDto?>> GetByIdAsync(long id);
    Task<ServiceResult<List<OrderDto>>> GetDoneByCustomerIdAsync(long customerId);
    Task<ServiceResult<List<OrderDto>>> GetOldMaintenanceAsync(long orderId);
    Task<ServiceResult<OrderStatsDto>> GetStatsAsync(string userId, bool allUsers);
    Task<ServiceResult<OrderDto>> CreateAsync(OrderDto dto);
    Task<ServiceResult<OrderDto>> CreateMaintenanceAsync(OrderMaintenanceRequestDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, OrderDto dto);
    Task<ServiceResult<bool>> CancelAsync(long id, OrderCancelDto body);
    Task<ServiceResult<bool>> DeleteAsync(long id, OrderCancelDto body);
}