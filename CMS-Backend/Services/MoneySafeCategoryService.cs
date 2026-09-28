using CMS_Backend.Data;
using CMS_Backend.Models;
using CMS_Backend.Models.DTOs.Responses;
using CMS_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CMS_Backend.Services;

public class MoneySafeCategoryService : IMoneySafeCategoryService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<MoneySafeCategoryService> _logger;

    private const int NameMaxLength = 100;

    public MoneySafeCategoryService(
        AppDbContext dbContext,
        ILogger<MoneySafeCategoryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // Centralized validation used by both Create and Update.
    // isUpdate/currentId let the uniqueness check exclude the record being updated.
    private async Task<string?> ValidateAsync(MoneySafeCategory cat, bool isUpdate, long? currentId = null)
    {
        if (cat is null)
            return "Category payload is required";

        if (cat.OrderNumber == null)
            return "MoneySafeCategory OrderNumber is required";

        if (string.IsNullOrWhiteSpace(cat.Name))
            return "MoneySafeCategory name is required";

        if (cat.Name.Trim().Length > NameMaxLength)
            return $"MoneySafeCategory name cannot exceed {NameMaxLength} characters";

        // Normalize whitespace so "Manager " and "Manager" aren't treated as different names
        var normalizedName = cat.Name.Trim();
        cat.Name = normalizedName;

        var duplicateQuery = _dbContext.MoneySafeCategory
            .Where(j => j.Name.ToLower() == normalizedName.ToLower());

        if (isUpdate && currentId.HasValue)
            duplicateQuery = duplicateQuery.Where(j => j.Id != currentId.Value);

        var duplicateExists = await duplicateQuery.AnyAsync();
        if (duplicateExists)
            return $"A MoneySafeCategory named '{normalizedName}' already exists";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<MoneySafeCategory>>> GetAllAsync()
    {
        var cats = await _dbContext.MoneySafeCategory.AsNoTracking().ToListAsync();
        return ServiceResult<List<MoneySafeCategory>>.Ok(cats);
    }

    public async Task<ServiceResult<MoneySafeCategory?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<MoneySafeCategory?>.Failed("A valid MoneySafeCategory Id is required");

        var cat = await _dbContext.MoneySafeCategory.Where(z => z.Id == id).AsNoTracking().FirstOrDefaultAsync();

        if (cat is null)
            return ServiceResult<MoneySafeCategory?>.NotFound("MoneySafeCategory not found");

        return ServiceResult<MoneySafeCategory?>.Ok(cat);
    }

    public async Task<ServiceResult<MoneySafeCategory>> CreateAsync(MoneySafeCategory cat)
    {
        var validationError = await ValidateAsync(cat, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating MoneySafeCategory: {Error}", validationError);
            return ServiceResult<MoneySafeCategory>.Failed(validationError);
        }

        _dbContext.MoneySafeCategory.Add(cat);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("MoneySafeCategory created with Id {MoneySafeCategoryId}", cat.Id);
        return ServiceResult<MoneySafeCategory>.Ok(cat);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, MoneySafeCategory cat)
    {
        if (cat is null)
            return ServiceResult<bool>.Failed("MoneySafeCategory payload is required");

        if (id != cat.Id)
            return ServiceResult<bool>.Conflict("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.MoneySafeCategory.FirstOrDefaultAsync(p => p.Id == id);
        if (existing is null)
            return ServiceResult<bool>.NotFound("MoneySafeCategory not found");

        var validationError = await ValidateAsync(cat, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating MoneySafeCategory {MoneySafeCategoryId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        // Update only the mutable field rather than blindly overwriting audit/soft-delete fields
        existing.Name = cat.Name;
        existing.OrderNumber = cat.OrderNumber;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating MoneySafeCategory with Id {MoneySafeCategoryId}", id);
            throw;
        }

        _logger.LogInformation("MoneySafeCategory with Id {MoneySafeCategoryId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var cat = await _dbContext.MoneySafeCategory.FirstOrDefaultAsync(j => j.Id == id);
        if (cat is null)
            return ServiceResult<bool>.NotFound("MoneySafeCategory not found");

        // Guard against deleting a MoneySafeCategory that's still assigned to city,
        // if City has a MoneySafeCategoryId FK — remove this block if that's not the case.
        var isInUse = await _dbContext.MoneySafe
            .AnyAsync(e => e.CategoryId == id && !e.IsDeleted);

        if (isInUse)
            return ServiceResult<bool>.Conflict("Cannot delete a MoneySafeCategory that is still assigned to cities");


        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("MoneySafeCategory with Id {MoneySafeCategoryId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}