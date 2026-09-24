using CMS_Backend.Models;
using CMS_Backend.Models.DTOs;
using CMS_Backend.Models.DTOs.Responses;

namespace CMS_Backend.Services.Interfaces;

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