using CMS.Application.Interfaces.Configuration;
using CMS.Domain.Models;
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Extensions.Logging;

namespace CMS.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<ProjectService> _logger;

    private const int NameMaxLength = 100;

    public ProjectService(
        IAppDbContext dbContext,
        ILogger<ProjectService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // Centralized validation used by both Create and Update.
    // isUpdate/currentId let the uniqueness check exclude the record being updated.
    private async Task<string?> ValidateAsync(Project project, bool isUpdate, long? currentId = null)
    {
        if (project is null)
            return "Project payload is required";

        if (string.IsNullOrWhiteSpace(project.Name))
            return "Project name is required";

        if (project.Name.Trim().Length > NameMaxLength)
            return $"Project name cannot exceed {NameMaxLength} characters";

        // Normalize whitespace so "Manager " and "Manager" aren't treated as different names
        var normalizedName = project.Name.Trim();
        project.Name = normalizedName;

        var duplicateQuery = _dbContext.Project
            .Where(j => !j.IsDeleted && j.Name.ToLower() == normalizedName.ToLower());

        if (isUpdate && currentId.HasValue)
            duplicateQuery = duplicateQuery.Where(j => j.Id != currentId.Value);

        var duplicateExists = await duplicateQuery.AnyAsync();
        if (duplicateExists)
            return $"A Project named '{normalizedName}' already exists";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<Project>>> GetAllAsync()
    {
        var projects = await _dbContext.Project.Where(z => !z.IsDeleted).AsNoTracking().ToListAsync();
        return ServiceResult<List<Project>>.Ok(projects);
    }

    public async Task<ServiceResult<Project?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<Project?>.Failed("A valid Project Id is required");

        var project = await _dbContext.Project.Where(z => !z.IsDeleted && z.Id == id).AsNoTracking().FirstOrDefaultAsync();

        if (project is null)
            return ServiceResult<Project?>.NotFound("Project not found");

        return ServiceResult<Project?>.Ok(project);
    }

    public async Task<ServiceResult<Project>> CreateAsync(Project project)
    {
        var validationError = await ValidateAsync(project, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating Project: {Error}", validationError);
            return ServiceResult<Project>.Failed(validationError);
        }

        _dbContext.Project.Add(project);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Project created with Id {ProjectId}", project.Id);
        return ServiceResult<Project>.Ok(project);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, Project project)
    {
        if (project is null)
            return ServiceResult<bool>.Failed("Project payload is required");

        if (id != project.Id)
            return ServiceResult<bool>.Conflict("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.Project.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Project not found");

        var validationError = await ValidateAsync(project, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating Project {ProjectId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        // Update only the mutable field rather than blindly overwriting audit/soft-delete fields
        existing.Name = project.Name;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating Project with Id {ProjectId}", id);
            throw;
        }

        _logger.LogInformation("Project with Id {ProjectId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var project = await _dbContext.Project.FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);
        if (project is null)
            return ServiceResult<bool>.NotFound("Project not found");

        project.IsDeleted = true;
        project.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Project with Id {ProjectId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}