// Services/MoneySafeTransactionService.cs
using CMS.Application.Interfaces.Configuration;
using CMS.Domain.Models;
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using CMS.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CMS.Application.Services;

public class MoneySafeTransactionService : IMoneySafeTransactionService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<MoneySafeTransactionService> _logger;

    private const int NotesMaxLength = 2000;

    public MoneySafeTransactionService(
        IAppDbContext dbContext,
        ILogger<MoneySafeTransactionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private MoneySafeTransactionDto ToDto(MoneySafeTransaction t)
    {
        return new MoneySafeTransactionDto
        {
            Id = t.Id,
            TransactionDate = t.TransactionDate,
            TransactionNotes = t.TransactionNotes,
            TransactionAmount = t.TransactionAmount,
            TransactionAmountCurrency = t.TransactionAmountCurrency,
            TransactionType = t.TransactionType,
            AuditorId = t.AuditorId,
            AuditorName = t.Auditor?.Name ?? "",
            OutMoneySafeId = t.OutMoneySafeId,
            OutMoneySafeName = t.OutMoneySafe?.Name ?? "",
            InMoneySafeId = t.InMoneySafeId,
            InMoneySafeName = t.InMoneySafe?.Name ?? "",
            ProjectId = t.ProjectId,
            ProjectName = t.Project?.Name ?? "",
            CreatedAtUtc = t.CreatedAtUtc,
            CreatedBy = t.CreatedBy ?? "",
            UpdatedAtUtc = t.UpdatedAtUtc,
            UpdatedBy = t.UpdatedBy ?? "",
            ShiftId = t.ShiftId,
        };
    }

    private MoneySafeTransaction ToEntity(MoneySafeTransactionDto dto)
    {
        return new MoneySafeTransaction
        {
            Id = dto.Id,
            TransactionDate = dto.TransactionDate,
            TransactionNotes = dto.TransactionNotes,
            TransactionAmount = dto.TransactionAmount,
            TransactionAmountCurrency = dto.TransactionAmountCurrency,
            TransactionType = dto.TransactionType,
            AuditorId = dto.AuditorId,
            OutMoneySafeId = dto.OutMoneySafeId,
            InMoneySafeId = dto.InMoneySafeId,
            ProjectId = dto.ProjectId,
            ShiftId = dto.ShiftId,
        };
    }

    private IQueryable<MoneySafeTransaction> WithIncludes(IQueryable<MoneySafeTransaction> query)
    {
        return query
            .Include(t => t.Auditor)
            .Include(t => t.OutMoneySafe)
            .Include(t => t.InMoneySafe)
            .Include(t => t.Project);
    }

    // Centralized validation used by both Create and Update.
    private async Task<string?> ValidateAsync(MoneySafeTransactionDto dto)
    {
        if (dto is null)
            return "Transaction payload is required";

        // --- TransactionDate ---
        if (dto.TransactionDate == default)
            return "TransactionDate is required";

        // --- TransactionType ---
        if (!Enum.IsDefined(typeof(MoneySafeTransactionType), dto.TransactionType))
            return "TransactionType is not a valid value";

        // --- Amounts ---
        if (dto.TransactionAmount <= 0)
            return "TransactionAmount must be greater than zero";

        if (dto.TransactionAmountCurrency < 0)
            return "TransactionAmountCurrency cannot be negative";

        // --- Notes (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(dto.TransactionNotes) && dto.TransactionNotes.Length > NotesMaxLength)
            return $"TransactionNotes cannot exceed {NotesMaxLength} characters";

        // --- Auditor (required FK) ---
        if (dto.AuditorId <= 0)
            return "AuditorId is required";

        var auditorExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == dto.AuditorId && !e.IsDeleted);

        if (!auditorExists)
            return $"Auditor (Employee) with Id {dto.AuditorId} was not found";

        // --- OutMoneySafe (required FK, must be active) ---
        if (dto.OutMoneySafeId <= 0)
            return "OutMoneySafeId is required";

        var outSafe = await _dbContext.MoneySafe
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == dto.OutMoneySafeId && !m.IsDeleted);

        if (outSafe is null)
            return $"Out money safe with Id {dto.OutMoneySafeId} was not found";

        if (!outSafe.Active)
            return $"Out money safe '{outSafe.Name}' is not active";

        // --- InMoneySafe (required FK, must be active) ---
        if (dto.InMoneySafeId <= 0)
            return "InMoneySafeId is required";

        var inSafe = await _dbContext.MoneySafe
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == dto.InMoneySafeId && !m.IsDeleted);

        if (inSafe is null)
            return $"In money safe with Id {dto.InMoneySafeId} was not found";

        if (!inSafe.Active)
            return $"In money safe '{inSafe.Name}' is not active";

        // --- Out and In must differ ---
        if (dto.OutMoneySafeId == dto.InMoneySafeId)
            return "OutMoneySafeId and InMoneySafeId cannot be the same safe";

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

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<MoneySafeTransactionDto>>> GetAllAsync()
    {
        var items = await WithIncludes(_dbContext.MoneySafeTransaction.Where(z => !z.IsDeleted))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<MoneySafeTransactionDto>>.Ok(items.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<MoneySafeTransactionDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<MoneySafeTransactionDto?>.Failed("A valid transaction Id is required");

        var item = await WithIncludes(_dbContext.MoneySafeTransaction.Where(z => !z.IsDeleted && z.Id == id))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (item is null)
            return ServiceResult<MoneySafeTransactionDto?>.NotFound("Transaction not found");

        return ServiceResult<MoneySafeTransactionDto?>.Ok(ToDto(item));
    }

    public async Task<ServiceResult<MoneySafeTransactionDto>> CreateAsync(MoneySafeTransactionDto dto)
    {
        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating money safe transaction: {Error}", validationError);
            return ServiceResult<MoneySafeTransactionDto>.Failed(validationError);
        }

        var entity = ToEntity(dto);

        _dbContext.MoneySafeTransaction.Add(entity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Money safe transaction created with Id {TransactionId}", entity.Id);

        var created = await WithIncludes(_dbContext.MoneySafeTransaction)
            .AsNoTracking()
            .FirstAsync(t => t.Id == entity.Id);

        return ServiceResult<MoneySafeTransactionDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, MoneySafeTransactionDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Transaction payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.MoneySafeTransaction.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Transaction not found");

        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating money safe transaction {TransactionId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        // Update mutable fields explicitly rather than blindly overwriting audit/soft-delete fields
        existing.TransactionDate = dto.TransactionDate;
        existing.TransactionNotes = dto.TransactionNotes;
        existing.TransactionAmount = dto.TransactionAmount;
        existing.TransactionAmountCurrency = dto.TransactionAmountCurrency;
        existing.TransactionType = dto.TransactionType;
        existing.AuditorId = dto.AuditorId;
        existing.OutMoneySafeId = dto.OutMoneySafeId;
        existing.InMoneySafeId = dto.InMoneySafeId;
        existing.ProjectId = dto.ProjectId;
        existing.ShiftId = dto.ShiftId;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating money safe transaction with Id {TransactionId}", id);
            throw;
        }

        _logger.LogInformation("Money safe transaction with Id {TransactionId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var entity = await _dbContext.MoneySafeTransaction.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        if (entity is null)
            return ServiceResult<bool>.NotFound("Transaction not found");

        entity.IsDeleted = true;
        entity.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Money safe transaction with Id {TransactionId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}