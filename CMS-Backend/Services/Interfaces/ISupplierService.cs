using CMS_Backend.Models;
using CMS_Backend.Models.DTOs.Responses;

namespace CMS_Backend.Services.Interfaces;

public interface ISupplierService
{
    Task<ServiceResult<List<Supplier>>> GetAllAsync();
    Task<ServiceResult<Supplier?>> GetByIdAsync(long id);
    Task<ServiceResult<Supplier>> CreateAsync(Supplier state);
    Task<ServiceResult<bool>> UpdateAsync(long id, Supplier state);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}