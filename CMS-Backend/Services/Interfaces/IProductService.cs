using CMS_Backend.Models;
using CMS_Backend.Models.DTOs;
using CMS_Backend.Models.DTOs.Responses;

namespace CMS_Backend.Services.Interfaces;

public interface IProductService
{
    Task<ServiceResult<List<ProductDto>>> GetAllAsync();
    Task<ServiceResult<ProductDto?>> GetByIdAsync(long id);
    Task<ServiceResult<ProductDto>> CreateAsync(ProductDto cat);
    Task<ServiceResult<bool>> UpdateAsync(long id, ProductDto cat);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}