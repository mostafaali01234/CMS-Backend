using CMS_Backend.Models;
using CMS_Backend.Models.DTOs.Responses;

namespace CMS_Backend.Services.Interfaces;

public interface IProjectService
{
    Task<ServiceResult<List<Project>>> GetAllAsync();
    Task<ServiceResult<Project?>> GetByIdAsync(long id);
    Task<ServiceResult<Project>> CreateAsync(Project project);
    Task<ServiceResult<bool>> UpdateAsync(long id, Project project);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}