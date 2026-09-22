using CMS_Backend.Configuration;
using CMS_Backend.Data;
using CMS_Backend.Models;
using CMS_Backend.Models.DTOs;
using CMS_Backend.Models.DTOs.Responses;
using CMS_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CMS_Backend.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ProductService> _logger;

    private const int NameMaxLength = 150;
    private const int BarcodeMaxLength = 50;
    private const int DescriptionMaxLength = 2000;

    public ProductService(
        AppDbContext dbContext,
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
            UpdatedBy = product.UpdatedBy ?? ""
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
            ArrangeOrder = dto.ArrangeOrder
        };
    }

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
        if (string.IsNullOrWhiteSpace(dto.Barcode))
            return "Barcode is required";

        if (dto.Barcode.Trim().Length > BarcodeMaxLength)
            return $"Barcode cannot exceed {BarcodeMaxLength} characters";

        dto.Barcode = dto.Barcode.Trim();

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

        return null; // no validation errors
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

        return ServiceResult<ProductDto?>.Ok(ToDto(product));
    }

    public async Task<ServiceResult<ProductDto>> CreateAsync(ProductDto cat)
    {
        var validationError = await ValidateAsync(cat, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating product: {Error}", validationError);
            return ServiceResult<ProductDto>.Failed(validationError);
        }

        var product = ToEntity(cat);

        _dbContext.Product.Add(product);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Product created with Id {ProductId}", product.Id);

        var created = await _dbContext.Product
            .Include(p => p.Category)
            .AsNoTracking()
            .FirstAsync(p => p.Id == product.Id);

        return ServiceResult<ProductDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, ProductDto cat)
    {
        if (cat is null)
            return ServiceResult<bool>.Failed("Product payload is required");

        if (id != cat.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.Product.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Product not found");

        var validationError = await ValidateAsync(cat, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating product {ProductId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        // Update mutable fields explicitly rather than blindly overwriting audit/soft-delete fields
        existing.Name = cat.Name;
        existing.Description = cat.Description;
        existing.Barcode = cat.Barcode;
        existing.CategoryId = cat.CategoryId;
        existing.BuyPrice = cat.BuyPrice;
        existing.SalePrice = cat.SalePrice;
        existing.StorageType = cat.StorageType;
        existing.ProductType = cat.ProductType;
        existing.Active = cat.Active;
        existing.HasSerial = cat.HasSerial;
        existing.EnableCounter = cat.EnableCounter;
        existing.EnableAddOrder = cat.EnableAddOrder;
        existing.ArrangeOrder = cat.ArrangeOrder;

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