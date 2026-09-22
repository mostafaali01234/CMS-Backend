using CMS_Backend.Data;
using CMS_Backend.Models;
using CMS_Backend.Models.DTOs.Responses;
using CMS_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CMS_Backend.Services;

public class JobService : IJobService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<JobService> _logger;

    private const int NameMaxLength = 100;

    public JobService(
        AppDbContext dbContext,
        ILogger<JobService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // Centralized validation used by both Create and Update.
    // isUpdate/currentId let the uniqueness check exclude the record being updated.
    private async Task<string?> ValidateAsync(Job job, bool isUpdate, long? currentId = null)
    {
        if (job is null)
            return "Job payload is required";

        if (string.IsNullOrWhiteSpace(job.Name))
            return "Job name is required";

        if (job.Name.Trim().Length > NameMaxLength)
            return $"Job name cannot exceed {NameMaxLength} characters";

        // Normalize whitespace so "Manager " and "Manager" aren't treated as different names
        var normalizedName = job.Name.Trim();
        job.Name = normalizedName;

        var duplicateQuery = _dbContext.Job
            .Where(j => !j.IsDeleted && j.Name.ToLower() == normalizedName.ToLower());

        if (isUpdate && currentId.HasValue)
            duplicateQuery = duplicateQuery.Where(j => j.Id != currentId.Value);

        var duplicateExists = await duplicateQuery.AnyAsync();
        if (duplicateExists)
            return $"A job named '{normalizedName}' already exists";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<Job>>> GetAllAsync()
    {
        var jobs = await _dbContext.Job.Where(z => !z.IsDeleted).AsNoTracking().ToListAsync();
        return ServiceResult<List<Job>>.Ok(jobs);
    }

    public async Task<ServiceResult<Job?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<Job?>.Failed("A valid job Id is required");

        var job = await _dbContext.Job.Where(z => !z.IsDeleted && z.Id == id).AsNoTracking().FirstOrDefaultAsync();

        if (job is null)
            return ServiceResult<Job?>.NotFound("Job not found");

        return ServiceResult<Job?>.Ok(job);
    }

    public async Task<ServiceResult<Job>> CreateAsync(Job job)
    {
        var validationError = await ValidateAsync(job, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating job: {Error}", validationError);
            return ServiceResult<Job>.Failed(validationError);
        }

        _dbContext.Job.Add(job);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Job created with Id {JobId}", job.Id);
        return ServiceResult<Job>.Ok(job);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, Job job)
    {
        if (job is null)
            return ServiceResult<bool>.Failed("Job payload is required");

        if (id != job.Id)
            return ServiceResult<bool>.Conflict("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.Job.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Job not found");

        var validationError = await ValidateAsync(job, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating job {JobId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        // Update only the mutable field rather than blindly overwriting audit/soft-delete fields
        existing.Name = job.Name;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating Job with Id {JobId}", id);
            throw;
        }

        _logger.LogInformation("Job with Id {JobId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var job = await _dbContext.Job.FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);
        if (job is null)
            return ServiceResult<bool>.NotFound("Job not found");

        // Guard against deleting a job that's still assigned to employees,
        // if Employee has a JobId FK — remove this block if that's not the case.
        var isInUse = await _dbContext.Employee
            .AnyAsync(e => !e.IsDeleted && e.JobId == id);

        if (isInUse)
            return ServiceResult<bool>.Conflict("Cannot delete a job that is still assigned to employees");

        job.IsDeleted = true;
        job.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Job with Id {JobId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}