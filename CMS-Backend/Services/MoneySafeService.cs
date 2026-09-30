// Services/MoneySafeService.cs
using CMS.Api.Data;
using CMS.Domain.Models;
using CMS.Api.Models.DTOs;
using CMS.Api.Models.DTOs.Responses;
using CMS.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMS.Api.Services;

public class MoneySafeService : IMoneySafeService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<MoneySafeService> _logger;

    private const int NameMaxLength = 150;
    private const int NotesMaxLength = 2000;

    public MoneySafeService(
        AppDbContext dbContext,
        ILogger<MoneySafeService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private MoneySafeDto ToDto(MoneySafe safe)
    {
        return new MoneySafeDto
        {
            Id = safe.Id,
            Cod = safe.Cod,
            Name = safe.Name,
            Notes = safe.Notes,
            Active = safe.Active,
            OpeningBalance = safe.OpeningBalance,
            OpeningBalanceCurrency = safe.OpeningBalanceCurrency,
            ManagerId = safe.ManagerId,
            ManagerName = safe.Manager?.Name ?? "",
            CategoryId = safe.CategoryId,
            CategoryName = safe.Category?.Name ?? "",
            TypeId = safe.TypeId,
            TypeName = safe.Type?.Name ?? "",
            CreatedAtUtc = safe.CreatedAtUtc,
            CreatedBy = safe.CreatedBy ?? "",
            UpdatedAtUtc = safe.UpdatedAtUtc,
            UpdatedBy = safe.UpdatedBy ?? ""
        };
    }

    private MoneySafe ToEntity(MoneySafeDto dto)
    {
        return new MoneySafe
        {
            Id = dto.Id,
            Cod = dto.Cod,
            Name = dto.Name,
            Notes = dto.Notes,
            Active = dto.Active,
            OpeningBalance = dto.OpeningBalance,
            OpeningBalanceCurrency = dto.OpeningBalanceCurrency,
            ManagerId = dto.ManagerId,
            CategoryId = dto.CategoryId,
            TypeId = dto.TypeId
        };
    }

    // Centralized validation used by both Create and Update.
    private async Task<string?> ValidateAsync(MoneySafeDto dto, bool isUpdate, long? currentId = null)
    {
        if (dto is null)
            return "Money safe payload is required";

        // --- Cod (required + unique) ---
        if (dto.Cod <= 0)
            return "Cod is required and must be greater than zero";

        var codQuery = _dbContext.MoneySafe.Where(m => !m.IsDeleted && m.Cod == dto.Cod);

        if (isUpdate && currentId.HasValue)
            codQuery = codQuery.Where(m => m.Id != currentId.Value);

        if (await codQuery.AnyAsync())
            return $"A money safe with code {dto.Cod} already exists";

        // --- Name (required + unique) ---
        if (string.IsNullOrWhiteSpace(dto.Name))
            return "Money safe name is required";

        if (dto.Name.Trim().Length > NameMaxLength)
            return $"Money safe name cannot exceed {NameMaxLength} characters";

        dto.Name = dto.Name.Trim();

        var nameQuery = _dbContext.MoneySafe
            .Where(m => !m.IsDeleted && m.Name.ToLower() == dto.Name.ToLower());

        if (isUpdate && currentId.HasValue)
            nameQuery = nameQuery.Where(m => m.Id != currentId.Value);

        if (await nameQuery.AnyAsync())
            return $"A money safe named '{dto.Name}' already exists";

        // --- Notes (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Length > NotesMaxLength)
            return $"Notes cannot exceed {NotesMaxLength} characters";

        // --- OpeningBalance / OpeningBalanceCurrency --- (unbounded; may legitimately be negative)

        // --- Manager (optional FK) ---
        if (dto.ManagerId.HasValue)
        {
            if (dto.ManagerId.Value <= 0)
                return "ManagerId is invalid";

            var managerExists = await _dbContext.Employee
                .AnyAsync(e => e.Id == dto.ManagerId.Value && !e.IsDeleted);

            if (!managerExists)
                return $"Manager with Id {dto.ManagerId.Value} was not found";
        }

        // --- Category (optional FK) ---
        if (dto.CategoryId.HasValue)
        {
            if (dto.CategoryId.Value <= 0)
                return "CategoryId is invalid";

            var categoryExists = await _dbContext.MoneySafeCategory
                .AnyAsync(c => c.Id == dto.CategoryId.Value);

            if (!categoryExists)
                return $"Category with Id {dto.CategoryId.Value} was not found";
        }

        // --- Type (optional FK) ---
        if (dto.TypeId.HasValue)
        {
            if (dto.TypeId.Value <= 0)
                return "TypeId is invalid";

            var typeExists = await _dbContext.MoneySafeType
                .AnyAsync(t => t.Id == dto.TypeId.Value);

            if (!typeExists)
                return $"Type with Id {dto.TypeId.Value} was not found";
        }

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<MoneySafeDto>>> GetAllAsync()
    {
        var safes = await _dbContext.MoneySafe
            .Where(z => !z.IsDeleted)
            .Include(m => m.Manager)
            .Include(m => m.Category)
            .Include(m => m.Type)
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<MoneySafeDto>>.Ok(safes.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<MoneySafeDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<MoneySafeDto?>.Failed("A valid money safe Id is required");

        var safe = await _dbContext.MoneySafe
            .Where(z => !z.IsDeleted && z.Id == id)
            .Include(m => m.Manager)
            .Include(m => m.Category)
            .Include(m => m.Type)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (safe is null)
            return ServiceResult<MoneySafeDto?>.NotFound("Money safe not found");

        return ServiceResult<MoneySafeDto?>.Ok(ToDto(safe));
    }

    public async Task<ServiceResult<MoneySafeDto>> CreateAsync(MoneySafeDto dto)
    {
        var validationError = await ValidateAsync(dto, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating money safe: {Error}", validationError);
            return ServiceResult<MoneySafeDto>.Failed(validationError);
        }

        var safe = ToEntity(dto);

        _dbContext.MoneySafe.Add(safe);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Money safe created with Id {MoneySafeId}", safe.Id);

        var created = await _dbContext.MoneySafe
            .Include(m => m.Manager)
            .Include(m => m.Category)
            .Include(m => m.Type)
            .AsNoTracking()
            .FirstAsync(m => m.Id == safe.Id);

        return ServiceResult<MoneySafeDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, MoneySafeDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Money safe payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.MoneySafe.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Money safe not found");

        var validationError = await ValidateAsync(dto, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating money safe {MoneySafeId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        // Update mutable fields explicitly rather than blindly overwriting audit/soft-delete fields
        existing.Cod = dto.Cod;
        existing.Name = dto.Name;
        existing.Notes = dto.Notes;
        existing.Active = dto.Active;
        existing.OpeningBalance = dto.OpeningBalance;
        existing.OpeningBalanceCurrency = dto.OpeningBalanceCurrency;
        existing.ManagerId = dto.ManagerId;
        existing.CategoryId = dto.CategoryId;
        existing.TypeId = dto.TypeId;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating money safe with Id {MoneySafeId}", id);
            throw;
        }

        _logger.LogInformation("Money safe with Id {MoneySafeId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var safe = await _dbContext.MoneySafe.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
        if (safe is null)
            return ServiceResult<bool>.NotFound("Money safe not found");

        safe.IsDeleted = true;
        safe.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Money safe with Id {MoneySafeId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}