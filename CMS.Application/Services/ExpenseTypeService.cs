using CMS.Application.Interfaces.Configuration;
using CMS.Domain.Models;
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Extensions.Logging;
namespace CMS.Application.Services;

public class ExpenseTypeService : IExpenseTypeService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<ExpenseTypeService> _logger;

    private const int NameMaxLength = 100;

    public ExpenseTypeService(
        IAppDbContext dbContext,
        ILogger<ExpenseTypeService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // Centralized validation used by both Create and Update.
    // isUpdate/currentId let the uniqueness check exclude the record being updated.
    private async Task<string?> ValidateAsync(ExpenseType type, bool isUpdate, long? currentId = null)
    {
        if (type is null)
            return "ExpenseTypeService payload is required";

        if (string.IsNullOrWhiteSpace(type.Name))
            return "ExpenseTypeService name is required";

        if (type.Name.Trim().Length > NameMaxLength)
            return $"ExpenseType name cannot exceed {NameMaxLength} characters";

        // Normalize whitespace so "Sales " and "Sales" aren't treated as different names
        var normalizedName = type.Name.Trim();
        type.Name = normalizedName;

        var duplicateQuery = _dbContext.ExpenseType
            .Where(d => d.Name.ToLower() == normalizedName.ToLower());

        if (isUpdate && currentId.HasValue)
            duplicateQuery = duplicateQuery.Where(d => d.Id != currentId.Value);

        var duplicateExists = await duplicateQuery.AnyAsync();
        if (duplicateExists)
            return $"A ExpenseType named '{normalizedName}' already exists";

        if (type.CategoryId <= 0)
            return "CategoryId is required";

        var stateExists = await _dbContext.CountryState
            .AnyAsync(e => e.Id == type.CategoryId);

        if (!stateExists)
            return $"ExpenseCategory with Id {type.CategoryId} was not found";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<ExpenseType>>> GetAllAsync()
    {
        var types = await _dbContext.ExpenseType.AsNoTracking().ToListAsync();
        return ServiceResult<List<ExpenseType>>.Ok(types);
    }

    public async Task<ServiceResult<ExpenseType?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<ExpenseType?>.Failed("A valid ExpenseType Id is required");

        var type = await _dbContext.ExpenseType.Where(z => z.Id == id).AsNoTracking().FirstOrDefaultAsync();

        if (type is null)
            return ServiceResult<ExpenseType?>.NotFound("ExpenseType not found");

        return ServiceResult<ExpenseType?>.Ok(type);
    }

    public async Task<ServiceResult<ExpenseType>> CreateAsync(ExpenseType type)
    {
        var validationError = await ValidateAsync(type, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating ExpenseType: {Error}", validationError);
            return ServiceResult<ExpenseType>.Failed(validationError);
        }

        _dbContext.ExpenseType.Add(type);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("ExpenseType created with Id {ExpenseTypeId}", type.Id);

        var created = await _dbContext.ExpenseType
            .AsNoTracking()
            .FirstAsync(d => d.Id == type.Id);

        return ServiceResult<ExpenseType>.Ok(created);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, ExpenseType type)
    {
        if (type is null)
            return ServiceResult<bool>.Failed("ExpenseType payload is required");

        if (id != type.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.ExpenseType.FirstOrDefaultAsync(p => p.Id == id);
        if (existing is null)
            return ServiceResult<bool>.NotFound("ExpenseType not found");

        var validationError = await ValidateAsync(type, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating ExpenseType {ExpenseTypeId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        // Update only the mutable fields rather than blindly overwriting audit/soft-delete fields
        existing.Name = type.Name;
        existing.CategoryId = type.CategoryId;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating ExpenseType with Id {ExpenseTypeId}", id);
            throw;
        }

        _logger.LogInformation("ExpenseType with Id {ExpenseTypeId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var type = await _dbContext.ExpenseType.FirstOrDefaultAsync(d => d.Id == id);
        if (type is null)
            return ServiceResult<bool>.NotFound("ExpenseType not found");

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("ExpenseType with Id {ExpenseTypeId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}