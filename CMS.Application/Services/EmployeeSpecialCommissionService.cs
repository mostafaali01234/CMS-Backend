// Services/EmployeeSpecialCommissionService.cs
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using CMS.Domain.DTOs;
using CMS.Domain.Models;
using CMS.Application.Interfaces.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMS.Application.Services;

public class EmployeeSpecialCommissionService : IEmployeeSpecialCommissionService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<EmployeeSpecialCommissionService> _logger;

    private const int NotesMaxLength = 2000;

    public EmployeeSpecialCommissionService(
        IAppDbContext dbContext,
        ILogger<EmployeeSpecialCommissionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private EmployeeSpecialCommissionDto ToDto(EmployeeSpecialCommission c)
    {
        return new EmployeeSpecialCommissionDto
        {
            Id = c.Id,
            ManagerId = c.ManagerId,
            ManagerName = c.Manager?.Name ?? "",
            EmployeeId = c.EmployeeId,
            EmployeeName = c.Employee?.Name ?? "",
            Month = c.Month,
            Year = c.Year,
            SalesTotal = c.SalesTotal,
            CommissionRate = c.CommissionRate,
            CommissionTotal = c.CommissionTotal,
            Notes = c.Notes,
            CreatedAtUtc = c.CreatedAtUtc,
            CreatedBy = c.CreatedBy ?? "",
            UpdatedAtUtc = c.UpdatedAtUtc,
            UpdatedBy = c.UpdatedBy ?? ""
        };
    }

    private IQueryable<EmployeeSpecialCommission> WithIncludes(IQueryable<EmployeeSpecialCommission> query)
    {
        return query
            .Include(c => c.Manager)
            .Include(c => c.Employee);
    }

    // Centralized validation used by both Create and Update.
    private async Task<string?> ValidateAsync(EmployeeSpecialCommissionDto dto, bool isUpdate, long? currentId = null)
    {
        if (dto is null)
            return "Special commission payload is required";

        // --- Manager (required FK) ---
        if (dto.ManagerId <= 0)
            return "ManagerId is required";

        var managerExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == dto.ManagerId && !e.IsDeleted);

        if (!managerExists)
            return $"Manager (Employee) with Id {dto.ManagerId} was not found";

        // --- Employee (required FK) ---
        if (dto.EmployeeId <= 0)
            return "EmployeeId is required";

        var employeeExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == dto.EmployeeId && !e.IsDeleted);

        if (!employeeExists)
            return $"Employee with Id {dto.EmployeeId} was not found";

        if (dto.EmployeeId == dto.ManagerId)
            return "ManagerId and EmployeeId cannot be the same employee";

        // --- Month / Year ---
        if (dto.Month < 1 || dto.Month > 12)
            return "Month must be between 1 and 12";

        if (dto.Year < 2000 || dto.Year > DateTime.UtcNow.Year + 1)
            return $"Year must be between 2000 and {DateTime.UtcNow.Year + 1}";

        // --- Uniqueness: one record per (Manager, Employee, Month, Year) ---
        var periodQuery = _dbContext.EmployeeSpecialCommission.Where(c =>
            !c.IsDeleted &&
            c.ManagerId == dto.ManagerId &&
            c.EmployeeId == dto.EmployeeId &&
            c.Month == dto.Month &&
            c.Year == dto.Year);

        if (isUpdate && currentId.HasValue)
            periodQuery = periodQuery.Where(c => c.Id != currentId.Value);

        if (await periodQuery.AnyAsync())
            return $"A special commission record for employee {dto.EmployeeId} under manager {dto.ManagerId} for {dto.Month}/{dto.Year} already exists";

        // --- SalesTotal / CommissionRate ---
        if (dto.SalesTotal < 0)
            return "SalesTotal cannot be negative";

        if (dto.CommissionRate < 0)
            return "CommissionRate cannot be negative";

        if (dto.CommissionRate > 100)
            return "CommissionRate cannot exceed 100";

        // --- Notes (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Length > NotesMaxLength)
            return $"Notes cannot exceed {NotesMaxLength} characters";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<EmployeeSpecialCommissionDto>>> GetAllAsync()
    {
        var items = await WithIncludes(_dbContext.EmployeeSpecialCommission.Where(z => !z.IsDeleted))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<EmployeeSpecialCommissionDto>>.Ok(items.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<EmployeeSpecialCommissionDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<EmployeeSpecialCommissionDto?>.Failed("A valid Id is required");

        var item = await WithIncludes(_dbContext.EmployeeSpecialCommission.Where(z => !z.IsDeleted && z.Id == id))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (item is null)
            return ServiceResult<EmployeeSpecialCommissionDto?>.NotFound("Special commission not found");

        return ServiceResult<EmployeeSpecialCommissionDto?>.Ok(ToDto(item));
    }

    public async Task<ServiceResult<List<EmployeeSpecialCommissionDto>>> GetByEmployeeIdAsync(long employeeId, int year, int month)
    {
        var items = await WithIncludes(_dbContext.EmployeeSpecialCommission
            .Where(z => !z.IsDeleted && z.ManagerId == employeeId && z.Month == month && z.Year == year))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<EmployeeSpecialCommissionDto>>.Ok(items.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<EmployeeSpecialCommissionDto>> CreateAsync(EmployeeSpecialCommissionDto dto)
    {
        var validationError = await ValidateAsync(dto, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating special commission: {Error}", validationError);
            return ServiceResult<EmployeeSpecialCommissionDto>.Failed(validationError);
        }

        dto.CommissionTotal = Math.Round(dto.SalesTotal * dto.CommissionRate / 100m, 2);

        var entity = new EmployeeSpecialCommission
        {
            ManagerId = dto.ManagerId,
            EmployeeId = dto.EmployeeId,
            Month = dto.Month,
            Year = dto.Year,
            SalesTotal = dto.SalesTotal,
            CommissionRate = dto.CommissionRate,
            CommissionTotal = dto.CommissionTotal,
            Notes = dto.Notes
        };

        _dbContext.EmployeeSpecialCommission.Add(entity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Special commission created with Id {CommissionId} for Employee {EmployeeId} ({Month}/{Year})",
            entity.Id, entity.EmployeeId, entity.Month, entity.Year);

        var created = await WithIncludes(_dbContext.EmployeeSpecialCommission)
            .AsNoTracking()
            .FirstAsync(c => c.Id == entity.Id);

        return ServiceResult<EmployeeSpecialCommissionDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, EmployeeSpecialCommissionDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Special commission payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.EmployeeSpecialCommission.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Special commission not found");

        var validationError = await ValidateAsync(dto, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating special commission {CommissionId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        dto.CommissionTotal = Math.Round(dto.SalesTotal * dto.CommissionRate / 100m, 2);

        existing.ManagerId = dto.ManagerId;
        existing.EmployeeId = dto.EmployeeId;
        existing.Month = dto.Month;
        existing.Year = dto.Year;
        existing.SalesTotal = dto.SalesTotal;
        existing.CommissionRate = dto.CommissionRate;
        existing.CommissionTotal = dto.CommissionTotal;
        existing.Notes = dto.Notes;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating special commission with Id {CommissionId}", id);
            throw;
        }

        _logger.LogInformation("Special commission with Id {CommissionId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var entity = await _dbContext.EmployeeSpecialCommission.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (entity is null)
            return ServiceResult<bool>.NotFound("Special commission not found");

        entity.IsDeleted = true;
        entity.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Special commission with Id {CommissionId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}