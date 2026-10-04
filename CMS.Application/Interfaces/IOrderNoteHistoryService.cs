// Services/Interfaces/IOrderNoteHistoryService.cs
using CMS.Application.DTOs.Responses;
using CMS.Application.DTOs;

namespace CMS.Application.Interfaces;

public interface IOrderNoteHistoryService
{
    Task<ServiceResult<List<OrderNoteHistoryDto>>> GetAllAsync();
    Task<ServiceResult<OrderNoteHistoryDto?>> GetByIdAsync(long id);
    Task<ServiceResult<OrderNoteHistoryDto?>> GetByOrderIdAsync(long orderId);
    Task<ServiceResult<OrderNoteHistoryDto>> CreateAsync(OrderNoteHistoryDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, OrderNoteHistoryDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}