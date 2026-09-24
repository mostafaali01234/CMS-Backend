using CMS_Backend.Models.DTOs;
using CMS_Backend.Models.DTOs.Responses;

namespace CMS_Backend.Services.Interfaces;

public interface ICustomerService
{
    Task<ServiceResult<List<CustomerDto>>> GetAllAsync();
    Task<ServiceResult<CustomerDto?>> GetByIdAsync(long id);
    Task<ServiceResult<CustomerDto>> CreateAsync(CustomerDto state);
    Task<ServiceResult<bool>> UpdateAsync(long id, CustomerDto state);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}