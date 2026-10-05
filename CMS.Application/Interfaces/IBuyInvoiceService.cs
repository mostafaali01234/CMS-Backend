// Services/Interfaces/IBuyInvoiceService.cs
using CMS.Application.DTOs.Responses;
using CMS.Domain.DTOs;

namespace CMS.Application.Interfaces;

public interface IBuyInvoiceService
{
    Task<ServiceResult<List<BuyInvoiceDto>>> GetAllAsync();
    Task<ServiceResult<BuyInvoiceDto?>> GetByIdAsync(long id);
    Task<ServiceResult<BuyInvoiceDto>> CreateAsync(BuyInvoiceDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, BuyInvoiceDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}