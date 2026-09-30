using CMS.Api.Models.DTOs;
using CMS.Api.Models.DTOs.Responses;

namespace CMS.Api.Services.Interfaces;

public interface IProductAssemblyDefinitionService
{
    Task<ServiceResult<List<ProductAssemblyDefinitionDto>>> GetAllAsync();
    Task<ServiceResult<ProductAssemblyDefinitionDto?>> GetByIdAsync(long id);
    Task<ServiceResult<ProductAssemblyDefinitionDto>> CreateAsync(ProductAssemblyDefinitionDto cat);
    Task<ServiceResult<bool>> UpdateAsync(long id, ProductAssemblyDefinitionDto cat);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}