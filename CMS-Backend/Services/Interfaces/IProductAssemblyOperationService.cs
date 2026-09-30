
using CMS.Api.Models.DTOs;
using CMS.Api.Models.DTOs.Responses;

namespace CMS.Api.Services.Interfaces;

public interface IProductAssemblyOperationService
{
    Task<ServiceResult<List<ProductAssemblyOperationDto>>> GetAllAsync();
    Task<ServiceResult<ProductAssemblyOperationDto?>> GetByIdAsync(long id);
    Task<ServiceResult<ProductAssemblyOperationDto>> CreateAsync(ProductAssemblyOperationDto op);
    Task<ServiceResult<bool>> UpdateAsync(long id, ProductAssemblyOperationDto op);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}