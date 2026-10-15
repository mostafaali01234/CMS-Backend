using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using CMS.Application.Interfaces.Configuration;
using CMS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
namespace CMS.Application.Services;

public class OrderLineService : IOrderLineService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<OrderLineService> _logger;

    private const int NameMaxLength = 100;

    public OrderLineService(
        IAppDbContext dbContext,
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
            .Where(d => d.Name.ToLower() == normalizedName.ToLower() && !d.IsDeleted);

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
        var lines = await _dbContext.OrderLine
            .Include(z => z.ProductCategories)
            .Include(z => z.Cities)
            .Where(z => !z.IsDeleted)
            .AsNoTracking()
            .ToListAsync();
        return ServiceResult<List<OrderLine>>.Ok(lines);
    }

    public async Task<ServiceResult<OrderLine?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<OrderLine?>.Failed("A valid OrderLine Id is required");

        var line = await _dbContext.OrderLine
            .Include(z => z.ProductCategories)
            .Include(z => z.Cities)
            .Where(z => z.Id == id && !z.IsDeleted)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (line is null)
            return ServiceResult<OrderLine?>.NotFound("OrderLine not found");

        return ServiceResult<OrderLine?>.Ok(line);
    }

    public async Task<ServiceResult<OrderLine>> CreateAsync(OrderLineRequest request)
    {
        var cityIds = request.CityIds.Distinct().ToList();
        var categoryIds = request.CategoryIds.Distinct().ToList();
        
        var cities = await _dbContext.City.Where(c => cityIds.Contains(c.Id)).ToListAsync();
        if (cities.Count != cityIds.Count)
            return ServiceResult<OrderLine>.Failed("بعض المدن غير موجودة");

        var categories = await _dbContext.ProductCategory.Where(c => categoryIds.Contains(c.Id)).ToListAsync();
        if (categories.Count != categoryIds.Count)
            return ServiceResult<OrderLine>.Failed("بعض التصنيفات غير موجودة");

        var line = new OrderLine
        {
            Name = request.Name,
            Notes = request.Notes,
            ManagerId = request.ManagerId,
            Cities = cities,
            ProductCategories = categories,
        };

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

    public async Task<ServiceResult<bool>> UpdateAsync(long id, OrderLineRequest request)
    {
        var line = await _dbContext.OrderLine
       .Include(l => l.Cities)
       .Include(l => l.ProductCategories)
       .FirstOrDefaultAsync(l => l.Id == id);

        if (line is null)
            return ServiceResult<bool>.NotFound("الخط غير موجود");

        var cityIds = request.CityIds.Distinct().ToList();
        var categoryIds = request.CategoryIds.Distinct().ToList();

        var cities = await _dbContext.City.Where(c => cityIds.Contains(c.Id)).ToListAsync();
        if (cities.Count != cityIds.Count)
            return ServiceResult<bool>.Failed("بعض المدن غير موجودة");

        var categories = await _dbContext.ProductCategory.Where(c => categoryIds.Contains(c.Id)).ToListAsync();
        if (categories.Count != categoryIds.Count)
            return ServiceResult<bool>.Failed("بعض التصنيفات غير موجودة");

        line.Name = request.Name;
        line.Notes = request.Notes;
        line.ManagerId = request.ManagerId;

        // replace the sets: remove what's no longer selected, add what's new
        foreach (var c in line.Cities.Where(c => !cityIds.Contains(c.Id)).ToList())
            line.Cities.Remove(c);
        foreach (var c in cities.Where(c => line.Cities.All(x => x.Id != c.Id)))
            line.Cities.Add(c);

        foreach (var c in line.ProductCategories.Where(c => !categoryIds.Contains(c.Id)).ToList())
            line.ProductCategories.Remove(c);
        foreach (var c in categories.Where(c => line.ProductCategories.All(x => x.Id != c.Id)))
            line.ProductCategories.Add(c);

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


        _dbContext.OrderLine.Remove(line);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("OrderLine with Id {OrderLineId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}