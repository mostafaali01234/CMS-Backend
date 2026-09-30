using CMS.Domain.Models;
using CMS.Api.Models.DTOs.Responses;

namespace CMS.Api.Services.Interfaces;

public interface ICityService
{
    Task<ServiceResult<List<City>>> GetAllAsync();
    Task<ServiceResult<City?>> GetByIdAsync(long id);
    Task<ServiceResult<City>> CreateAsync(City city);
    Task<ServiceResult<bool>> UpdateAsync(long id, City city);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}