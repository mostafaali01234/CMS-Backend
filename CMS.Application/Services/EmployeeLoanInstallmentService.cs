// Services/EmployeeLoanInstallmentService.cs
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using CMS.Domain.Enums;
using CMS.Domain.Models;
using CMS.Domain.Models.DTOs;
using CMS.Application.Interfaces.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMS.Application.Services;

public class EmployeeLoanInstallmentService : IEmployeeLoanInstallmentService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<EmployeeLoanInstallmentService> _logger;

    public EmployeeLoanInstallmentService(
        IAppDbContext dbContext,
        ILogger<EmployeeLoanInstallmentService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private EmployeeLoanInstallmentDto ToDto(EmployeeLoanInstallment i)
    {
        return new EmployeeLoanInstallmentDto
        {
            Id = i.Id,
            LoanId = i.LoanId,
            EmployeeName = i.Loan?.Employee?.Name ?? "",
            InstallmentNumber = i.InstallmentNumber,
            DueDate = i.DueDate,
            Amount = i.Amount,
            Status = i.Status,
            DeductionId = i.DeductionId,
            CreatedAtUtc = i.CreatedAtUtc,
            CreatedBy = i.CreatedBy ?? "",
            UpdatedAtUtc = i.UpdatedAtUtc,
            UpdatedBy = i.UpdatedBy ?? ""
        };
    }

    private IQueryable<EmployeeLoanInstallment> WithIncludes(IQueryable<EmployeeLoanInstallment> query)
    {
        return query
            .Include(i => i.Loan)
                .ThenInclude(l => l!.Employee)
            .Include(i => i.Deduction);
    }

    // Centralized validation used by both Create and Update.
    // Only Status and DeductionId are expected to actually change here — the installment's
    // identity (LoanId, number, due date, amount) is normally fixed by EmployeeLoanService
    // when the schedule is generated.
    private async Task<string?> ValidateAsync(EmployeeLoanInstallmentDto dto, bool isUpdate, long? currentId = null)
    {
        if (dto is null)
            return "Installment payload is required";

        // --- Loan (required FK) ---
        if (dto.LoanId <= 0)
            return "LoanId is required";

        var loanExists = await _dbContext.EmployeeLoan
            .AnyAsync(l => l.Id == dto.LoanId && !l.IsDeleted);

        if (!loanExists)
            return $"Loan with Id {dto.LoanId} was not found";

        // --- InstallmentNumber (required, unique within the loan) ---
        if (dto.InstallmentNumber <= 0)
            return "InstallmentNumber must be greater than zero";

        var numberQuery = _dbContext.EmployeeLoanInstallment
            .Where(i => !i.IsDeleted && i.LoanId == dto.LoanId && i.InstallmentNumber == dto.InstallmentNumber);

        if (isUpdate && currentId.HasValue)
            numberQuery = numberQuery.Where(i => i.Id != currentId.Value);

        if (await numberQuery.AnyAsync())
            return $"Installment number {dto.InstallmentNumber} already exists for loan {dto.LoanId}";

        // --- DueDate ---
        if (dto.DueDate == default)
            return "DueDate is required";

        // --- Amount ---
        if (dto.Amount <= 0)
            return "Amount must be greater than zero";

        // --- Status ---
        if (!Enum.IsDefined(typeof(InstallmentStatus), dto.Status))
            return "Status is not a valid value";

        // --- DeductionId (optional FK, only valid once actually deducted) ---
        if (dto.DeductionId.HasValue)
        {
            if (dto.DeductionId.Value <= 0)
                return "DeductionId is invalid";

            var deductionExists = await _dbContext.EmployeePayrollAdjustment
                .AnyAsync(a => a.Id == dto.DeductionId.Value && !a.IsDeleted);

            if (!deductionExists)
                return $"Payroll adjustment with Id {dto.DeductionId.Value} was not found";

            if (dto.Status != InstallmentStatus.Deducted)
                return "DeductionId can only be set when Status is Deducted";
        }

        if (dto.Status == InstallmentStatus.Deducted && !dto.DeductionId.HasValue)
            return "DeductionId is required when Status is Deducted";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<EmployeeLoanInstallmentDto>>> GetAllAsync()
    {
        var items = await WithIncludes(_dbContext.EmployeeLoanInstallment.Where(z => !z.IsDeleted))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<EmployeeLoanInstallmentDto>>.Ok(items.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<EmployeeLoanInstallmentDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<EmployeeLoanInstallmentDto?>.Failed("A valid Id is required");

        var item = await WithIncludes(_dbContext.EmployeeLoanInstallment.Where(z => !z.IsDeleted && z.Id == id))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (item is null)
            return ServiceResult<EmployeeLoanInstallmentDto?>.NotFound("Installment not found");

        return ServiceResult<EmployeeLoanInstallmentDto?>.Ok(ToDto(item));
    }

    // Note: in practice, installments are normally created in bulk by EmployeeLoanService
    // when a loan is created. This direct Create path exists to satisfy the interface but
    // is mainly useful for adding a one-off extra installment to an existing loan.
    public async Task<ServiceResult<EmployeeLoanInstallmentDto>> CreateAsync(EmployeeLoanInstallmentDto dto)
    {
        var validationError = await ValidateAsync(dto, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating installment: {Error}", validationError);
            return ServiceResult<EmployeeLoanInstallmentDto>.Failed(validationError);
        }

        var entity = new EmployeeLoanInstallment
        {
            LoanId = dto.LoanId,
            InstallmentNumber = dto.InstallmentNumber,
            DueDate = dto.DueDate,
            Amount = dto.Amount,
            Status = dto.Status,
            DeductionId = dto.DeductionId
        };

        _dbContext.EmployeeLoanInstallment.Add(entity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Installment created with Id {InstallmentId} for Loan {LoanId}", entity.Id, entity.LoanId);

        var created = await WithIncludes(_dbContext.EmployeeLoanInstallment)
            .AsNoTracking()
            .FirstAsync(i => i.Id == entity.Id);

        return ServiceResult<EmployeeLoanInstallmentDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, EmployeeLoanInstallmentDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Installment payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.EmployeeLoanInstallment.FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Installment not found");

        // Once deducted, an installment is historical payroll fact and should not be edited.
        if (existing.Status == InstallmentStatus.Deducted)
            return ServiceResult<bool>.Failed("This installment has already been deducted and cannot be edited");

        var validationError = await ValidateAsync(dto, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating installment {InstallmentId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        existing.LoanId = dto.LoanId;
        existing.InstallmentNumber = dto.InstallmentNumber;
        existing.DueDate = dto.DueDate;
        existing.Amount = dto.Amount;
        existing.Status = dto.Status;
        existing.DeductionId = dto.DeductionId;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating installment with Id {InstallmentId}", id);
            throw;
        }

        _logger.LogInformation("Installment with Id {InstallmentId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var entity = await _dbContext.EmployeeLoanInstallment.FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
        if (entity is null)
            return ServiceResult<bool>.NotFound("Installment not found");

        if (entity.Status == InstallmentStatus.Deducted)
            return ServiceResult<bool>.Failed("This installment has already been deducted and cannot be deleted");

        entity.IsDeleted = true;
        entity.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Installment with Id {InstallmentId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}