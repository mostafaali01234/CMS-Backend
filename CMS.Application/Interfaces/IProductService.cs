using CMS.Domain.Models;
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;

namespace CMS.Application.Interfaces;

public interface IProductService
{
    Task<ServiceResult<List<ProductDto>>> GetAllAsync();
    Task<ServiceResult<ProductDto?>> GetByIdAsync(long id);
    Task<ServiceResult<ProductDto>> CreateAsync(ProductDto cat);
    Task<ServiceResult<bool>> UpdateAsync(long id, ProductDto cat);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}