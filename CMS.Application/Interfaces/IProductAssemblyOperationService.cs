
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;

namespace CMS.Application.Interfaces;

public interface IProductAssemblyOperationService
{
    Task<ServiceResult<List<ProductAssemblyOperationDto>>> GetAllAsync();
    Task<ServiceResult<ProductAssemblyOperationDto?>> GetByIdAsync(long id);
    Task<ServiceResult<ProductAssemblyOperationDto>> CreateAsync(ProductAssemblyOperationDto op);
    Task<ServiceResult<bool>> UpdateAsync(long id, ProductAssemblyOperationDto op);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}