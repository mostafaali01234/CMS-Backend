using CMS.Domain.Models;
using CMS.Api.Models.DTOs.Responses;

namespace CMS.Api.Services.Interfaces;

public interface IStateService
{
    Task<ServiceResult<List<CountryState>>> GetAllAsync();
    Task<ServiceResult<CountryState?>> GetByIdAsync(long id);
    Task<ServiceResult<CountryState>> CreateAsync(CountryState state);
    Task<ServiceResult<bool>> UpdateAsync(long id, CountryState state);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}