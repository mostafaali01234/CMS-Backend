// Services/Interfaces/IBuyInvoiceService.cs
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Domain.DTOs;

namespace CMS.Application.Interfaces;

public interface IBuyInvoiceService
{
    Task<ServiceResult<List<BuyInvoiceDto>>> GetAllAsync();
    Task<ServiceResult<PagedResult<BuyInvoiceDto, InvoiceListSummaryDto>>> GetPagedAsync(BuyInvoiceFilterDto filter);
    Task<ServiceResult<BuyInvoiceDto?>> GetByIdAsync(long id);
    Task<ServiceResult<BuyInvoiceDto>> CreateAsync(BuyInvoiceDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, BuyInvoiceDto dto, bool isSuperAdmin = false);
    Task<ServiceResult<bool>> DeleteAsync(long id, bool isSuperAdmin = false);
    Task<ServiceResult<List<BuyInvoiceDto>>> GetUnpaidBySupplierAsync(long supplierId);

    Task<ServiceResult<List<BuyInvoiceDto>>> GetBuyBySupplierAsync(long supplierId);
    Task<ServiceResult<ReturnableInvoiceDto>> GetReturnableAsync(long invoiceId, long? excludeReturnId);
    Task<ServiceResult<BuyInvoiceDto>> CreateReturnAsync(BuyReturnRequestDto request);
    Task<ServiceResult<bool>> UpdateReturnAsync(long id, BuyReturnRequestDto request);

    Task<(byte[] Bytes, string FileName)?> ExportAsync(long id);
}