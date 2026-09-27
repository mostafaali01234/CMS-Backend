// Services/ProductAssemblyDefinitionService.cs
using CMS_Backend.Data;
using CMS_Backend.Models;
using CMS_Backend.Models.DTOs;
using CMS_Backend.Models.DTOs.Responses;
using CMS_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CMS_Backend.Services;

public class ProductAssemblyDefinitionService : IProductAssemblyDefinitionService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ProductAssemblyDefinitionService> _logger;

    public ProductAssemblyDefinitionService(
        AppDbContext dbContext,
        ILogger<ProductAssemblyDefinitionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private ProductAssemblyDefinitionDto ToDto(long outProductId, string outProductName, List<ProductAssemblyDefinition> rows)
    {
        var first = rows.First();

        return new ProductAssemblyDefinitionDto
        {
            OutProductId = outProductId,
            OutProductName = outProductName,
            InProductList = rows.Select(r => new AssemblyDefinitionInProduct
            {
                InProductId = r.InProductId,
                InProductName = r.InProduct?.Name ?? "",
                InProductQuantity = r.InQuantity
            }).ToList(),
            CreatedAtUtc = first.CreatedAtUtc,
            CreatedBy = first.CreatedBy ?? "",
            UpdatedAtUtc = rows.Max(r => r.UpdatedAtUtc),
            UpdatedBy = rows.OrderByDescending(r => r.UpdatedAtUtc ?? r.CreatedAtUtc).First().UpdatedBy ?? ""
        };
    }

    // Centralized validation for both Create and Update.
    private async Task<string?> ValidateAsync(ProductAssemblyDefinitionDto dto)
    {
        if (dto is null)
            return "Assembly definition payload is required";

        // --- OutProduct (required FK) ---
        if (dto.OutProductId <= 0)
            return "OutProductId is required";

        var outProductExists = await _dbContext.Product
            .AnyAsync(p => p.Id == dto.OutProductId && !p.IsDeleted);

        if (!outProductExists)
            return $"Out product with Id {dto.OutProductId} was not found";

        // --- InProductList (at least one required) ---
        if (dto.InProductList is null || dto.InProductList.Count == 0)
            return "At least one input product is required";

        var seenInProductIds = new HashSet<long>();

        foreach (var item in dto.InProductList)
        {
            if (item.InProductId <= 0)
                return "Each input product must have a valid InProductId";

            if (item.InProductId == dto.OutProductId)
                return "A product cannot be an input to its own assembly (OutProductId cannot appear in InProductList)";

            if (!seenInProductIds.Add(item.InProductId))
                return $"Product Id {item.InProductId} appears more than once in the input product list";

            if (item.InProductQuantity <= 0)
                return $"Quantity for input product Id {item.InProductId} must be greater than zero";

            var inProductExists = await _dbContext.Product
                .AnyAsync(p => p.Id == item.InProductId && !p.IsDeleted);

            if (!inProductExists)
                return $"Input product with Id {item.InProductId} was not found";
        }

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<ProductAssemblyDefinitionDto>>> GetAllAsync()
    {
        var rows = await _dbContext.ProductAssemblyDefinition
            .Where(z => !z.IsDeleted)
            .Include(r => r.OutProduct)
            .Include(r => r.InProduct)
            .AsNoTracking()
            .ToListAsync();

        var dtos = rows
            .GroupBy(r => r.OutProductId)
            .Select(g => ToDto(g.Key, g.First().OutProduct?.Name ?? "", g.ToList()))
            .ToList();

        return ServiceResult<List<ProductAssemblyDefinitionDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<ProductAssemblyDefinitionDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<ProductAssemblyDefinitionDto?>.Failed("A valid OutProductId is required");

        var rows = await _dbContext.ProductAssemblyDefinition
            .Where(z => !z.IsDeleted && z.OutProductId == id)
            .Include(r => r.OutProduct)
            .Include(r => r.InProduct)
            .AsNoTracking()
            .ToListAsync();

        if (rows.Count == 0)
            return ServiceResult<ProductAssemblyDefinitionDto?>.NotFound("Assembly definition not found");

        return ServiceResult<ProductAssemblyDefinitionDto?>.Ok(ToDto(id, rows.First().OutProduct?.Name ?? "", rows));
    }

    public async Task<ServiceResult<ProductAssemblyDefinitionDto>> CreateAsync(ProductAssemblyDefinitionDto op)
    {
        var validationError = await ValidateAsync(op);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating assembly definition: {Error}", validationError);
            return ServiceResult<ProductAssemblyDefinitionDto>.Failed(validationError);
        }

        // An assembly definition for this OutProductId shouldn't already exist —
        // use UpdateAsync to modify an existing one instead of creating a duplicate group.
        var alreadyExists = await _dbContext.ProductAssemblyDefinition
            .AnyAsync(r => !r.IsDeleted && r.OutProductId == op.OutProductId);

        if (alreadyExists)
            return ServiceResult<ProductAssemblyDefinitionDto>.Failed(
                $"An assembly definition for product Id {op.OutProductId} already exists. Use update instead.");

        var rows = op.InProductList.Select(item => new ProductAssemblyDefinition
        {
            OutProductId = op.OutProductId,
            InProductId = item.InProductId,
            InQuantity = item.InProductQuantity
        }).ToList();

        await _dbContext.ProductAssemblyDefinition.AddRangeAsync(rows);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Assembly definition created for OutProductId {OutProductId} with {ItemCount} input products",
            op.OutProductId, rows.Count);

        var created = await _dbContext.ProductAssemblyDefinition
            .Where(r => r.OutProductId == op.OutProductId)
            .Include(r => r.OutProduct)
            .Include(r => r.InProduct)
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<ProductAssemblyDefinitionDto>.Ok(ToDto(op.OutProductId, created.First().OutProduct?.Name ?? "", created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, ProductAssemblyDefinitionDto cat)
    {
        if (cat is null)
            return ServiceResult<bool>.Failed("Assembly definition payload is required");

        if (id != cat.OutProductId)
            return ServiceResult<bool>.Failed("Id in the route does not match OutProductId in the payload");

        var existingRows = await _dbContext.ProductAssemblyDefinition
            .Where(r => r.OutProductId == id && !r.IsDeleted)
            .ToListAsync();

        if (existingRows.Count == 0)
            return ServiceResult<bool>.NotFound("Assembly definition not found");

        var validationError = await ValidateAsync(cat);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating assembly definition {OutProductId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        // Replace all input-product rows for this OutProductId with the new set
        _dbContext.ProductAssemblyDefinition.RemoveRange(existingRows);

        var newRows = cat.InProductList.Select(item => new ProductAssemblyDefinition
        {
            OutProductId = id,
            InProductId = item.InProductId,
            InQuantity = item.InProductQuantity
        }).ToList();

        await _dbContext.ProductAssemblyDefinition.AddRangeAsync(newRows);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating assembly definition for OutProductId {OutProductId}", id);
            throw;
        }

        _logger.LogInformation("Assembly definition for OutProductId {OutProductId} updated successfully ({ItemCount} input products)",
            id, newRows.Count);

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var rows = await _dbContext.ProductAssemblyDefinition
            .Where(r => r.OutProductId == id && !r.IsDeleted)
            .ToListAsync();

        if (rows.Count == 0)
            return ServiceResult<bool>.NotFound("Assembly definition not found");

        var now = DateTime.UtcNow;

        foreach (var row in rows)
        {
            row.IsDeleted = true;
            row.DeletedAtUtc = now;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Assembly definition for OutProductId {OutProductId} soft-deleted successfully ({ItemCount} rows)",
            id, rows.Count);

        return ServiceResult<bool>.Ok(true);
    }
}