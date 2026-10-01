
using CMS.Application.Interfaces.Configuration;
using CMS.Domain.Models;
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Extensions.Logging;

namespace CMS.Application.Services;

public class ExpenseCategoryService : IExpenseCategoryService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<ExpenseCategoryService> _logger;

    private const int NameMaxLength = 100;

    public ExpenseCategoryService(
        IAppDbContext dbContext,
        ILogger<ExpenseCategoryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // Centralized validation used by both Create and Update.
    // isUpdate/currentId let the uniqueness check exclude the record being updated.
    private async Task<string?> ValidateAsync(ExpenseCategory cat, bool isUpdate, long? currentId = null)
    {
        if (cat is null)
            return "ExpenseCategory payload is required";

        if (string.IsNullOrWhiteSpace(cat.Name))
            return "ExpenseCategory name is required";

        if (cat.Name.Trim().Length > NameMaxLength)
            return $"ExpenseCategory name cannot exceed {NameMaxLength} characters";

        // Normalize whitespace so "Manager " and "Manager" aren't treated as different names
        var normalizedName = cat.Name.Trim();
        cat.Name = normalizedName;

        var duplicateQuery = _dbContext.ExpenseCategory
            .Where(j => !j.IsDeleted && j.Name.ToLower() == normalizedName.ToLower());

        if (isUpdate && currentId.HasValue)
            duplicateQuery = duplicateQuery.Where(j => j.Id != currentId.Value);

        var duplicateExists = await duplicateQuery.AnyAsync();
        if (duplicateExists)
            return $"A ExpenseCategory named '{normalizedName}' already exists";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<ExpenseCategory>>> GetAllAsync()
    {
        var cats = await _dbContext.ExpenseCategory.Where(z => !z.IsDeleted).AsNoTracking().ToListAsync();
        return ServiceResult<List<ExpenseCategory>>.Ok(cats);
    }

    public async Task<ServiceResult<ExpenseCategory?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<ExpenseCategory?>.Failed("A valid ExpenseCategory Id is required");

        var cats = await _dbContext.ExpenseCategory.Where(z => !z.IsDeleted && z.Id == id).AsNoTracking().FirstOrDefaultAsync();

        if (cats is null)
            return ServiceResult<ExpenseCategory?>.NotFound("ExpenseCategory not found");

        return ServiceResult<ExpenseCategory?>.Ok(cats);
    }

    public async Task<ServiceResult<ExpenseCategory>> CreateAsync(ExpenseCategory cats)
    {
        var validationError = await ValidateAsync(cats, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating ExpenseCategory: {Error}", validationError);
            return ServiceResult<ExpenseCategory>.Failed(validationError);
        }

        _dbContext.ExpenseCategory.Add(cats);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("ExpenseCategory created with Id {ExpenseCategoryId}", cats.Id);
        return ServiceResult<ExpenseCategory>.Ok(cats);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, ExpenseCategory cat)
    {
        if (cat is null)
            return ServiceResult<bool>.Failed("ExpenseCategory payload is required");

        if (id != cat.Id)
            return ServiceResult<bool>.Conflict("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.ExpenseCategory.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("ExpenseCategory not found");

        var validationError = await ValidateAsync(cat, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating ExpenseCategory {ExpenseCategoryId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        // Update only the mutable field rather than blindly overwriting audit/soft-delete fields
        existing.Name = cat.Name;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating ExpenseCategory with Id {ExpenseCategoryId}", id);
            throw;
        }

        _logger.LogInformation("ExpenseCategory with Id {ExpenseCategoryId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var cat = await _dbContext.ExpenseCategory.FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);
        if (cat is null)
            return ServiceResult<bool>.NotFound("ExpenseCategory not found");

        // Guard against deleting a ExpenseCategory that's still assigned to type,
        // if type has a ExpenseCategoryId FK — remove this block if that's not the case.
        var isInUse = await _dbContext.ExpenseType
            .AnyAsync(e => !e.IsDeleted && e.CategoryId == id);

        if (isInUse)
            return ServiceResult<bool>.Conflict("Cannot delete a ExpenseCategory that is still assigned to ExpenseType");

        cat.IsDeleted = true;
        cat.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("ExpenseCategory with Id {ExpenseCategoryId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}