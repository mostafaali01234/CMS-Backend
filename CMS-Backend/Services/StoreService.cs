// Services/StoreService.cs
using CMS.Api.Data;
using CMS.Domain.Models;
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.RegularExpressions;

namespace CMS.Api.Services;

public class StoreService : IStoreService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<StoreService> _logger;

    private const int NameMaxLength = 150;
    private const int NotesMaxLength = 2000;
    private const int PhoneMaxLength = 20;
    private const int AddressMaxLength = 250;

    private static readonly Regex PhoneRegex = new(@"^[0-9+\-\s()]{6,20}$", RegexOptions.Compiled);

    public StoreService(
        AppDbContext dbContext,
        ILogger<StoreService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private StoreDto ToDto(Store store)
    {
        return new StoreDto
        {
            Id = store.Id,
            Name = store.Name,
            ManagerId = store.ManagerId ?? 0,
            ManagerName = store.Manager?.Name ?? "",
            Phone = store.Phone,
            Notes = store.Notes,
            CityId = store.CityId,
            CityName = store.City?.Name ?? "",
            Address = store.Address,
            Active = store.Active,
            CreatedAtUtc = store.CreatedAtUtc,
            CreatedBy = store.CreatedBy ?? "",
            UpdatedAtUtc = store.UpdatedAtUtc,
            UpdatedBy = store.UpdatedBy ?? ""
        };
    }

    private Store ToEntity(StoreDto dto)
    {
        return new Store
        {
            Id = dto.Id,
            Name = dto.Name,
            ManagerId = dto.ManagerId,
            Phone = dto.Phone,
            Notes = dto.Notes,
            CityId = dto.CityId,
            Address = dto.Address,
            Active = dto.Active
        };
    }

    // Centralized validation used by both Create and Update.
    // isUpdate/currentId let uniqueness checks exclude the record being updated.
    private async Task<string?> ValidateAsync(StoreDto dto, bool isUpdate, long? currentId = null)
    {
        if (dto is null)
            return "Store payload is required";

        // --- Name (required + unique) ---
        if (string.IsNullOrWhiteSpace(dto.Name))
            return "Store name is required";

        if (dto.Name.Trim().Length > NameMaxLength)
            return $"Store name cannot exceed {NameMaxLength} characters";

        dto.Name = dto.Name.Trim();

        var nameQuery = _dbContext.Store
            .Where(s => !s.IsDeleted && s.Name.ToLower() == dto.Name.ToLower());

        if (isUpdate && currentId.HasValue)
            nameQuery = nameQuery.Where(s => s.Id != currentId.Value);

        if (await nameQuery.AnyAsync())
            return $"A store named '{dto.Name}' already exists";

        // --- Manager (required FK) ---
        if (dto.ManagerId <= 0)
            return "ManagerId is required";

        var managerExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == dto.ManagerId && !e.IsDeleted);

        if (!managerExists)
            return $"Manager with Id {dto.ManagerId} was not found";

        // --- Phone (required, format-checked) ---
        if (string.IsNullOrWhiteSpace(dto.Phone))
            return "Phone is required";

        dto.Phone = dto.Phone.Trim();

        if (dto.Phone.Length > PhoneMaxLength || !PhoneRegex.IsMatch(dto.Phone))
            return "Phone is not a valid phone number";

        // --- City (required FK) ---
        if (dto.CityId <= 0)
            return "CityId is required";

        var cityExists = await _dbContext.City.AnyAsync(c => c.Id == dto.CityId);
        if (!cityExists)
            return $"City with Id {dto.CityId} was not found";

        // --- Address (required, length only) ---
        if (string.IsNullOrWhiteSpace(dto.Address))
            return "Address is required";

        if (dto.Address.Length > AddressMaxLength)
            return $"Address cannot exceed {AddressMaxLength} characters";

        // --- Notes (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Length > NotesMaxLength)
            return $"Notes cannot exceed {NotesMaxLength} characters";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<StoreDto>>> GetAllAsync()
    {
        var stores = await _dbContext.Store
            .Where(z => !z.IsDeleted)
            .Include(s => s.Manager)
            .Include(s => s.City)
            .AsNoTracking()
            .ToListAsync();

        var dtos = stores.Select(ToDto).ToList();
        return ServiceResult<List<StoreDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<StoreDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<StoreDto?>.Failed("A valid store Id is required");

        var store = await _dbContext.Store
            .Where(z => !z.IsDeleted && z.Id == id)
            .Include(s => s.Manager)
            .Include(s => s.City)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (store is null)
            return ServiceResult<StoreDto?>.NotFound("Store not found");

        return ServiceResult<StoreDto?>.Ok(ToDto(store));
    }

    public async Task<ServiceResult<StoreDto>> CreateAsync(StoreDto dto)
    {
        var validationError = await ValidateAsync(dto, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating store: {Error}", validationError);
            return ServiceResult<StoreDto>.Failed(validationError);
        }

        var store = ToEntity(dto);

        _dbContext.Store.Add(store);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Store created with Id {StoreId}", store.Id);

        var created = await _dbContext.Store
            .Include(s => s.Manager)
            .Include(s => s.City)
            .AsNoTracking()
            .FirstAsync(s => s.Id == store.Id);

        return ServiceResult<StoreDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, StoreDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Store payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.Store.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Store not found");

        var validationError = await ValidateAsync(dto, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating store {StoreId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        // Update mutable fields explicitly rather than blindly overwriting audit/soft-delete fields
        existing.Name = dto.Name;
        existing.ManagerId = dto.ManagerId;
        existing.Phone = dto.Phone;
        existing.Notes = dto.Notes;
        existing.CityId = dto.CityId;
        existing.Address = dto.Address;
        existing.Active = dto.Active;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating store with Id {StoreId}", id);
            throw;
        }

        _logger.LogInformation("Store with Id {StoreId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var store = await _dbContext.Store.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (store is null)
            return ServiceResult<bool>.NotFound("Store not found");

        store.IsDeleted = true;
        store.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Store with Id {StoreId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}