// Services/EmployeePayrollAdjustmentService.cs
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using CMS.Domain.DTOs;
using CMS.Domain.Enums;
using CMS.Domain.Models;
using CMS.Application.Interfaces.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMS.Application.Services;

public class EmployeePayrollAdjustmentService : IEmployeePayrollAdjustmentService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<EmployeePayrollAdjustmentService> _logger;

    private const int ReasonMaxLength = 2000;

    public EmployeePayrollAdjustmentService(
        IAppDbContext dbContext,
        ILogger<EmployeePayrollAdjustmentService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private EmployeePayrollAdjustmentDto ToDto(EmployeePayrollAdjustment a)
    {
        return new EmployeePayrollAdjustmentDto
        {
            Id = a.Id,
            EmployeeId = a.EmployeeId,
            EmployeeName = a.Employee?.Name ?? "",
            OperationDate = a.OperationDate,
            Kind = a.Kind,
            Type = a.Type,
            Amount = a.Amount,
            Reason = a.Reason,
            ApprovedById = a.ApprovedById,
            ApprovedByName = a.ApprovedBy?.Name ?? "",
            CreatedAtUtc = a.CreatedAtUtc,
            CreatedBy = a.CreatedBy ?? "",
            UpdatedAtUtc = a.UpdatedAtUtc,
            UpdatedBy = a.UpdatedBy ?? ""
        };
    }

    private EmployeePayrollAdjustment ToEntity(EmployeePayrollAdjustmentDto dto)
    {
        return new EmployeePayrollAdjustment
        {
            Id = dto.Id,
            EmployeeId = dto.EmployeeId,
            OperationDate = dto.OperationDate,
            Kind = dto.Kind,
            Type = dto.Type,
            Amount = dto.Amount,
            Reason = dto.Reason,
            ApprovedById = dto.ApprovedById
        };
    }

    private IQueryable<EmployeePayrollAdjustment> WithIncludes(IQueryable<EmployeePayrollAdjustment> query)
    {
        return query
            .Include(a => a.Employee)
            .Include(a => a.ApprovedBy);
    }

    // Centralized validation used by both Create and Update.
    private async Task<string?> ValidateAsync(EmployeePayrollAdjustmentDto dto)
    {
        if (dto is null)
            return "Payroll adjustment payload is required";

        // --- Employee (required FK) ---
        if (dto.EmployeeId <= 0)
            return "EmployeeId is required";

        var employeeExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == dto.EmployeeId && !e.IsDeleted);

        if (!employeeExists)
            return $"Employee with Id {dto.EmployeeId} was not found";

        // --- OperationDate ---
        if (dto.OperationDate == default)
            return "OperationDate is required";

        // --- Enums ---
        //if (!Enum.IsDefined(typeof(PayrollAdjustmentEmpKind), dto.EmpKind))
        //    return "EmpKind is not a valid value";

        //if (!Enum.IsDefined(typeof(PayrollAdjustmentMSKind), dto.MSKind))
        //    return "MSKind is not a valid value";

        if (!Enum.IsDefined(typeof(PayrollAdjustmentType), dto.Type))
            return "Type is not a valid value";

        // --- Amount ---
        if (dto.Amount <= 0)
            return "Amount must be greater than zero";

        // --- Reason (required, length only) ---
        if (string.IsNullOrWhiteSpace(dto.Reason))
            return "Reason is required";

        if (dto.Reason.Length > ReasonMaxLength)
            return $"Reason cannot exceed {ReasonMaxLength} characters";

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
                return "An employee cannot approve their own payroll adjustment";
        }

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<EmployeePayrollAdjustmentDto>>> GetAllAsync()
    {
        var items = await WithIncludes(_dbContext.EmployeePayrollAdjustment.Where(z => !z.IsDeleted))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<EmployeePayrollAdjustmentDto>>.Ok(items.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<EmployeePayrollAdjustmentDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<EmployeePayrollAdjustmentDto?>.Failed("A valid Id is required");

        var item = await WithIncludes(_dbContext.EmployeePayrollAdjustment.Where(z => !z.IsDeleted && z.Id == id))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (item is null)
            return ServiceResult<EmployeePayrollAdjustmentDto?>.NotFound("Payroll adjustment not found");

        return ServiceResult<EmployeePayrollAdjustmentDto?>.Ok(ToDto(item));
    }

    public async Task<ServiceResult<EmployeePayrollAdjustmentDto>> CreateAsync(EmployeePayrollAdjustmentDto dto)
    {
        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating payroll adjustment: {Error}", validationError);
            return ServiceResult<EmployeePayrollAdjustmentDto>.Failed(validationError);
        }
        switch (dto.Type)
        {
            case PayrollAdjustmentType.Incentive:
            case PayrollAdjustmentType.Commission:
            case PayrollAdjustmentType.Overtime:
            case PayrollAdjustmentType.Bonus:
                dto.Kind = PayrollAdjustmentKind.Addition;
                break;
            case PayrollAdjustmentType.LoanInstallment:
            case PayrollAdjustmentType.Absence:
            case PayrollAdjustmentType.Penalty:
            case PayrollAdjustmentType.Insurance:
                dto.Kind = PayrollAdjustmentKind.Deduction;
                break;
            default:
                return ServiceResult<EmployeePayrollAdjustmentDto>.Failed("Invalid payroll adjustment type");
        }

        var entity = ToEntity(dto);

        _dbContext.EmployeePayrollAdjustment.Add(entity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Payroll adjustment created with Id {AdjustmentId} for Employee {EmployeeId}", entity.Id, entity.EmployeeId);

        var created = await WithIncludes(_dbContext.EmployeePayrollAdjustment)
            .AsNoTracking()
            .FirstAsync(a => a.Id == entity.Id);

        return ServiceResult<EmployeePayrollAdjustmentDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, EmployeePayrollAdjustmentDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Payroll adjustment payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.EmployeePayrollAdjustment.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Payroll adjustment not found");

        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating payroll adjustment {AdjustmentId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        existing.EmployeeId = dto.EmployeeId;
        existing.OperationDate = dto.OperationDate;
        existing.Kind = dto.Kind;
        existing.Type = dto.Type;
        existing.Amount = dto.Amount;
        existing.Reason = dto.Reason;
        existing.ApprovedById = dto.ApprovedById;
        switch (dto.Type)
        {
            case PayrollAdjustmentType.Incentive:
            case PayrollAdjustmentType.Commission:
            case PayrollAdjustmentType.Overtime:
            case PayrollAdjustmentType.Bonus:
                existing.Kind = PayrollAdjustmentKind.Addition;
                break;
            case PayrollAdjustmentType.LoanInstallment:
            case PayrollAdjustmentType.Absence:
            case PayrollAdjustmentType.Penalty:
            case PayrollAdjustmentType.Insurance:
                existing.Kind = PayrollAdjustmentKind.Deduction;
                break;
            default:
                return ServiceResult<bool>.Failed("Invalid payroll adjustment type");
        }
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating payroll adjustment with Id {AdjustmentId}", id);
            throw;
        }

        _logger.LogInformation("Payroll adjustment with Id {AdjustmentId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var entity = await _dbContext.EmployeePayrollAdjustment.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        if (entity is null)
            return ServiceResult<bool>.NotFound("Payroll adjustment not found");

        entity.IsDeleted = true;
        entity.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Payroll adjustment with Id {AdjustmentId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}