// Services/Interfaces/ISaleInvoiceService.cs
using CMS.Application.DTOs.Responses;
using CMS.Domain.DTOs;

namespace CMS.Application.Interfaces;

public interface ISaleInvoiceService
{
    Task<ServiceResult<List<SaleInvoiceDto>>> GetAllAsync();
    Task<ServiceResult<SaleInvoiceDto?>> GetByIdAsync(long id);
    Task<ServiceResult<SaleInvoiceDto>> CreateAsync(SaleInvoiceDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, SaleInvoiceDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}