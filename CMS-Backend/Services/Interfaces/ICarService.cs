    using CMS_Backend.Models.DTOs;
using CMS_Backend.Models.DTOs.Responses;

namespace CMS_Backend.Services.Interfaces;

public interface ICarService
{
    Task<ServiceResult<List<CarDto>>> GetAllAsync();
    Task<ServiceResult<CarDto?>> GetByIdAsync(long id);
    Task<ServiceResult<CarDto>> CreateAsync(CarDto car);
    Task<ServiceResult<bool>> UpdateAsync(long id, CarDto car);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}