using CMS.Domain.Models;
using CMS.Api.Models.DTOs.Responses;

namespace CMS.Api.Services.Interfaces;

public interface IJobService
{
    Task<ServiceResult<List<Job>>> GetAllAsync();
    Task<ServiceResult<Job?>> GetByIdAsync(long id);
    Task<ServiceResult<Job>> CreateAsync(Job job);
    Task<ServiceResult<bool>> UpdateAsync(long id, Job job);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}