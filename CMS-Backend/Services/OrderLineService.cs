using CMS_Backend.Data;
using CMS_Backend.Models;
using CMS_Backend.Models.DTOs.Responses;
using CMS_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;
namespace CMS_Backend.Services;

public class OrderLineService : IOrderLineService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<OrderLineService> _logger;

    private const int NameMaxLength = 100;

    public OrderLineService(
        AppDbContext dbContext,
        ILogger<OrderLineService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // Centralized validation used by both Create and Update.
    // isUpdate/currentId let the uniqueness check exclude the record being updated.
    private async Task<string?> ValidateAsync(OrderLine line, bool isUpdate, long? currentId = null)
    {
        if (line is null)
            return "OrderLine payload is required";

        if (string.IsNullOrWhiteSpace(line.Name))
            return "OrderLine name is required";

        if (line.Name.Trim().Length > NameMaxLength)
            return $"OrderLine name cannot exceed {NameMaxLength} characters";

        // Normalize whitespace so "Sales " and "Sales" aren't treated as different names
        var normalizedName = line.Name.Trim();
        line.Name = normalizedName;

        var duplicateQuery = _dbContext.OrderLine
            .Where(d => d.Name.ToLower() == normalizedName.ToLower());

        if (isUpdate && currentId.HasValue)
            duplicateQuery = duplicateQuery.Where(d => d.Id != currentId.Value);

        var duplicateExists = await duplicateQuery.AnyAsync();
        if (duplicateExists)
            return $"A OrderLine named '{normalizedName}' already exists";

        if (line.ManagerId <= 0)
            return "StateId is required";

        var stateExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == line.ManagerId);

        if (!stateExists)
            return $"Employee with Id {line.Manager} was not found";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<OrderLine>>> GetAllAsync()
    {
        var lines = await _dbContext.OrderLine.AsNoTracking().ToListAsync();
        return ServiceResult<List<OrderLine>>.Ok(lines);
    }

    public async Task<ServiceResult<OrderLine?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<OrderLine?>.Failed("A valid OrderLine Id is required");

        var line = await _dbContext.OrderLine.Where(z => z.Id == id).AsNoTracking().FirstOrDefaultAsync();

        if (line is null)
            return ServiceResult<OrderLine?>.NotFound("OrderLine not found");

        return ServiceResult<OrderLine?>.Ok(line);
    }

    public async Task<ServiceResult<OrderLine>> CreateAsync(OrderLine line)
    {
        var validationError = await ValidateAsync(line, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating OrderLine: {Error}", validationError);
            return ServiceResult<OrderLine>.Failed(validationError);
        }

        _dbContext.OrderLine.Add(line);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("OrderLine created with Id {OrderLineId}", line.Id);

        var created = await _dbContext.OrderLine
            .AsNoTracking()
            .FirstAsync(d => d.Id == line.Id);

        return ServiceResult<OrderLine>.Ok(created);
    }

    public async Task<ServiceResult<bool>> AddCitiesAsync(long id, List<long> citiesIds)
    {
        if (id == 0)
            return ServiceResult<bool>.Failed("OrderLine id is required");
        
        if (citiesIds == null || !citiesIds.Any())
            return ServiceResult<bool>.Failed("Atleast one city id is required");

        var line = await _dbContext.OrderLine.FirstOrDefaultAsync(d => d.Id == id);
        if (line == null)
            return ServiceResult<bool>.NotFound("OrderLine Not Found");

        line.Cities.Clear();

        var cities = await _dbContext.City
          .Where(c => citiesIds.Contains(c.Id))
          .ToListAsync();

        foreach (var city in cities)
        {
            line.Cities.Add(city);
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Cities has been added to OrderLine with Id {OrderLineId}", line.Id);

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> AddCategoriesAsync(long id, List<long> categoriesIds)
    {
        if (id == 0)
            return ServiceResult<bool>.Failed("OrderLine id is required");
        
        if (categoriesIds == null || !categoriesIds.Any())
            return ServiceResult<bool>.Failed("Atleast one category id is required");

        var line = await _dbContext.OrderLine.FirstOrDefaultAsync(d => d.Id == id);
        if (line == null)
            return ServiceResult<bool>.NotFound("OrderLine Not Found");

        line.Cities.Clear();

        var categories = await _dbContext.ProductCategory
          .Where(c => categoriesIds.Contains(c.Id))
          .ToListAsync();

        foreach (var category in categories)
        {
            line.ProductCategories.Add(category);
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Categories has been added to OrderLine with Id {OrderLineId}", line.Id);

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, OrderLine line)
    {
        if (line is null)
            return ServiceResult<bool>.Failed("OrderLine payload is required");

        if (id != line.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.OrderLine.FirstOrDefaultAsync(p => p.Id == id);
        if (existing is null)
            return ServiceResult<bool>.NotFound("OrderLine not found");

        var validationError = await ValidateAsync(line, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating OrderLine {OrderLineId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        // Update only the mutable fields rather than blindly overwriting audit/soft-delete fields
        existing.Name = line.Name;
        existing.ManagerId = line.ManagerId;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating OrderLine with Id {OrderLineId}", id);
            throw;
        }

        _logger.LogInformation("OrderLine with Id {OrderLineId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var line = await _dbContext.OrderLine.FirstOrDefaultAsync(d => d.Id == id);
        if (line is null)
            return ServiceResult<bool>.NotFound("OrderLine not found");

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("OrderLine with Id {OrderLineId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}