// Services/ProductUnitCommissionService.cs
using CMS_Backend.Data;
using CMS_Backend.Models;
using CMS_Backend.Models.DTOs.Responses;
using CMS_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CMS_Backend.Services;

public class ProductUnitCommissionService : IProductUnitCommissionService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ProductUnitCommissionService> _logger;

    // Commission percentages assumed to be stored as 0-100, not 0-1. Adjust if your convention differs.
    private const decimal MinPercent = 0;
    private const decimal MaxPercent = 100;

    public ProductUnitCommissionService(
        AppDbContext dbContext,
        ILogger<ProductUnitCommissionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private static bool IsValidPercent(decimal value) => value >= MinPercent && value <= MaxPercent;

    // Centralized validation used by both Create and Update.
    // isUpdate/currentId let the (ProductId, UnitId) uniqueness check exclude the record being updated.
    private async Task<string?> ValidateAsync(ProductUnitCommission commission, bool isUpdate, long? currentId = null)
    {
        if (commission is null)
            return "Commission payload is required";

        // --- Product (required FK) ---
        if (commission.ProductId <= 0)
            return "ProductId is required";

        var productExists = await _dbContext.Product
            .AnyAsync(p => p.Id == commission.ProductId && !p.IsDeleted);

        if (!productExists)
            return $"Product with Id {commission.ProductId} was not found";

        // --- Unit (required FK) ---
        if (commission.UnitId <= 0)
            return "UnitId is required";

        var unitExists = await _dbContext.ProductUnit
            .AnyAsync(u => u.Id == commission.UnitId);

        if (!unitExists)
            return $"Unit with Id {commission.UnitId} was not found";

        //// --- Uniqueness: one commission record per (ProductId, UnitId) pair ---
        //var duplicateQuery = _dbContext.ProductUnitCommission
        //    .Where(c => c.ProductId == commission.ProductId && c.UnitId == commission.UnitId);

        //if (isUpdate && currentId.HasValue)
        //    duplicateQuery = duplicateQuery.Where(c => c.Id != currentId.Value);

        //if (await duplicateQuery.AnyAsync())
        //    return $"A commission record for Product {commission.ProductId} / Unit {commission.UnitId} already exists";

        // --- Commission percentages (0–100) ---
        if (!IsValidPercent(commission.SalesWholesaleCommPercent))
            return "SalesWholesaleCommPercent must be between 0 and 100";

        if (!IsValidPercent(commission.SalesHalfWholesaleCommPercent))
            return "SalesHalfWholesaleCommPercent must be between 0 and 100";

        if (!IsValidPercent(commission.SalesRetailCommPercent))
            return "SalesRetailCommPercent must be between 0 and 100";

        // --- Flat commission amounts (assumed non-negative currency values, not percentages) ---
        if (commission.TechComm < 0)
            return "TechComm cannot be negative";

        if (commission.Assistant1Comm < 0)
            return "Assistant1Comm cannot be negative";

        if (commission.Assistant2Comm < 0)
            return "Assistant2Comm cannot be negative";

        if (commission.Assistant3Comm < 0)
            return "Assistant3Comm cannot be negative";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<ProductUnitCommission>>> GetAllAsync()
    {
        var records = await _dbContext.ProductUnitCommission
            //.Include(c => c.Product)
            //.Include(c => c.Unit)
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<ProductUnitCommission>>.Ok(records);
    }

    public async Task<ServiceResult<List<ProductUnitCommission>>> GetByProductIdAsync(long productId)
    {
        var records = await _dbContext.ProductUnitCommission
            //.Include(c => c.Product)
            //.Include(c => c.Unit)
            .Where(z => z.ProductId == productId)
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<ProductUnitCommission>>.Ok(records);
    }

    public async Task<ServiceResult<ProductUnitCommission?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<ProductUnitCommission?>.Failed("A valid Id is required");

        var record = await _dbContext.ProductUnitCommission
            //.Include(c => c.Product)
            //.Include(c => c.Unit)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (record is null)
            return ServiceResult<ProductUnitCommission?>.NotFound("Commission record not found");

        return ServiceResult<ProductUnitCommission?>.Ok(record);
    }
    
    public async Task<ServiceResult<bool>> CreateForProductAsync(List<ProductUnitCommission> commList)
    {
        if (commList == null || !commList.Any())
            return ServiceResult<bool>.Failed("List has no items");

        foreach (var commission in commList)
        {
            var validationError = await ValidateAsync(commission, isUpdate: false);
            if (validationError is not null)
            {
                _logger.LogWarning("Validation failed while creating commission record: {Error}", validationError);
                return ServiceResult<bool>.Failed(validationError);
            }
        }

        var productId = commList[0].ProductId;
        var oldRecords = await _dbContext.ProductUnitCommission
            .Where(c => c.ProductId == productId)
            .ToListAsync();

        _dbContext.ProductUnitCommission.RemoveRange(oldRecords);
        _dbContext.ProductUnitCommission.AddRange(commList);

        await _dbContext.SaveChangesAsync();

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<ProductUnitCommission>> CreateAsync(ProductUnitCommission commission)
    {
        var validationError = await ValidateAsync(commission, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating commission record: {Error}", validationError);
            return ServiceResult<ProductUnitCommission>.Failed(validationError);
        }

        _dbContext.ProductUnitCommission.Add(commission);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Commission record created with Id {CommissionId} for Product {ProductId} / Unit {UnitId}",
            commission.Id, commission.ProductId, commission.UnitId);

        return ServiceResult<ProductUnitCommission>.Ok(commission);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, ProductUnitCommission commission)
    {
        if (commission is null)
            return ServiceResult<bool>.Failed("Commission payload is required");

        if (id != commission.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.ProductUnitCommission.FirstOrDefaultAsync(c => c.Id == id);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Commission record not found");

        var validationError = await ValidateAsync(commission, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating commission record {CommissionId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        existing.ProductId = commission.ProductId;
        existing.UnitId = commission.UnitId;
        existing.SalesWholesaleCommPercent = commission.SalesWholesaleCommPercent;
        existing.SalesHalfWholesaleCommPercent = commission.SalesHalfWholesaleCommPercent;
        existing.SalesRetailCommPercent = commission.SalesRetailCommPercent;
        existing.TechComm = commission.TechComm;
        existing.Assistant1Comm = commission.Assistant1Comm;
        existing.Assistant2Comm = commission.Assistant2Comm;
        existing.Assistant3Comm = commission.Assistant3Comm;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating commission record with Id {CommissionId}", id);
            throw;
        }

        _logger.LogInformation("Commission record with Id {CommissionId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var record = await _dbContext.ProductUnitCommission.FirstOrDefaultAsync(c => c.Id == id);
        if (record is null)
            return ServiceResult<bool>.NotFound("Commission record not found");

        // Hard delete — this entity has no IsDeleted/DeletedAtUtc, unlike Department/Job/Employee/Product.
        _dbContext.ProductUnitCommission.Remove(record);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Commission record with Id {CommissionId} deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}