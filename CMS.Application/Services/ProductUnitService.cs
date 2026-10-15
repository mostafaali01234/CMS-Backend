using CMS.Application.Interfaces.Configuration;
using CMS.Domain.Models;
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Extensions.Logging;

namespace CMS.Application.Services;

public class ProductUnitService : IProductUnitService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<ProductUnitService> _logger;

    private const int NameMaxLength = 100;

    public ProductUnitService(
        IAppDbContext dbContext,
        ILogger<ProductUnitService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // Centralized validation used by both Create and Update.
    // isUpdate/currentId let the uniqueness check exclude the record being updated.
    private async Task<string?> ValidateAsync(ProductUnit unit, bool isUpdate, long? currentId = null)
    {
        if (unit is null)
            return "ProductUnit payload is required";

        if (string.IsNullOrWhiteSpace(unit.Name))
            return "ProductUnit name is required";

        if (unit.Name.Trim().Length > NameMaxLength)
            return $"ProductUnit name cannot exceed {NameMaxLength} characters";

        // Normalize whitespace so "Manager " and "Manager" aren't treated as different names
        var normalizedName = unit.Name.Trim();
        unit.Name = normalizedName;

        var duplicateQuery = _dbContext.ProductUnit
            .Where(j => j.Name.ToLower() == normalizedName.ToLower());

        if (isUpdate && currentId.HasValue)
            duplicateQuery = duplicateQuery.Where(j => j.Id != currentId.Value);

        var duplicateExists = await duplicateQuery.AnyAsync();
        if (duplicateExists)
            return $"A ProductUnit named '{normalizedName}' already exists";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<ProductUnit>>> GetAllAsync()
    {
        var cats = await _dbContext.ProductUnit.AsNoTracking().ToListAsync();
        return ServiceResult<List<ProductUnit>>.Ok(cats);
    }

    public async Task<ServiceResult<ProductUnit?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<ProductUnit?>.Failed("A valid ProductUnit Id is required");

        var cat = await _dbContext.ProductUnit.Where(z => z.Id == id).AsNoTracking().FirstOrDefaultAsync();

        if (cat is null)
            return ServiceResult<ProductUnit?>.NotFound("A valid ProductUnit Id is required");

        return ServiceResult<ProductUnit?>.Ok(cat);
    }

    public async Task<ServiceResult<ProductUnit>> CreateAsync(ProductUnit unit)
    {
        var validationError = await ValidateAsync(unit, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating ProductUnit: {Error}", validationError);
            return ServiceResult<ProductUnit>.Failed(validationError);
        }

        _dbContext.ProductUnit.Add(unit);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("ProductUnit created with Id {ProductUnitId}", unit.Id);
        return ServiceResult<ProductUnit>.Ok(unit);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, ProductUnit cat)
    {
        if (cat is null)
            return ServiceResult<bool>.Failed("ProductUnit payload is required");

        if (id != cat.Id)
            return ServiceResult<bool>.Conflict("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.ProductUnit.FirstOrDefaultAsync(p => p.Id == id);
        if (existing is null)
            return ServiceResult<bool>.NotFound("ProductUnit not found");

        var validationError = await ValidateAsync(cat, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating ProductUnit {ProductUnitId}: {Error}", id, validationError);
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
            _logger.LogError(ex, "Concurrency error occurred while updating ProductUnit with Id {ProductUnitId}", id);
            throw;
        }

        _logger.LogInformation("ProductUnit with Id {ProductUnitId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var unit = await _dbContext.ProductUnit.FirstOrDefaultAsync(j => j.Id == id);
        if (unit is null)
            return ServiceResult<bool>.NotFound("ProductUnit not found");

        // Guard against deleting a product that's still assigned to products,
        // if Product has a ProductId FK — remove this block if that's not the case.
        var isInUse = await _dbContext.ProductUnitCommission
            .AnyAsync(e => e.UnitId == id);

        if (isInUse)
            return ServiceResult<bool>.Conflict("Cannot delete a commission that is still assigned to products");

        _dbContext.ProductUnit.Remove(unit);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("ProductUnit with Id {ProductUnitId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}