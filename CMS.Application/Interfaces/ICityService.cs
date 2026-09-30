using CMS.Domain.Models;
using CMS.Application.DTOs.Responses;

namespace CMS.Application.Interfaces;

public interface ICityService
{
    Task<ServiceResult<List<City>>> GetAllAsync();
    Task<ServiceResult<City?>> GetByIdAsync(long id);
    Task<ServiceResult<City>> CreateAsync(City city);
    Task<ServiceResult<bool>> UpdateAsync(long id, City city);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}