// Services/EmployeeSpecialCommissionDefinitionService.cs
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using CMS.Domain.DTOs;
using CMS.Domain.Models;
using CMS.Application.Interfaces.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMS.Application.Services;

public class EmployeeSpecialCommissionDefinitionService : IEmployeeSpecialCommissionDefinitionService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<EmployeeSpecialCommissionDefinitionService> _logger;

    public EmployeeSpecialCommissionDefinitionService(
        IAppDbContext dbContext,
        ILogger<EmployeeSpecialCommissionDefinitionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private EmployeeSpecialCommissionDefinitionDto ToDto(EmployeeSpecialCommissionDefinition d)
    {
        return new EmployeeSpecialCommissionDefinitionDto
        {
            Id = d.Id,
            ManagerId = d.ManagerId,
            ManagerName = d.Manager?.Name ?? "",
            CommissionRate = d.CommissionRate,
            EmployeeIds = d.Employees.Select(e => e.Id).ToList(),
            Employees = d.Employees.Select(e => new SpecialCommissionEmployeeDto
            {
                Id = e.Id,
                Name = e.Name
            }).ToList(),
            ProductIds = d.Products.Select(p => p.Id).ToList(),
            Products = d.Products.Select(p => new SpecialCommissionProductDto
            {
                Id = p.Id,
                Name = p.Name
            }).ToList(),
            CreatedAtUtc = d.CreatedAtUtc,
            CreatedBy = d.CreatedBy ?? "",
            UpdatedAtUtc = d.UpdatedAtUtc,
            UpdatedBy = d.UpdatedBy ?? ""
        };
    }

    private IQueryable<EmployeeSpecialCommissionDefinition> WithIncludes(IQueryable<EmployeeSpecialCommissionDefinition> query)
    {
        return query
            .Include(d => d.Manager)
            .Include(d => d.Employees)
            .Include(d => d.Products);
    }

    // Centralized validation used by both Create and Update.
    private async Task<string?> ValidateAsync(EmployeeSpecialCommissionDefinitionDto dto)
    {
        if (dto is null)
            return "Special commission definition payload is required";

        // --- Manager (required FK) ---
        if (dto.ManagerId <= 0)
            return "ManagerId is required";

        var managerExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == dto.ManagerId && !e.IsDeleted);

        if (!managerExists)
            return $"Manager (Employee) with Id {dto.ManagerId} was not found";

        // --- CommissionRate ---
        if (dto.CommissionRate <= 0)
            return "CommissionRate must be greater than zero";

        if (dto.CommissionRate > 100)
            return "CommissionRate cannot exceed 100";

        // --- EmployeeIds (at least one required) ---
        if (dto.EmployeeIds is null || dto.EmployeeIds.Count == 0)
            return "At least one employee must be assigned to this commission definition";

        var distinctEmployeeIds = dto.EmployeeIds.Distinct().ToList();
        if (distinctEmployeeIds.Count != dto.EmployeeIds.Count)
            return "EmployeeIds contains duplicate values";

        if (distinctEmployeeIds.Contains(dto.ManagerId))
            return "The manager cannot also be listed as one of the assigned employees";

        var validEmployeeCount = await _dbContext.Employee
            .CountAsync(e => distinctEmployeeIds.Contains(e.Id) && !e.IsDeleted);

        if (validEmployeeCount != distinctEmployeeIds.Count)
            return "One or more EmployeeIds were not found";

        // --- ProductIds (at least one required) ---
        if (dto.ProductIds is null || dto.ProductIds.Count == 0)
            return "At least one product must be assigned to this commission definition";

        var distinctProductIds = dto.ProductIds.Distinct().ToList();
        if (distinctProductIds.Count != dto.ProductIds.Count)
            return "ProductIds contains duplicate values";

        var validProductCount = await _dbContext.Product
            .CountAsync(p => distinctProductIds.Contains(p.Id) && !p.IsDeleted);

        if (validProductCount != distinctProductIds.Count)
            return "One or more ProductIds were not found";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<EmployeeSpecialCommissionDefinitionDto>>> GetAllAsync()
    {
        var items = await WithIncludes(_dbContext.EmployeeSpecialCommissionDefinition.Where(z => !z.IsDeleted))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<EmployeeSpecialCommissionDefinitionDto>>.Ok(items.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<EmployeeSpecialCommissionDefinitionDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<EmployeeSpecialCommissionDefinitionDto?>.Failed("A valid Id is required");

        var item = await WithIncludes(_dbContext.EmployeeSpecialCommissionDefinition.Where(z => !z.IsDeleted && z.Id == id))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (item is null)
            return ServiceResult<EmployeeSpecialCommissionDefinitionDto?>.NotFound("Special commission definition not found");

        return ServiceResult<EmployeeSpecialCommissionDefinitionDto?>.Ok(ToDto(item));
    }

    public async Task<ServiceResult<EmployeeSpecialCommissionDefinitionDto>> CreateAsync(EmployeeSpecialCommissionDefinitionDto dto)
    {
        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating special commission definition: {Error}", validationError);
            return ServiceResult<EmployeeSpecialCommissionDefinitionDto>.Failed(validationError);
        }

        var employees = await _dbContext.Employee
            .Where(e => dto.EmployeeIds.Contains(e.Id))
            .ToListAsync();

        var products = await _dbContext.Product
            .Where(p => dto.ProductIds.Contains(p.Id))
            .ToListAsync();

        var entity = new EmployeeSpecialCommissionDefinition
        {
            ManagerId = dto.ManagerId,
            CommissionRate = dto.CommissionRate,
            Employees = employees,
            Products = products
        };

        _dbContext.EmployeeSpecialCommissionDefinition.Add(entity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Special commission definition created with Id {DefinitionId} for Manager {ManagerId}",
            entity.Id, entity.ManagerId);

        var created = await WithIncludes(_dbContext.EmployeeSpecialCommissionDefinition)
            .AsNoTracking()
            .FirstAsync(d => d.Id == entity.Id);

        return ServiceResult<EmployeeSpecialCommissionDefinitionDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, EmployeeSpecialCommissionDefinitionDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Special commission definition payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.EmployeeSpecialCommissionDefinition
            .Include(d => d.Employees)
            .Include(d => d.Products)
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (existing is null)
            return ServiceResult<bool>.NotFound("Special commission definition not found");

        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating special commission definition {DefinitionId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        existing.ManagerId = dto.ManagerId;
        existing.CommissionRate = dto.CommissionRate;

        // Replace both many-to-many sets with the new selection
        var employees = await _dbContext.Employee
            .Where(e => dto.EmployeeIds.Contains(e.Id))
            .ToListAsync();

        var products = await _dbContext.Product
            .Where(p => dto.ProductIds.Contains(p.Id))
            .ToListAsync();

        existing.Employees.Clear();
        foreach (var employee in employees)
            existing.Employees.Add(employee);

        existing.Products.Clear();
        foreach (var product in products)
            existing.Products.Add(product);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating special commission definition with Id {DefinitionId}", id);
            throw;
        }

        _logger.LogInformation("Special commission definition with Id {DefinitionId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var entity = await _dbContext.EmployeeSpecialCommissionDefinition.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
        if (entity is null)
            return ServiceResult<bool>.NotFound("Special commission definition not found");

        entity.IsDeleted = true;
        entity.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Special commission definition with Id {DefinitionId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}