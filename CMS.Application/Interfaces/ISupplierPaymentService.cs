// Services/Interfaces/ISupplierPaymentService.cs
using CMS.Application.DTOs.Responses;
using CMS.Domain.DTOs;

namespace CMS.Application.Interfaces;

public interface ISupplierPaymentService
{
    Task<ServiceResult<List<SupplierPaymentDto>>> GetAllAsync();
    Task<ServiceResult<SupplierPaymentDto?>> GetByIdAsync(long id);
    Task<ServiceResult<SupplierPaymentDto>> CreateAsync(SupplierPaymentDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, SupplierPaymentDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}