using CMS_Backend.Data;
using CMS_Backend.Models;
using CMS_Backend.Models.DTOs;
using CMS_Backend.Models.DTOs.Responses;
using CMS_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;
namespace CMS_Backend.Services;

public class DepartmentService : IDepartmentService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<DepartmentService> _logger;

    private const int NameMaxLength = 100;

    public DepartmentService(
        AppDbContext dbContext,
        ILogger<DepartmentService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private List<DepartmentDto> convertToDto(List<Department> depts)
    {
        var dtos = new List<DepartmentDto>();

        foreach (var dept in depts)
        {
            dtos.Add(new DepartmentDto
            {
                Id = dept.Id,
                Name = dept.Name,
                ManagerId = dept.ManagerId,
                ManagerName = dept.Manager?.Name ?? "",
                CreatedAtUtc = dept.CreatedAtUtc,
                CreatedBy = dept.CreatedBy ?? "",
                UpdatedAtUtc = dept.UpdatedAtUtc,
                UpdatedBy = dept.UpdatedBy ?? ""
            });
        }

        return dtos;
    }

    // Centralized validation used by both Create and Update.
    // isUpdate/currentId let the uniqueness check exclude the record being updated.
    private async Task<string?> ValidateAsync(Department department, bool isUpdate, long? currentId = null)
    {
        if (department is null)
            return "Department payload is required";

        if (string.IsNullOrWhiteSpace(department.Name))
            return "Department name is required";

        if (department.Name.Trim().Length > NameMaxLength)
            return $"Department name cannot exceed {NameMaxLength} characters";

        // Normalize whitespace so "Sales " and "Sales" aren't treated as different names
        var normalizedName = department.Name.Trim();
        department.Name = normalizedName;

        var duplicateQuery = _dbContext.Department
            .Where(d => !d.IsDeleted && d.Name.ToLower() == normalizedName.ToLower());

        if (isUpdate && currentId.HasValue)
            duplicateQuery = duplicateQuery.Where(d => d.Id != currentId.Value);

        var duplicateExists = await duplicateQuery.AnyAsync();
        if (duplicateExists)
            return $"A department named '{normalizedName}' already exists";

        if (department.ManagerId <= 0)
            return "ManagerId is required";

        var managerExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == department.ManagerId && !e.IsDeleted);

        if (!managerExists)
            return $"Manager with Id {department.ManagerId} was not found";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<DepartmentDto>>> GetAllAsync()
    {
        var departments = await _dbContext.Department.Where(z => !z.IsDeleted).AsNoTracking().ToListAsync();
        return ServiceResult<List<DepartmentDto>>.Ok(convertToDto(departments));
    }

    public async Task<ServiceResult<DepartmentDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<DepartmentDto?>.Failed("A valid department Id is required");

        var department = await _dbContext.Department.Where(z => !z.IsDeleted && z.Id == id).AsNoTracking().ToListAsync();

        var dto = convertToDto(department).FirstOrDefault();
        if (dto is null)
            return ServiceResult<DepartmentDto?>.NotFound("Department not found");

        return ServiceResult<DepartmentDto?>.Ok(dto);
    }

    public async Task<ServiceResult<DepartmentDto>> CreateAsync(Department department)
    {
        var validationError = await ValidateAsync(department, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating department: {Error}", validationError);
            return ServiceResult<DepartmentDto>.Failed(validationError);
        }

        _dbContext.Department.Add(department);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Department created with Id {DepartmentId}", department.Id);

        // Reload with Manager included so ManagerName is populated on the DTO
        var created = await _dbContext.Department
            .Include(d => d.Manager)
            .AsNoTracking()
            .FirstAsync(d => d.Id == department.Id);

        return ServiceResult<DepartmentDto>.Ok(convertToDto(new List<Department> { created }).First());
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, Department department)
    {
        if (department is null)
            return ServiceResult<bool>.Failed("Department payload is required");

        if (id != department.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.Department.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Department not found");

        var validationError = await ValidateAsync(department, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating department {DepartmentId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        // Update only the mutable fields rather than blindly overwriting audit/soft-delete fields
        existing.Name = department.Name;
        existing.ManagerId = department.ManagerId;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating department with Id {DepartmentId}", id);
            throw;
        }

        _logger.LogInformation("Department with Id {DepartmentId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var department = await _dbContext.Department.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
        if (department is null)
            return ServiceResult<bool>.NotFound("Department not found");

        // Guard against deleting a department that still has employees assigned to it,
        // if Employee has a DepartmentId FK — remove this block if that's not the case.
        var hasEmployees = await _dbContext.Employee
            .AnyAsync(e => !e.IsDeleted && e.DepartmentId == id);

        if (hasEmployees)
            return ServiceResult<bool>.Failed("Cannot delete a department that still has employees assigned to it");

        department.IsDeleted = true;
        department.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Department with Id {DepartmentId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}