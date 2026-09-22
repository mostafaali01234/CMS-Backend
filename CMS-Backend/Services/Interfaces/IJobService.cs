using CMS_Backend.Models;
using CMS_Backend.Models.DTOs.Responses;

namespace CMS_Backend.Services.Interfaces;

public interface IJobService
{
    Task<ServiceResult<List<Job>>> GetAllAsync();
    Task<ServiceResult<Job?>> GetByIdAsync(long id);
    Task<ServiceResult<Job>> CreateAsync(Job department);
    Task<ServiceResult<bool>> UpdateAsync(long id, Job department);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}