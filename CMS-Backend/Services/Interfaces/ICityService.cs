using CMS_Backend.Models;
using CMS_Backend.Models.DTOs.Responses;

namespace CMS_Backend.Services.Interfaces;

public interface ICityService
{
    Task<ServiceResult<List<City>>> GetAllAsync();
    Task<ServiceResult<City?>> GetByIdAsync(long id);
    Task<ServiceResult<City>> CreateAsync(City city);
    Task<ServiceResult<bool>> UpdateAsync(long id, City city);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}