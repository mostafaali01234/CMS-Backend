// Services/Interfaces/ISaleInvoiceService.cs
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Domain.DTOs;

namespace CMS.Application.Interfaces;

public interface ISaleInvoiceService
{
    Task<ServiceResult<List<SaleInvoiceDto>>> GetAllAsync();
    Task<ServiceResult<PagedResult<SaleInvoiceDto, InvoiceListSummaryDto>>> GetPagedAsync(SaleInvoiceFilterDto filter);
    Task<ServiceResult<SaleInvoiceDto?>> GetByIdAsync(long id);
    Task<ServiceResult<SaleInvoiceDto>> CreateAsync(SaleInvoiceDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, SaleInvoiceDto dto, bool isSuperAdmin = false);
    Task<ServiceResult<bool>> DeleteAsync(long id, bool isSuperAdmin = false);

    Task<ServiceResult<List<SaleInvoiceDto>>> GetUnpaidByCustomerAsync(long customerId);
    Task<ServiceResult<List<SaleInvoiceDto>>> GetUnpaidByOrderAsync(long orderId);
    Task<ServiceResult<List<SaleInvoiceDto>>> GetSaleByCustomerAsync(long customerId);
    Task<ServiceResult<SaleInvoiceDto>> GetDraftFromOrderAsync(long orderId);
    Task<ServiceResult<SaleReturnableInvoiceDto>> GetReturnableAsync(long invoiceId, long? excludeReturnId);
    Task<ServiceResult<SaleInvoiceDto>> CreateReturnAsync(SaleReturnRequestDto request);
    Task<ServiceResult<bool>> UpdateReturnAsync(long id, SaleReturnRequestDto request, bool isSuperAdmin = false);

    Task<(byte[] Bytes, string FileName)?> ExportAsync(long id);
}