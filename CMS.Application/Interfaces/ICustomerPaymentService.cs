// Services/Interfaces/ICustomerPaymentService.cs
using CMS.Application.DTOs.Responses;
using CMS.Domain.DTOs;

namespace CMS.Application.Interfaces;

public interface ICustomerPaymentService
{
    Task<ServiceResult<List<CustomerPaymentDto>>> GetAllAsync();
    Task<ServiceResult<CustomerPaymentDto?>> GetByIdAsync(long id);
    Task<ServiceResult<CustomerPaymentDto>> CreateAsync(CustomerPaymentDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, CustomerPaymentDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}