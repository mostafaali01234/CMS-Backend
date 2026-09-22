using CMS_Backend.Data;
using CMS_Backend.Models;
using CMS_Backend.Models.DTOs.Responses;
using CMS_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CMS_Backend.Services;

public class StateService : IStateService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<StateService> _logger;

    private const int NameMaxLength = 100;

    public StateService(
        AppDbContext dbContext,
        ILogger<StateService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // Centralized validation used by both Create and Update.
    // isUpdate/currentId let the uniqueness check exclude the record being updated.
    private async Task<string?> ValidateAsync(CountryState state, bool isUpdate, long? currentId = null)
    {
        if (state is null)
            return "State payload is required";

        if (state.OrderNumber == null)
            return "State OrderNumber is required";

        if (string.IsNullOrWhiteSpace(state.Name))
            return "State name is required";

        if (state.Name.Trim().Length > NameMaxLength)
            return $"State name cannot exceed {NameMaxLength} characters";

        // Normalize whitespace so "Manager " and "Manager" aren't treated as different names
        var normalizedName = state.Name.Trim();
        state.Name = normalizedName;

        var duplicateQuery = _dbContext.CountryState
            .Where(j => j.Name.ToLower() == normalizedName.ToLower());

        if (isUpdate && currentId.HasValue)
            duplicateQuery = duplicateQuery.Where(j => j.Id != currentId.Value);

        var duplicateExists = await duplicateQuery.AnyAsync();
        if (duplicateExists)
            return $"A State named '{normalizedName}' already exists";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<CountryState>>> GetAllAsync()
    {
        var states = await _dbContext.CountryState.AsNoTracking().ToListAsync();
        return ServiceResult<List<CountryState>>.Ok(states);
    }

    public async Task<ServiceResult<CountryState?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<CountryState?>.Failed("A valid State Id is required");

        var state = await _dbContext.CountryState.Where(z => z.Id == id).AsNoTracking().FirstOrDefaultAsync();

        if (state is null)
            return ServiceResult<CountryState?>.NotFound("State not found");

        return ServiceResult<CountryState?>.Ok(state);
    }

    public async Task<ServiceResult<CountryState>> CreateAsync(CountryState state)
    {
        var validationError = await ValidateAsync(state, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating State: {Error}", validationError);
            return ServiceResult<CountryState>.Failed(validationError);
        }

        _dbContext.CountryState.Add(state);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("State created with Id {StateId}", state.Id);
        return ServiceResult<CountryState>.Ok(state);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, CountryState state)
    {
        if (state is null)
            return ServiceResult<bool>.Failed("State payload is required");

        if (id != state.Id)
            return ServiceResult<bool>.Conflict("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.CountryState.FirstOrDefaultAsync(p => p.Id == id);
        if (existing is null)
            return ServiceResult<bool>.NotFound("State not found");

        var validationError = await ValidateAsync(state, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating State {StateId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        // Update only the mutable field rather than blindly overwriting audit/soft-delete fields
        existing.Name = state.Name;
        existing.OrderNumber = state.OrderNumber;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating State with Id {StateId}", id);
            throw;
        }

        _logger.LogInformation("State with Id {StateId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var state = await _dbContext.CountryState.FirstOrDefaultAsync(j => j.Id == id);
        if (state is null)
            return ServiceResult<bool>.NotFound("State not found");

        // Guard against deleting a State that's still assigned to city,
        // if City has a StateId FK — remove this block if that's not the case.
        var isInUse = await _dbContext.City
            .AnyAsync(e => e.StateId == id);

        if (isInUse)
            return ServiceResult<bool>.Conflict("Cannot delete a state that is still assigned to cities");


        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("State with Id {StateId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}