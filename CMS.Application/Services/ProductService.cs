using CMS.Application.Interfaces.Configuration;
using CMS.Domain.Models;
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;
using CMS.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CMS.Application.Services;

public class ProductService : IProductService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<ProductService> _logger;

    private const int NameMaxLength = 150;
    private const int BarcodeMaxLength = 50;
    private const int DescriptionMaxLength = 2000;
    private const decimal MinPercent = 0;
    private const decimal MaxPercent = 100;
    private static bool IsValidPercent(decimal v) => v >= MinPercent && v <= MaxPercent;

    public ProductService(
        IAppDbContext dbContext,
        ILogger<ProductService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private ProductDto ToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description ?? "",
            Barcode = product.Barcode,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? "",
            BuyPrice = product.BuyPrice,
            SalePrice = product.SalePrice,
            StorageType = product.StorageType,
            ProductType = product.ProductType,
            Active = product.Active,
            HasSerial = product.HasSerial,
            EnableCounter = product.EnableCounter,
            EnableAddOrder = product.EnableAddOrder,
            ArrangeOrder = product.ArrangeOrder,
            CreatedAtUtc = product.CreatedAtUtc,
            CreatedBy = product.CreatedBy ?? "",
            UpdatedAtUtc = product.UpdatedAtUtc,
            UpdatedBy = product.UpdatedBy ?? "",
            InspectionCommissionDeductionPercent = product.InspectionCommissionDeductionPercent
        };
    }

    private Product ToEntity(ProductDto dto)
    {
        return new Product
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            Barcode = dto.Barcode,
            CategoryId = dto.CategoryId,
            BuyPrice = dto.BuyPrice,
            SalePrice = dto.SalePrice,
            StorageType = dto.StorageType,
            ProductType = dto.ProductType,
            Active = dto.Active,
            HasSerial = dto.HasSerial,
            EnableCounter = dto.EnableCounter,
            EnableAddOrder = dto.EnableAddOrder,
            ArrangeOrder = dto.ArrangeOrder,
            InspectionCommissionDeductionPercent = dto.InspectionCommissionDeductionPercent
        };
    }

    private static ProductUnitCommission ToCommissionEntity(ProductUnitCommissionDto d) => new()
    {
        UnitId = d.UnitId,
        SalesWholesaleCommPercent = d.SalesWholesaleCommPercent,
        SalesHalfWholesaleCommPercent = d.SalesHalfWholesaleCommPercent,
        SalesRetailCommPercent = d.SalesRetailCommPercent,
        TechComm = d.TechComm,
        Assistant1Comm = d.Assistant1Comm,
        Assistant2Comm = d.Assistant2Comm,
        Assistant3Comm = d.Assistant3Comm,
    };

    private static ProductUnitCommissionDto ToCommissionDto(ProductUnitCommission c) => new()
    {
        UnitId = c.UnitId,
        SalesWholesaleCommPercent = c.SalesWholesaleCommPercent,
        SalesHalfWholesaleCommPercent = c.SalesHalfWholesaleCommPercent,
        SalesRetailCommPercent = c.SalesRetailCommPercent,
        TechComm = c.TechComm,
        Assistant1Comm = c.Assistant1Comm,
        Assistant2Comm = c.Assistant2Comm,
        Assistant3Comm = c.Assistant3Comm,
    };

    // Centralized validation used by both Create and Update.
    // isUpdate/currentId let uniqueness checks exclude the record being updated.
    private async Task<string?> ValidateAsync(ProductDto dto, bool isUpdate, long? currentId = null)
    {
        if (dto is null)
            return "Product payload is required";

        // --- Name ---
        if (string.IsNullOrWhiteSpace(dto.Name))
            return "Product name is required";

        if (dto.Name.Trim().Length > NameMaxLength)
            return $"Product name cannot exceed {NameMaxLength} characters";

        dto.Name = dto.Name.Trim();

        // --- Barcode (required + unique) ---
        //if (string.IsNullOrWhiteSpace(dto.Barcode))
        //    return "Barcode is required";

        //if (dto.Barcode.Trim().Length > BarcodeMaxLength)
        //    return $"Barcode cannot exceed {BarcodeMaxLength} characters";

        dto.Barcode = dto.Barcode?.Trim() ?? "";

        var barcodeQuery = _dbContext.Product
            .Where(p => !p.IsDeleted && p.Barcode == dto.Barcode);

        if (isUpdate && currentId.HasValue)
            barcodeQuery = barcodeQuery.Where(p => p.Id != currentId.Value);

        if (await barcodeQuery.AnyAsync())
            return $"A product with barcode '{dto.Barcode}' already exists";

        // --- Description (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(dto.Description) && dto.Description.Length > DescriptionMaxLength)
            return $"Description cannot exceed {DescriptionMaxLength} characters";

        // --- Category (required FK) ---
        if (dto.CategoryId <= 0)
            return "CategoryId is required";

        var categoryExists = await _dbContext.ProductCategory
            .AnyAsync(c => c.Id == dto.CategoryId && !c.IsDeleted);

        if (!categoryExists)
            return $"Category with Id {dto.CategoryId} was not found";

        // --- Prices ---
        if (dto.BuyPrice < 0)
            return "BuyPrice cannot be negative";

        if (dto.SalePrice < 0)
            return "SalePrice cannot be negative";

        if (dto.SalePrice < dto.BuyPrice)
            return "SalePrice cannot be lower than BuyPrice";

        // --- Enums ---
        if (!Enum.IsDefined(typeof(StorageType), dto.StorageType))
            return "StorageType is not a valid value";

        if (!Enum.IsDefined(typeof(ProductType), dto.ProductType))
            return "ProductType is not a valid value";

        // --- ArrangeOrder ---
        if (dto.ArrangeOrder.HasValue && dto.ArrangeOrder.Value < 0)
            return "ArrangeOrder cannot be negative";

        return await ValidateCommissionsAsync(dto.UnitCommissions);
    }
    private async Task<string?> ValidateCommissionsAsync(List<ProductUnitCommissionDto>? list)
    {
        if (list is null || list.Count == 0) return null;

        if (list.Any(c => c.UnitId <= 0))
            return "UnitId is required for every commission row";

        var unitIds = list.Select(c => c.UnitId).ToList();
        if (unitIds.Distinct().Count() != unitIds.Count)
            return "A unit can appear only once per product";

        var found = await _dbContext.ProductUnit.CountAsync(u => unitIds.Contains(u.Id));
        if (found != unitIds.Count)
            return "One or more units were not found";

        foreach (var c in list)
        {
            if (!IsValidPercent(c.SalesWholesaleCommPercent) ||
                !IsValidPercent(c.SalesHalfWholesaleCommPercent) ||
                !IsValidPercent(c.SalesRetailCommPercent))
                return "Sales commission percentages must be between 0 and 100";

            if (c.TechComm < 0 || c.Assistant1Comm < 0 || c.Assistant2Comm < 0 || c.Assistant3Comm < 0)
                return "Commission amounts cannot be negative";
        }
        return null;
    }
    public async Task<ServiceResult<List<ProductDto>>> GetAllAsync()
    {
        var products = await _dbContext.Product
            .Where(z => !z.IsDeleted)
            .Include(p => p.Category)
            .AsNoTracking()
            .ToListAsync();

        var dtos = products.Select(ToDto).ToList();

        return ServiceResult<List<ProductDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<ProductDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<ProductDto?>.Failed("A valid product Id is required");

        var product = await _dbContext.Product
            .Where(z => !z.IsDeleted && z.Id == id)
            .Include(p => p.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (product is null)
            return ServiceResult<ProductDto?>.NotFound("Product not found");

        var comms = await _dbContext.ProductUnitCommission
                .Where(c => c.ProductId == id)
                .AsNoTracking()
                .ToListAsync();

        var dto = ToDto(product);
        dto.UnitCommissions = comms.Select(ToCommissionDto).ToList();
        return ServiceResult<ProductDto?>.Ok(dto);
    }

    public async Task<ServiceResult<ProductDto>> CreateAsync(ProductDto dto)
    {
        var product = ToEntity(dto);
        product.Id = 0;   // never trust a client-supplied id

        foreach (var c in dto.UnitCommissions)
        {
            var entity = ToCommissionEntity(c);
            entity.Product = product;
            _dbContext.ProductUnitCommission.Add(entity);
        }

        _dbContext.Product.Add(product);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Product created with Id {ProductId}", product.Id);

        var created = await _dbContext.Product
            .Include(p => p.Category)
            .AsNoTracking()
            .FirstAsync(p => p.Id == product.Id);

        var result = ToDto(created);
        result.UnitCommissions = dto.UnitCommissions;
        return ServiceResult<ProductDto>.Ok(result);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, ProductDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Product payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.Product.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Product not found");

        var validationError = await ValidateAsync(dto, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating product {ProductId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        // Update mutable fields explicitly rather than blindly overwriting audit/soft-delete fields
        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.Barcode = dto.Barcode;
        existing.CategoryId = dto.CategoryId;
        existing.BuyPrice = dto.BuyPrice;
        existing.SalePrice = dto.SalePrice;
        existing.StorageType = dto.StorageType;
        existing.ProductType = dto.ProductType;
        existing.Active = dto.Active;
        existing.HasSerial = dto.HasSerial;
        existing.EnableCounter = dto.EnableCounter;
        existing.EnableAddOrder = dto.EnableAddOrder;
        existing.ArrangeOrder = dto.ArrangeOrder;
        existing.InspectionCommissionDeductionPercent = dto.InspectionCommissionDeductionPercent;

        var oldComms = await _dbContext.ProductUnitCommission
            .Where(c => c.ProductId == id)
            .ToListAsync();
        _dbContext.ProductUnitCommission.RemoveRange(oldComms);

        foreach (var c in dto.UnitCommissions)
        {
            var entity = ToCommissionEntity(c);
            entity.ProductId = id;
            _dbContext.ProductUnitCommission.Add(entity);
        }

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating product with Id {ProductId}", id);
            throw;
        }

        _logger.LogInformation("Product with Id {ProductId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var product = await _dbContext.Product.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (product is null)
            return ServiceResult<bool>.NotFound("Product not found");

        product.IsDeleted = true;
        product.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Product with Id {ProductId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}