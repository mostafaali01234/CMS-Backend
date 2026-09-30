using CMS.Api.Models.DTOs;
using CMS.Api.Models.DTOs.Responses;

namespace CMS.Api.Services.Interfaces;

public interface ICustomerService
{
    Task<ServiceResult<List<CustomerDto>>> GetAllAsync();
    Task<ServiceResult<CustomerDto?>> GetByIdAsync(long id);
    Task<ServiceResult<CustomerDto>> CreateAsync(CustomerDto state);
    Task<ServiceResult<bool>> UpdateAsync(long id, CustomerDto state);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}