using CMS.Domain.Models;
using CMS.Application.DTOs.Responses;

namespace CMS.Application.Interfaces;

public interface IProductUnitService
{
    Task<ServiceResult<List<ProductUnit>>> GetAllAsync();
    Task<ServiceResult<ProductUnit?>> GetByIdAsync(long id);
    Task<ServiceResult<ProductUnit>> CreateAsync(ProductUnit unit);
    Task<ServiceResult<bool>> UpdateAsync(long id, ProductUnit unit);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}