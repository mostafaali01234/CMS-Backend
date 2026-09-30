    using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;

namespace CMS.Application.Interfaces;

public interface ICarService
{
    Task<ServiceResult<List<CarDto>>> GetAllAsync();
    Task<ServiceResult<CarDto?>> GetByIdAsync(long id);
    Task<ServiceResult<CarDto>> CreateAsync(CarDto car);
    Task<ServiceResult<bool>> UpdateAsync(long id, CarDto car);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}