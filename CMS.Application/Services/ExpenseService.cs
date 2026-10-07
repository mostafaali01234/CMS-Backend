// Services/ExpenseService.cs
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using CMS.Domain.DTOs;
using CMS.Domain.Models;
using CMS.Application.Interfaces.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CMS.Application.Services;

public class ExpenseService : IExpenseService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<ExpenseService> _logger;

    private const int NotesMaxLength = 2000;

    public ExpenseService(
        IAppDbContext dbContext,
        ILogger<ExpenseService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private ExpenseDto ToDto(Expense e)
    {
        ExpenseDataDto? data = null;

        if (!string.IsNullOrWhiteSpace(e.Data))
        {
            try
            {
                data = JsonSerializer.Deserialize<ExpenseDataDto>(e.Data);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize Data JSON for expense {ExpenseId}", e.Id);
            }
        }

        return new ExpenseDto
        {
            Id = e.Id,
            ExpenseTypeId = e.ExpenseTypeId,
            ExpenseTypeName = e.ExpenseType?.Name ?? "",
            Amount = e.Amount,
            AmountCurrency = e.AmountCurrency,
            Notes = e.Notes,
            Data = data,
            MoneySafeId = e.MoneySafeId,
            MoneySafeName = e.MoneySafe?.Name ?? "",
            AuditorId = e.AuditorId,
            AuditorName = e.Auditor?.Name ?? "",
            ProjectId = e.ProjectId,
            ProjectName = e.Project?.Name ?? "",
            CreatedAtUtc = e.CreatedAtUtc,
            CreatedBy = e.CreatedBy ?? "",
            UpdatedAtUtc = e.UpdatedAtUtc,
            UpdatedBy = e.UpdatedBy ?? "",
            ShiftId = e.ShiftId
        };
    }

    private IQueryable<Expense> WithIncludes(IQueryable<Expense> query)
    {
        return query
            .Include(e => e.ExpenseType)
            .Include(e => e.MoneySafe)
            .Include(e => e.Auditor)
            .Include(e => e.Project);
    }

    // Centralized validation used by both Create and Update.
    private async Task<string?> ValidateAsync(ExpenseDto dto)
    {
        if (dto is null)
            return "Expense payload is required";

        // --- ExpenseType (required FK) ---
        if (dto.ExpenseTypeId <= 0)
            return "ExpenseTypeId is required";

        var expenseTypeExists = await _dbContext.ExpenseType
            .AnyAsync(t => t.Id == dto.ExpenseTypeId);

        if (!expenseTypeExists)
            return $"Expense type with Id {dto.ExpenseTypeId} was not found";

        // --- Amount ---
        if (dto.Amount <= 0)
            return "Amount must be greater than zero";

        if (dto.AmountCurrency < 0)
            return "AmountCurrency cannot be negative";

        // --- Notes (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Length > NotesMaxLength)
            return $"Notes cannot exceed {NotesMaxLength} characters";

        // --- MoneySafe (required FK, must be active) ---
        if (dto.MoneySafeId <= 0)
            return "MoneySafeId is required";

        var safe = await _dbContext.MoneySafe
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == dto.MoneySafeId && !m.IsDeleted);

        if (safe is null)
            return $"Money safe with Id {dto.MoneySafeId} was not found";

        if (!safe.Active)
            return $"Money safe '{safe.Name}' is not active";

        // --- Auditor (required FK) ---
        if (dto.AuditorId <= 0)
            return "AuditorId is required";

        var auditorExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == dto.AuditorId && !e.IsDeleted);

        if (!auditorExists)
            return $"Auditor (Employee) with Id {dto.AuditorId} was not found";

        // --- Project (optional FK) ---
        if (dto.ProjectId.HasValue)
        {
            if (dto.ProjectId.Value <= 0)
                return "ProjectId is invalid";

            var projectExists = await _dbContext.Project
                .AnyAsync(p => p.Id == dto.ProjectId.Value);

            if (!projectExists)
                return $"Project with Id {dto.ProjectId.Value} was not found";
        }

        // --- Data (optional car-maintenance payload) ---
        if (dto.Data is not null)
        {
            var data = dto.Data;

            if (data.CarId.HasValue)
            {
                if (data.CarId.Value <= 0)
                    return "Data.CarId is invalid";

                var carExists = await _dbContext.Car
                    .AnyAsync(c => c.Id == data.CarId.Value && !c.IsDeleted);

                if (!carExists)
                    return $"Car with Id {data.CarId.Value} was not found";
            }

            if (data.OilStartKm.HasValue && data.OilStartKm.Value < 0)
                return "Data.OilStartKm cannot be negative";

            if (data.OilEndKm.HasValue && data.OilEndKm.Value < 0)
                return "Data.OilEndKm cannot be negative";

            if (data.OilStartKm.HasValue && data.OilEndKm.HasValue && data.OilEndKm.Value < data.OilStartKm.Value)
                return "Data.OilEndKm cannot be less than Data.OilStartKm";

            if (data.GasCurrentKm.HasValue && data.GasCurrentKm.Value < 0)
                return "Data.GasCurrentKm cannot be negative";

            if (data.GasLitresCount.HasValue && data.GasLitresCount.Value <= 0)
                return "Data.GasLitresCount must be greater than zero when provided";

            // If any car-maintenance field is filled in, CarId should be provided too
            var hasAnyCarField = data.OilStartKm.HasValue || data.OilEndKm.HasValue
                || data.GasCurrentKm.HasValue || data.GasLitresCount.HasValue;

            if (hasAnyCarField && !data.CarId.HasValue)
                return "Data.CarId is required when oil/gas fields are provided";
        }

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<ExpenseDto>>> GetAllAsync()
    {
        var items = await WithIncludes(_dbContext.Expense.Where(z => !z.IsDeleted))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<ExpenseDto>>.Ok(items.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<ExpenseDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<ExpenseDto?>.Failed("A valid expense Id is required");

        var item = await WithIncludes(_dbContext.Expense.Where(z => !z.IsDeleted && z.Id == id))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (item is null)
            return ServiceResult<ExpenseDto?>.NotFound("Expense not found");

        return ServiceResult<ExpenseDto?>.Ok(ToDto(item));
    }

    public async Task<ServiceResult<ExpenseDto>> CreateAsync(ExpenseDto dto)
    {
        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating expense: {Error}", validationError);
            return ServiceResult<ExpenseDto>.Failed(validationError);
        }

        var entity = new Expense
        {
            ExpenseTypeId = dto.ExpenseTypeId,
            Amount = dto.Amount,
            AmountCurrency = dto.AmountCurrency,
            Notes = dto.Notes,
            Data = dto.Data is not null ? JsonSerializer.Serialize(dto.Data) : string.Empty,
            MoneySafeId = dto.MoneySafeId,
            AuditorId = dto.AuditorId,
            ProjectId = dto.ProjectId
        };

        _dbContext.Expense.Add(entity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Expense created with Id {ExpenseId}", entity.Id);

        var created = await WithIncludes(_dbContext.Expense)
            .AsNoTracking()
            .FirstAsync(e => e.Id == entity.Id);

        return ServiceResult<ExpenseDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, ExpenseDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Expense payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.Expense.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Expense not found");

        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating expense {ExpenseId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        existing.ExpenseTypeId = dto.ExpenseTypeId;
        existing.Amount = dto.Amount;
        existing.AmountCurrency = dto.AmountCurrency;
        existing.Notes = dto.Notes;
        existing.Data = dto.Data is not null ? JsonSerializer.Serialize(dto.Data) : string.Empty;
        existing.MoneySafeId = dto.MoneySafeId;
        existing.AuditorId = dto.AuditorId;
        existing.ProjectId = dto.ProjectId;
        existing.ShiftId = dto.ShiftId;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating expense with Id {ExpenseId}", id);
            throw;
        }

        _logger.LogInformation("Expense with Id {ExpenseId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var entity = await _dbContext.Expense.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        if (entity is null)
            return ServiceResult<bool>.NotFound("Expense not found");

        entity.IsDeleted = true;
        entity.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Expense with Id {ExpenseId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}