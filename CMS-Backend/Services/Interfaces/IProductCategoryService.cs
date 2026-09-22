using CMS_Backend.Models;
using CMS_Backend.Models.DTOs.Responses;

namespace CMS_Backend.Services.Interfaces;

public interface IProductCategoryService
{
    Task<ServiceResult<List<ProductCategory>>> GetAllAsync();
    Task<ServiceResult<ProductCategory?>> GetByIdAsync(long id);
    Task<ServiceResult<ProductCategory>> CreateAsync(ProductCategory cat);
    Task<ServiceResult<bool>> UpdateAsync(long id, ProductCategory cat);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}