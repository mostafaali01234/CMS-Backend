
using CMS_Backend.Models.DTOs;
using CMS_Backend.Models.DTOs.Responses;

namespace CMS_Backend.Services.Interfaces;

public interface IProductAssemblyOperationService
{
    Task<ServiceResult<List<ProductAssemblyOperationDto>>> GetAllAsync();
    Task<ServiceResult<ProductAssemblyOperationDto?>> GetByIdAsync(long id);
    Task<ServiceResult<ProductAssemblyOperationDto>> CreateAsync(ProductAssemblyOperationDto op);
    Task<ServiceResult<bool>> UpdateAsync(long id, ProductAssemblyOperationDto op);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}