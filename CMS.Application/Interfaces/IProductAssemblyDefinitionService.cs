using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;

namespace CMS.Application.Interfaces;

public interface IProductAssemblyDefinitionService
{
    Task<ServiceResult<List<ProductAssemblyDefinitionDto>>> GetAllAsync();
    Task<ServiceResult<ProductAssemblyDefinitionDto?>> GetByIdAsync(long id);
    Task<ServiceResult<ProductAssemblyDefinitionDto>> CreateAsync(ProductAssemblyDefinitionDto cat);
    Task<ServiceResult<bool>> UpdateAsync(long id, ProductAssemblyDefinitionDto cat);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}