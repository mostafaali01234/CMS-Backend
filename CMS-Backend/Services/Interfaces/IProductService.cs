using CMS.Domain.Models;
using CMS.Api.Models.DTOs;
using CMS.Api.Models.DTOs.Responses;

namespace CMS.Api.Services.Interfaces;

public interface IProductService
{
    Task<ServiceResult<List<ProductDto>>> GetAllAsync();
    Task<ServiceResult<ProductDto?>> GetByIdAsync(long id);
    Task<ServiceResult<ProductDto>> CreateAsync(ProductDto cat);
    Task<ServiceResult<bool>> UpdateAsync(long id, ProductDto cat);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}