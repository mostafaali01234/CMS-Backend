// Services/EmployeePayrollService.cs
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using CMS.Domain.DTOs;
using CMS.Domain.Enums;
using CMS.Domain.Models;
using CMS.Application.Interfaces.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMS.Application.Services;

public class EmployeePayrollService : IEmployeePayrollService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<EmployeePayrollService> _logger;

    public EmployeePayrollService(
        IAppDbContext dbContext,
        ILogger<EmployeePayrollService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private EmployeePayrollDto ToDto(EmployeePayroll p)
    {
        return new EmployeePayrollDto
        {
            Id = p.Id,
            EmployeeId = p.EmployeeId,
            EmployeeName = p.Employee?.Name ?? "",
            Month = p.Month,
            Year = p.Year,
            BaseSalary = p.BaseSalary,
            SalesCommissionTotal = p.SalesCommissionTotal,
            TechCommissionTotal = p.TechCommissionTotal,
            LoansTotal = p.LoansTotal,
            BonusTotal = p.BonusTotal,
            DeductionsTotal = p.DeductionsTotal,
            LunchTotal = p.LunchTotal,
            NetTotal = p.NetTotal,
            Status = p.Status,
            RevisedById = p.RevisedById,
            RevisedByName = p.RevisedBy?.Name ?? "",
            MoneySafeId = p.MoneySafeId,
            MoneySafeName = p.MoneySafe?.Name ?? "",
            WithdrawnAt = p.WithdrawnAt,
            WithdrawnById = p.WithdrawnById,
            WithdrawnByName = p.WithdrawnBy?.Name ?? "",
            CreatedAtUtc = p.CreatedAtUtc,
            CreatedBy = p.CreatedBy ?? "",
            UpdatedAtUtc = p.UpdatedAtUtc,
            UpdatedBy = p.UpdatedBy ?? ""
        };
    }

    private IQueryable<EmployeePayroll> WithIncludes(IQueryable<EmployeePayroll> query)
    {
        return query
            .Include(p => p.Employee)
            .Include(p => p.RevisedBy)
            .Include(p => p.MoneySafe)
            .Include(p => p.WithdrawnBy);
    }

    // NetTotal = BaseSalary + SalesCommissionTotal + TechCommissionTotal + BonusTotal
    //            - LoansTotal - DeductionsTotal - LunchTotal
    // ASSUMPTION: LoansTotal, DeductionsTotal and LunchTotal are all stored as positive
    // numbers representing amounts subtracted from pay (loan installment, deductions,
    // and lunch cost recovered from the employee). If LunchTotal is actually an allowance
    // ADDED to pay rather than deducted, this sign needs flipping.
    private decimal ComputeNetTotal(EmployeePayrollDto dto)
    {
        return Math.Round(
            dto.BaseSalary
            + dto.SalesCommissionTotal
            + dto.TechCommissionTotal
            + dto.BonusTotal
            - dto.LoansTotal
            - dto.DeductionsTotal
            - dto.LunchTotal,
            2);
    }

    // Centralized validation used by both Create and Update.
    private async Task<string?> ValidateAsync(EmployeePayrollDto dto, bool isUpdate, long? currentId = null)
    {
        if (dto is null)
            return "Payroll payload is required";

        // --- Employee (required FK) ---
        if (dto.EmployeeId <= 0)
            return "EmployeeId is required";

        var employeeExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == dto.EmployeeId && !e.IsDeleted);

        if (!employeeExists)
            return $"Employee with Id {dto.EmployeeId} was not found";

        // --- Month / Year ---
        if (dto.Month < 1 || dto.Month > 12)
            return "Month must be between 1 and 12";

        if (dto.Year < 2000 || dto.Year > DateTime.UtcNow.Year + 1)
            return $"Year must be between 2000 and {DateTime.UtcNow.Year + 1}";

        // --- Uniqueness: one payroll per (Employee, Month, Year) ---
        var periodQuery = _dbContext.EmployeePayroll.Where(p =>
            !p.IsDeleted && p.EmployeeId == dto.EmployeeId && p.Month == dto.Month && p.Year == dto.Year);

        if (isUpdate && currentId.HasValue)
            periodQuery = periodQuery.Where(p => p.Id != currentId.Value);

        if (await periodQuery.AnyAsync())
            return $"A payroll record for employee {dto.EmployeeId} for {dto.Month}/{dto.Year} already exists";

        // --- Amount fields (all non-negative; sign/direction is baked into NetTotal calc) ---
        if (dto.BaseSalary < 0)
            return "BaseSalary cannot be negative";

        if (dto.SalesCommissionTotal < 0)
            return "SalesCommissionTotal cannot be negative";

        if (dto.TechCommissionTotal < 0)
            return "TechCommissionTotal cannot be negative";

        if (dto.LoansTotal < 0)
            return "LoansTotal cannot be negative";

        if (dto.BonusTotal < 0)
            return "BonusTotal cannot be negative";

        if (dto.DeductionsTotal < 0)
            return "DeductionsTotal cannot be negative";

        if (dto.LunchTotal < 0)
            return "LunchTotal cannot be negative";

        // --- Status ---
        if (!Enum.IsDefined(typeof(PayrollStatus), dto.Status))
            return "Status is not a valid value";

        // --- RevisedBy (optional FK) ---
        if (dto.RevisedById.HasValue)
        {
            if (dto.RevisedById.Value <= 0)
                return "RevisedById is invalid";

            var reviserExists = await _dbContext.Employee
                .AnyAsync(e => e.Id == dto.RevisedById.Value && !e.IsDeleted);

            if (!reviserExists)
                return $"Reviser (Employee) with Id {dto.RevisedById.Value} was not found";
        }

        // --- MoneySafe (optional FK, required once withdrawn) ---
        if (dto.MoneySafeId.HasValue)
        {
            if (dto.MoneySafeId.Value <= 0)
                return "MoneySafeId is invalid";

            var safe = await _dbContext.MoneySafe
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == dto.MoneySafeId.Value && !m.IsDeleted);

            if (safe is null)
                return $"Money safe with Id {dto.MoneySafeId.Value} was not found";

            if (!safe.Active)
                return $"Money safe '{safe.Name}' is not active";
        }

        // --- Withdrawal consistency ---
        if (dto.WithdrawnAt.HasValue || dto.WithdrawnById.HasValue)
        {
            if (!dto.WithdrawnAt.HasValue || !dto.WithdrawnById.HasValue)
                return "WithdrawnAt and WithdrawnById must both be set together";

            if (!dto.MoneySafeId.HasValue)
                return "MoneySafeId is required once the payroll has been withdrawn";

            var withdrawerExists = await _dbContext.Employee
                .AnyAsync(e => e.Id == dto.WithdrawnById!.Value && !e.IsDeleted);

            if (!withdrawerExists)
                return $"Withdrawer (Employee) with Id {dto.WithdrawnById!.Value} was not found";
        }

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<EmployeePayrollDto>>> GetAllAsync()
    {
        var items = await WithIncludes(_dbContext.EmployeePayroll.Where(z => !z.IsDeleted))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<EmployeePayrollDto>>.Ok(items.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<EmployeePayrollDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<EmployeePayrollDto?>.Failed("A valid payroll Id is required");

        var item = await WithIncludes(_dbContext.EmployeePayroll.Where(z => !z.IsDeleted && z.Id == id))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (item is null)
            return ServiceResult<EmployeePayrollDto?>.NotFound("Payroll record not found");

        return ServiceResult<EmployeePayrollDto?>.Ok(ToDto(item));
    }
    
    public async Task<ServiceResult<List<EmployeePayrollDto>>> GetByEmployeeIdAsync(long employeeId)
    {
        if (employeeId <= 0)
            return ServiceResult<List<EmployeePayrollDto>>.Failed("A valid employee Id is required");

        var items = await WithIncludes(_dbContext.EmployeePayroll.Where(z => !z.IsDeleted && z.EmployeeId == employeeId))
            .AsNoTracking()
            .ToListAsync();

        if (items is null)
            return ServiceResult<List<EmployeePayrollDto>>.NotFound("Payroll record not found");

        return ServiceResult<List<EmployeePayrollDto>>.Ok(items.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<EmployeePayrollDto?>> GetByEmployeeIdMonthYearAsync(long employeeId, int month, int year)
    {
        if (employeeId <= 0)
            return ServiceResult<EmployeePayrollDto?>.Failed("A valid employee Id is required");

        if (month < 1 || month > 12)
            return ServiceResult<EmployeePayrollDto?>.Failed("Month must be between 1 and 12");

        if (year < 2000 || year > 2100)
            return ServiceResult<EmployeePayrollDto?>.Failed("Year is not valid");

        var item = await WithIncludes(_dbContext.EmployeePayroll
            .Where(z => !z.IsDeleted && z.EmployeeId == employeeId && z.Month == month && z.Year == year))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (item is null)
            return ServiceResult<EmployeePayrollDto?>.NotFound("Payroll record not found");

        return ServiceResult<EmployeePayrollDto?>.Ok(ToDto(item));
    }

    public async Task<ServiceResult<EmployeePayrollDto>> CreateAsync(EmployeePayrollDto dto)
    {
        var validationError = await ValidateAsync(dto, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating payroll: {Error}", validationError);
            return ServiceResult<EmployeePayrollDto>.Failed(validationError);
        }

        dto.NetTotal = ComputeNetTotal(dto);

        var entity = new EmployeePayroll
        {
            EmployeeId = dto.EmployeeId,
            Month = dto.Month,
            Year = dto.Year,
            BaseSalary = dto.BaseSalary,
            SalesCommissionTotal = dto.SalesCommissionTotal,
            TechCommissionTotal = dto.TechCommissionTotal,
            LoansTotal = dto.LoansTotal,
            BonusTotal = dto.BonusTotal,
            DeductionsTotal = dto.DeductionsTotal,
            LunchTotal = dto.LunchTotal,
            NetTotal = dto.NetTotal,
            Status = dto.Status,
            RevisedById = dto.RevisedById,
            MoneySafeId = dto.MoneySafeId,
            WithdrawnAt = dto.WithdrawnAt,
            WithdrawnById = dto.WithdrawnById
        };

        _dbContext.EmployeePayroll.Add(entity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Payroll created with Id {PayrollId} for Employee {EmployeeId} ({Month}/{Year})",
            entity.Id, entity.EmployeeId, entity.Month, entity.Year);

        var created = await WithIncludes(_dbContext.EmployeePayroll)
            .AsNoTracking()
            .FirstAsync(p => p.Id == entity.Id);

        return ServiceResult<EmployeePayrollDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, EmployeePayrollDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Payroll payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.EmployeePayroll.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Payroll record not found");

        // Once withdrawn, the payroll record is a closed financial document.
        if (existing.WithdrawnAt.HasValue)
            return ServiceResult<bool>.Failed("This payroll has already been withdrawn and cannot be edited");

        var validationError = await ValidateAsync(dto, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating payroll {PayrollId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        dto.NetTotal = ComputeNetTotal(dto);

        existing.EmployeeId = dto.EmployeeId;
        existing.Month = dto.Month;
        existing.Year = dto.Year;
        existing.BaseSalary = dto.BaseSalary;
        existing.SalesCommissionTotal = dto.SalesCommissionTotal;
        existing.TechCommissionTotal = dto.TechCommissionTotal;
        existing.LoansTotal = dto.LoansTotal;
        existing.BonusTotal = dto.BonusTotal;
        existing.DeductionsTotal = dto.DeductionsTotal;
        existing.LunchTotal = dto.LunchTotal;
        existing.NetTotal = dto.NetTotal;
        existing.Status = dto.Status;
        existing.RevisedById = dto.RevisedById;
        existing.MoneySafeId = dto.MoneySafeId;
        existing.WithdrawnAt = dto.WithdrawnAt;
        existing.WithdrawnById = dto.WithdrawnById;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating payroll with Id {PayrollId}", id);
            throw;
        }

        _logger.LogInformation("Payroll with Id {PayrollId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var entity = await _dbContext.EmployeePayroll.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (entity is null)
            return ServiceResult<bool>.NotFound("Payroll record not found");

        if (entity.WithdrawnAt.HasValue)
            return ServiceResult<bool>.Failed("This payroll has already been withdrawn and cannot be deleted");

        entity.IsDeleted = true;
        entity.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Payroll with Id {PayrollId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}