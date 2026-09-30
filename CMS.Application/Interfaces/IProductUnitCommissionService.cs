using CMS.Domain.Models;
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;

namespace CMS.Application.Interfaces;

public interface IProductUnitCommissionService
{
    Task<ServiceResult<List<ProductUnitCommission>>> GetAllAsync();
    Task<ServiceResult<ProductUnitCommission?>> GetByIdAsync(long id);
    Task<ServiceResult<List<ProductUnitCommission>>> GetByProductIdAsync(long productId);
    Task<ServiceResult<ProductUnitCommission>> CreateAsync(ProductUnitCommission comm);
    Task<ServiceResult<bool>> CreateForProductAsync(List<ProductUnitCommission> commList);
    Task<ServiceResult<bool>> UpdateAsync(long id, ProductUnitCommission comm);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}