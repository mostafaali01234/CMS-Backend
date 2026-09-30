using CMS.Domain.Models;
using CMS.Application.DTOs.Responses;

namespace CMS.Application.Interfaces;

public interface IJobService
{
    Task<ServiceResult<List<Job>>> GetAllAsync();
    Task<ServiceResult<Job?>> GetByIdAsync(long id);
    Task<ServiceResult<Job>> CreateAsync(Job job);
    Task<ServiceResult<bool>> UpdateAsync(long id, Job job);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}