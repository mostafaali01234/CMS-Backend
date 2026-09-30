using CMS.Domain.Models;
using CMS.Application.DTOs.Responses;

namespace CMS.Application.Interfaces;

public interface ISupplierService
{
    Task<ServiceResult<List<Supplier>>> GetAllAsync();
    Task<ServiceResult<Supplier?>> GetByIdAsync(long id);
    Task<ServiceResult<Supplier>> CreateAsync(Supplier state);
    Task<ServiceResult<bool>> UpdateAsync(long id, Supplier state);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}