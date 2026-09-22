using CMS_Backend.Data;
using CMS_Backend.Models;
using CMS_Backend.Models.DTOs.Responses;
using CMS_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;
namespace CMS_Backend.Services;

public class CityService : ICityService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<CityService> _logger;

    private const int NameMaxLength = 100;

    public CityService(
        AppDbContext dbContext,
        ILogger<CityService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // Centralized validation used by both Create and Update.
    // isUpdate/currentId let the uniqueness check exclude the record being updated.
    private async Task<string?> ValidateAsync(City city, bool isUpdate, long? currentId = null)
    {
        if (city is null)
            return "City payload is required";

        if (string.IsNullOrWhiteSpace(city.Name))
            return "City name is required";

        if (city.Name.Trim().Length > NameMaxLength)
            return $"City name cannot exceed {NameMaxLength} characters";

        // Normalize whitespace so "Sales " and "Sales" aren't treated as different names
        var normalizedName = city.Name.Trim();
        city.Name = normalizedName;

        var duplicateQuery = _dbContext.City
            .Where(d => d.Name.ToLower() == normalizedName.ToLower());

        if (isUpdate && currentId.HasValue)
            duplicateQuery = duplicateQuery.Where(d => d.Id != currentId.Value);

        var duplicateExists = await duplicateQuery.AnyAsync();
        if (duplicateExists)
            return $"A city named '{normalizedName}' already exists";

        if (city.StateId <= 0)
            return "StateId is required";

        var stateExists = await _dbContext.CountryState
            .AnyAsync(e => e.Id == city.StateId);

        if (!stateExists)
            return $"State with Id {city.StateId} was not found";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<City>>> GetAllAsync()
    {
        var cities = await _dbContext.City.AsNoTracking().ToListAsync();
        return ServiceResult<List<City>>.Ok(cities);
    }

    public async Task<ServiceResult<City?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<City?>.Failed("A valid City Id is required");

        var city = await _dbContext.City.Where(z => z.Id == id).AsNoTracking().FirstOrDefaultAsync();

        if (city is null)
            return ServiceResult<City?>.NotFound("City not found");

        return ServiceResult<City?>.Ok(city);
    }

    public async Task<ServiceResult<City>> CreateAsync(City city)
    {
        var validationError = await ValidateAsync(city, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating City: {Error}", validationError);
            return ServiceResult<City>.Failed(validationError);
        }

        _dbContext.City.Add(city);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("City created with Id {CityId}", city.Id);

        var created = await _dbContext.City
            .AsNoTracking()
            .FirstAsync(d => d.Id == city.Id);

        return ServiceResult<City>.Ok(created);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, City city)
    {
        if (city is null)
            return ServiceResult<bool>.Failed("City payload is required");

        if (id != city.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.City.FirstOrDefaultAsync(p => p.Id == id);
        if (existing is null)
            return ServiceResult<bool>.NotFound("City not found");

        var validationError = await ValidateAsync(city, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating City {CityId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        // Update only the mutable fields rather than blindly overwriting audit/soft-delete fields
        existing.Name = city.Name;
        existing.StateId = city.StateId;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating City with Id {CityId}", id);
            throw;
        }

        _logger.LogInformation("City with Id {CityId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var city = await _dbContext.City.FirstOrDefaultAsync(d => d.Id == id);
        if (city is null)
            return ServiceResult<bool>.NotFound("City not found");

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("City with Id {CityId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}