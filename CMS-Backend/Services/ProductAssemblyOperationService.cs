// Services/ProductAssemblyOperationService.cs
using CMS_Backend.Data;
using CMS_Backend.Models;
using CMS_Backend.Models.DTOs;
using CMS_Backend.Models.DTOs.Responses;
using CMS_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CMS_Backend.Services;

public class ProductAssemblyOperationService : IProductAssemblyOperationService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ProductAssemblyOperationService> _logger;

    private const int NotesMaxLength = 2000;

    public ProductAssemblyOperationService(
        AppDbContext dbContext,
        ILogger<ProductAssemblyOperationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private ProductAssemblyOperationDto ToDto(ProductAssemblyOperation op)
    {
        return new ProductAssemblyOperationDto
        {
            Id = op.Id,
            OutProductId = op.OutProductId,
            OutProductName = op.OutProduct?.Name ?? "",
            OutStoreId = op.OutStoreId,
            OutStoreName = op.OutStore?.Name ?? "",
            InStoreId = op.InStoreId,
            InStoreName = op.InStore?.Name ?? "",
            Quantity = op.Quantity,
            OperationDate = op.OperationDate,
            Notes = op.Notes,
            CreatedAtUtc = op.CreatedAtUtc,
            CreatedBy = op.CreatedBy ?? "",
            UpdatedAtUtc = op.UpdatedAtUtc,
            UpdatedBy = op.UpdatedBy ?? ""
        };
    }

    private ProductAssemblyOperation ToEntity(ProductAssemblyOperationDto dto)
    {
        return new ProductAssemblyOperation
        {
            Id = dto.Id,
            OutProductId = dto.OutProductId,
            OutStoreId = dto.OutStoreId,
            InStoreId = dto.InStoreId,
            Quantity = dto.Quantity,
            OperationDate = dto.OperationDate,
            Notes = dto.Notes
        };
    }

    // Centralized validation used by both Create and Update.
    private async Task<string?> ValidateAsync(ProductAssemblyOperationDto dto)
    {
        if (dto is null)
            return "Assembly operation payload is required";

        // --- OutProduct (required FK) ---
        if (dto.OutProductId <= 0)
            return "OutProductId is required";

        var outProductExists = await _dbContext.Product
            .AnyAsync(p => p.Id == dto.OutProductId && !p.IsDeleted);

        if (!outProductExists)
            return $"Out product with Id {dto.OutProductId} was not found";

        // --- OutStore (required FK) ---
        if (dto.OutStoreId <= 0)
            return "OutStoreId is required";

        var outStoreExists = await _dbContext.Store
            .AnyAsync(s => s.Id == dto.OutStoreId && !s.IsDeleted);

        if (!outStoreExists)
            return $"Out store with Id {dto.OutStoreId} was not found";

        // --- InStore (required FK) ---
        if (dto.InStoreId <= 0)
            return "InStoreId is required";

        var inStoreExists = await _dbContext.Store
            .AnyAsync(s => s.Id == dto.InStoreId && !s.IsDeleted);

        if (!inStoreExists)
            return $"In store with Id {dto.InStoreId} was not found";

        // --- Quantity ---
        if (dto.Quantity <= 0)
            return "Quantity must be greater than zero";

        // --- OperationDate ---
        if (dto.OperationDate == default)
            return "OperationDate is required";

        // --- Assembly definition must exist for this OutProductId ---
        // An assembly operation without a matching definition has nothing to "consume" from,
        // so it must reference a product that actually has a defined bill of materials.
        var hasDefinition = await _dbContext.ProductAssemblyDefinition
            .AnyAsync(d => !d.IsDeleted && d.OutProductId == dto.OutProductId);

        if (!hasDefinition)
            return $"Product with Id {dto.OutProductId} has no assembly definition; cannot record an assembly operation for it";

        // --- Notes (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Length > NotesMaxLength)
            return $"Notes cannot exceed {NotesMaxLength} characters";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<ProductAssemblyOperationDto>>> GetAllAsync()
    {
        var operations = await _dbContext.ProductAssemblyOperation
            .Where(z => !z.IsDeleted)
            .Include(o => o.OutProduct)
            .Include(o => o.OutStore)
            .Include(o => o.InStore)
            .AsNoTracking()
            .ToListAsync();

        var dtos = operations.Select(ToDto).ToList();
        return ServiceResult<List<ProductAssemblyOperationDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<ProductAssemblyOperationDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<ProductAssemblyOperationDto?>.Failed("A valid operation Id is required");

        var operation = await _dbContext.ProductAssemblyOperation
            .Where(z => !z.IsDeleted && z.Id == id)
            .Include(o => o.OutProduct)
            .Include(o => o.OutStore)
            .Include(o => o.InStore)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (operation is null)
            return ServiceResult<ProductAssemblyOperationDto?>.NotFound("Assembly operation not found");

        return ServiceResult<ProductAssemblyOperationDto?>.Ok(ToDto(operation));
    }

    public async Task<ServiceResult<ProductAssemblyOperationDto>> CreateAsync(ProductAssemblyOperationDto op)
    {
        var validationError = await ValidateAsync(op);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating assembly operation: {Error}", validationError);
            return ServiceResult<ProductAssemblyOperationDto>.Failed(validationError);
        }

        var operation = ToEntity(op);

        _dbContext.ProductAssemblyOperation.Add(operation);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Assembly operation created with Id {OperationId} for OutProductId {OutProductId}",
            operation.Id, operation.OutProductId);

        var created = await _dbContext.ProductAssemblyOperation
            .Include(o => o.OutProduct)
            .Include(o => o.OutStore)
            .Include(o => o.InStore)
            .AsNoTracking()
            .FirstAsync(o => o.Id == operation.Id);

        return ServiceResult<ProductAssemblyOperationDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, ProductAssemblyOperationDto op)
    {
        if (op is null)
            return ServiceResult<bool>.Failed("Assembly operation payload is required");

        if (id != op.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.ProductAssemblyOperation.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Assembly operation not found");

        var validationError = await ValidateAsync(op);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating assembly operation {OperationId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        // Update mutable fields explicitly rather than blindly overwriting audit/soft-delete fields
        existing.OutProductId = op.OutProductId;
        existing.OutStoreId = op.OutStoreId;
        existing.InStoreId = op.InStoreId;
        existing.Quantity = op.Quantity;
        existing.OperationDate = op.OperationDate;
        existing.Notes = op.Notes;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating assembly operation with Id {OperationId}", id);
            throw;
        }

        _logger.LogInformation("Assembly operation with Id {OperationId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var operation = await _dbContext.ProductAssemblyOperation.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
        if (operation is null)
            return ServiceResult<bool>.NotFound("Assembly operation not found");

        operation.IsDeleted = true;
        operation.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Assembly operation with Id {OperationId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}