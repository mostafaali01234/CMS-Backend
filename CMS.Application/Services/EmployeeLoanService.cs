// Services/EmployeeLoanService.cs
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using CMS.Domain.DTOs;
using CMS.Domain.Enums;
using CMS.Domain.Models;
using CMS.Application.Interfaces.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMS.Application.Services;

public class EmployeeLoanService : IEmployeeLoanService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<EmployeeLoanService> _logger;

    private const int NotesMaxLength = 2000;

    public EmployeeLoanService(
        IAppDbContext dbContext,
        ILogger<EmployeeLoanService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private EmployeeLoanDto ToDto(EmployeeLoan loan)
    {
        return new EmployeeLoanDto
        {
            Id = loan.Id,
            EmployeeId = loan.EmployeeId,
            EmployeeName = loan.Employee?.Name ?? "",
            ApprovedById = loan.ApprovedById,
            ApprovedByName = loan.ApprovedBy?.Name ?? "",
            Amount = loan.Amount,
            MoneySafeId = loan.MoneySafeId,
            MoneySafeName = loan.MoneySafe?.Name ?? "",
            InstallmentCount = loan.InstallmentCount,
            StartDate = loan.StartDate,
            Notes = loan.Notes,
            ProjectId = loan.ProjectId,
            ShiftId = loan.ShiftId,
            ProjectName = loan.Project?.Name ?? "",
            Installments = loan.Installments
                .Where(i => !i.IsDeleted)
                .OrderBy(i => i.InstallmentNumber)
                .Select(i => new LoanInstallmentSummaryDto
                {
                    Id = i.Id,
                    InstallmentNumber = i.InstallmentNumber,
                    DueDate = i.DueDate,
                    Amount = i.Amount,
                    Status = i.Status
                }).ToList(),
            CreatedAtUtc = loan.CreatedAtUtc,
            CreatedBy = loan.CreatedBy ?? "",
            UpdatedAtUtc = loan.UpdatedAtUtc,
            UpdatedBy = loan.UpdatedBy ?? ""
        };
    }

    private IQueryable<EmployeeLoan> WithIncludes(IQueryable<EmployeeLoan> query)
    {
        return query
            .Include(l => l.Employee)
            .Include(l => l.ApprovedBy)
            .Include(l => l.MoneySafe)
            .Include(l => l.Project)
            .Include(l => l.Installments);
    }

    // Centralized validation used by both Create and Update.
    private async Task<string?> ValidateAsync(EmployeeLoanDto dto)
    {
        if (dto is null)
            return "Loan payload is required";

        // --- Employee (required FK) ---
        if (dto.EmployeeId <= 0)
            return "EmployeeId is required";

        var employeeExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == dto.EmployeeId && !e.IsDeleted);

        if (!employeeExists)
            return $"Employee with Id {dto.EmployeeId} was not found";

        // --- Amount ---
        if (dto.Amount <= 0)
            return "Amount must be greater than zero";

        // --- StartDate ---
        if (dto.StartDate == default)
            return "StartDate is required";

        // --- MoneySafeId XOR InstallmentCount ---
        // A loan is either settled directly from a money safe (lump sum, no payroll schedule)
        // or repaid through payroll installments — never both, never neither.
        var hasMoneySafe = dto.MoneySafeId.HasValue && dto.MoneySafeId.Value > 0;
        var hasInstallments = dto.InstallmentCount > 0;

        if (hasMoneySafe == hasInstallments)
            return "A loan must have either MoneySafeId or InstallmentCount set, but not both and not neither";

        if (hasMoneySafe)
        {
            var safe = await _dbContext.MoneySafe
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == dto.MoneySafeId!.Value && !m.IsDeleted);

            if (safe is null)
                return $"Money safe with Id {dto.MoneySafeId!.Value} was not found";

            if (!safe.Active)
                return $"Money safe '{safe.Name}' is not active";
        }
        else
        {
            if (dto.InstallmentCount <= 0)
                return "InstallmentCount must be greater than zero when MoneySafeId is not set";

            if (dto.InstallmentCount > 120)
                return "InstallmentCount cannot exceed 120 (10 years of monthly installments)";
        }

        // --- ApprovedBy (optional FK) ---
        if (dto.ApprovedById.HasValue)
        {
            if (dto.ApprovedById.Value <= 0)
                return "ApprovedById is invalid";

            var approverExists = await _dbContext.Employee
                .AnyAsync(e => e.Id == dto.ApprovedById.Value && !e.IsDeleted);

            if (!approverExists)
                return $"Approver (Employee) with Id {dto.ApprovedById.Value} was not found";

            if (dto.ApprovedById.Value == dto.EmployeeId)
                return "An employee cannot approve their own loan";
        }

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

        // --- Notes (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Length > NotesMaxLength)
            return $"Notes cannot exceed {NotesMaxLength} characters";

        return null; // no validation errors
    }

    // Splits dto.Amount evenly across dto.InstallmentCount monthly installments starting at
    // dto.StartDate. Any rounding remainder (from dividing by a count that doesn't divide evenly)
    // is absorbed into the last installment so the sum always equals the original Amount exactly.
    private List<EmployeeLoanInstallment> GenerateInstallments(EmployeeLoanDto dto)
    {
        var baseAmount = Math.Round(dto.Amount / dto.InstallmentCount, 2, MidpointRounding.ToZero);
        var installments = new List<EmployeeLoanInstallment>();
        decimal runningTotal = 0;

        for (var i = 1; i <= dto.InstallmentCount; i++)
        {
            var isLast = i == dto.InstallmentCount;
            var amount = isLast ? dto.Amount - runningTotal : baseAmount;
            runningTotal += amount;

            installments.Add(new EmployeeLoanInstallment
            {
                InstallmentNumber = i,
                DueDate = dto.StartDate.AddMonths(i - 1),
                Amount = amount,
                Status = InstallmentStatus.Pending
            });
        }

        return installments;
    }

    public async Task<ServiceResult<List<EmployeeLoanDto>>> GetAllAsync()
    {
        var loans = await WithIncludes(_dbContext.EmployeeLoan.Where(z => !z.IsDeleted))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<EmployeeLoanDto>>.Ok(loans.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<EmployeeLoanDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<EmployeeLoanDto?>.Failed("A valid loan Id is required");

        var loan = await WithIncludes(_dbContext.EmployeeLoan.Where(z => !z.IsDeleted && z.Id == id))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (loan is null)
            return ServiceResult<EmployeeLoanDto?>.NotFound("Loan not found");

        return ServiceResult<EmployeeLoanDto?>.Ok(ToDto(loan));
    }

    public async Task<ServiceResult<EmployeeLoanDto>> CreateAsync(EmployeeLoanDto dto)
    {
        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating loan: {Error}", validationError);
            return ServiceResult<EmployeeLoanDto>.Failed(validationError);
        }

        var hasMoneySafe = dto.MoneySafeId.HasValue && dto.MoneySafeId.Value > 0;

        var loan = new EmployeeLoan
        {
            EmployeeId = dto.EmployeeId,
            ApprovedById = dto.ApprovedById,
            Amount = dto.Amount,
            MoneySafeId = hasMoneySafe ? dto.MoneySafeId : null,
            InstallmentCount = hasMoneySafe ? 0 : dto.InstallmentCount,
            StartDate = dto.StartDate,
            Notes = dto.Notes,
            ProjectId = dto.ProjectId,
            ShiftId = dto.ShiftId
        };

        if (!hasMoneySafe)
        {
            loan.Installments = GenerateInstallments(dto);
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

            _dbContext.EmployeeLoan.Add(loan);
            await _dbContext.SaveChangesAsync();

            await dbTransaction.CommitAsync();
        });

        _logger.LogInformation("Loan created with Id {LoanId} for Employee {EmployeeId} ({Mode})",
            loan.Id, loan.EmployeeId, hasMoneySafe ? "money safe" : "installments");

        var created = await WithIncludes(_dbContext.EmployeeLoan)
            .AsNoTracking()
            .FirstAsync(l => l.Id == loan.Id);

        return ServiceResult<EmployeeLoanDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, EmployeeLoanDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Loan payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.EmployeeLoan
            .Include(l => l.Installments)
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        if (existing is null)
            return ServiceResult<bool>.NotFound("Loan not found");

        // Once any installment has actually been deducted or skipped, the loan's repayment
        // schedule is no longer a blank slate — block edits that would regenerate it.
        var hasProgressedInstallments = existing.Installments
            .Any(i => !i.IsDeleted && i.Status != InstallmentStatus.Pending);

        if (hasProgressedInstallments)
            return ServiceResult<bool>.Failed(
                "This loan has installments that are already Deducted or Skipped and can no longer be edited");

        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating loan {LoanId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        var hasMoneySafe = dto.MoneySafeId.HasValue && dto.MoneySafeId.Value > 0;

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

            existing.EmployeeId = dto.EmployeeId;
            existing.ApprovedById = dto.ApprovedById;
            existing.Amount = dto.Amount;
            existing.MoneySafeId = hasMoneySafe ? dto.MoneySafeId : null;
            existing.InstallmentCount = hasMoneySafe ? 0 : dto.InstallmentCount;
            existing.StartDate = dto.StartDate;
            existing.Notes = dto.Notes;
            existing.ProjectId = dto.ProjectId;
            existing.ShiftId = dto.ShiftId;

            // Replace all (still-pending) installments with a freshly generated schedule
            if (existing.Installments is { Count: > 0 })
            {
                _dbContext.EmployeeLoanInstallment.RemoveRange(existing.Installments);
            }

            if (!hasMoneySafe)
            {
                var newInstallments = GenerateInstallments(dto)
                    .Select(i => { i.LoanId = existing.Id; return i; })
                    .ToList();

                await _dbContext.EmployeeLoanInstallment.AddRangeAsync(newInstallments);
            }

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error occurred while updating loan with Id {LoanId}", id);
                throw;
            }

            await dbTransaction.CommitAsync();
        });

        _logger.LogInformation("Loan with Id {LoanId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var loan = await _dbContext.EmployeeLoan
            .Include(l => l.Installments)
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        if (loan is null)
            return ServiceResult<bool>.NotFound("Loan not found");

        var hasDeductedInstallments = loan.Installments
            .Any(i => !i.IsDeleted && i.Status == InstallmentStatus.Deducted);

        if (hasDeductedInstallments)
            return ServiceResult<bool>.Failed(
                "This loan has installments that have already been deducted and cannot be deleted");

        var now = DateTime.UtcNow;

        loan.IsDeleted = true;
        loan.DeletedAtUtc = now;

        foreach (var installment in loan.Installments.Where(i => !i.IsDeleted))
        {
            installment.IsDeleted = true;
            installment.DeletedAtUtc = now;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Loan with Id {LoanId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}