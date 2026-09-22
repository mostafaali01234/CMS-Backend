using CMS_Backend.Data;
using CMS_Backend.Models;
using CMS_Backend.Models.DTOs.Responses;
using CMS_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CMS_Backend.Services;

public class ProductCategoryService : IProductCategoryService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ProductCategoryService> _logger;

    private const int NameMaxLength = 100;

    public ProductCategoryService(
        AppDbContext dbContext,
        ILogger<ProductCategoryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // Centralized validation used by both Create and Update.
    // isUpdate/currentId let the uniqueness check exclude the record being updated.
    private async Task<string?> ValidateAsync(ProductCategory cat, bool isUpdate, long? currentId = null)
    {
        if (cat is null)
            return "ProductCategory payload is required";

        if (string.IsNullOrWhiteSpace(cat.Name))
            return "ProductCategory name is required";

        if (cat.Name.Trim().Length > NameMaxLength)
            return $"ProductCategory name cannot exceed {NameMaxLength} characters";

        // Normalize whitespace so "Manager " and "Manager" aren't treated as different names
        var normalizedName = cat.Name.Trim();
        cat.Name = normalizedName;

        var duplicateQuery = _dbContext.ProductCategory
            .Where(j => !j.IsDeleted && j.Name.ToLower() == normalizedName.ToLower());

        if (isUpdate && currentId.HasValue)
            duplicateQuery = duplicateQuery.Where(j => j.Id != currentId.Value);

        var duplicateExists = await duplicateQuery.AnyAsync();
        if (duplicateExists)
            return $"A ProductCategory named '{normalizedName}' already exists";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<ProductCategory>>> GetAllAsync()
    {
        var cats = await _dbContext.ProductCategory.Where(z => !z.IsDeleted).AsNoTracking().ToListAsync();
        return ServiceResult<List<ProductCategory>>.Ok(cats);
    }

    public async Task<ServiceResult<ProductCategory?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<ProductCategory?>.Failed("A valid ProductCategory Id is required");

        var cat = await _dbContext.ProductCategory.Where(z => !z.IsDeleted && z.Id == id).AsNoTracking().FirstOrDefaultAsync();

        if (cat is null)
            return ServiceResult<ProductCategory?>.NotFound("A valid ProductCategory Id is required");

        return ServiceResult<ProductCategory?>.Ok(cat);
    }

    public async Task<ServiceResult<ProductCategory>> CreateAsync(ProductCategory cat)
    {
        var validationError = await ValidateAsync(cat, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating ProductCategory: {Error}", validationError);
            return ServiceResult<ProductCategory>.Failed(validationError);
        }

        _dbContext.ProductCategory.Add(cat);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("ProductCategory created with Id {ProductCategoryId}", cat.Id);
        return ServiceResult<ProductCategory>.Ok(cat);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, ProductCategory cat)
    {
        if (cat is null)
            return ServiceResult<bool>.Failed("ProductCategory payload is required");

        if (id != cat.Id)
            return ServiceResult<bool>.Conflict("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.ProductCategory.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("ProductCategory not found");

        var validationError = await ValidateAsync(cat, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating ProductCategory {ProductCategoryId}: {Error}", id, validationError);
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
            _logger.LogError(ex, "Concurrency error occurred while updating ProductCategory with Id {ProductCategoryId}", id);
            throw;
        }

        _logger.LogInformation("ProductCategory with Id {ProductCategoryId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var cat = await _dbContext.ProductCategory.FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);
        if (cat is null)
            return ServiceResult<bool>.NotFound("ProductCategory not found");

        // Guard against deleting a product that's still assigned to products,
        // if Product has a ProductId FK — remove this block if that's not the case.
        var isInUse = await _dbContext.Product
            .AnyAsync(e => !e.IsDeleted && e.CategoryId == id);

        if (isInUse)
            return ServiceResult<bool>.Conflict("Cannot delete a category that is still assigned to products");

        cat.IsDeleted = true;
        cat.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("ProductCategory with Id {ProductCategoryId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}