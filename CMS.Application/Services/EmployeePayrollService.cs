// Services/EmployeePayrollService.cs
using ClosedXML.Excel;
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;
using CMS.Application.Interfaces;
using CMS.Application.Interfaces.Configuration;
using CMS.Domain.DTOs;
using CMS.Domain.Enums;
using CMS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace CMS.Application.Services;

public class EmployeePayrollService : IEmployeePayrollService
{
    private readonly IAppDbContext _dbContext;
    private readonly ICommissionService _commission;
    private readonly ILogger<EmployeePayrollService> _logger;

    public EmployeePayrollService(
        IAppDbContext dbContext,
        ICommissionService commission,
        ILogger<EmployeePayrollService> logger)
    {
        _dbContext = dbContext;
        _commission = commission;
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
            ManagerCommissionTotal = p.ManagerCommissionTotal,
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
    private decimal ComputeNetTotal(EmployeePayrollDto dto) => 
        Math.Round(
            dto.BaseSalary + dto.SalesCommissionTotal + dto.TechCommissionTotal + dto.ManagerCommissionTotal
            + dto.BonusTotal + dto.LunchTotal - dto.LoansTotal - dto.DeductionsTotal, 2);

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
            ManagerCommissionTotal = dto.ManagerCommissionTotal,
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
        existing.ManagerCommissionTotal = dto.ManagerCommissionTotal;   
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
        if (entity is null) return ServiceResult<bool>.NotFound("Payroll record not found");
        if (entity.WithdrawnAt.HasValue) return ServiceResult<bool>.Failed("This payroll has already been withdrawn and cannot be deleted");

        foreach (var c in await _dbContext.EmployeeCommission.Where(c => c.PayrollId == id && !c.IsDeleted).ToListAsync())
        {
            c.PayrollId = null; c.WithdrawnAt = null;
            c.Status = c.PaidAt != null ? CommissionStatus.Invoice_Paid : CommissionStatus.Pending_Payment;
        }
        foreach (var a in await _dbContext.EmployeePayrollAdjustment.Where(a => a.PayrollId == id).ToListAsync())
            a.PayrollId = null;

        entity.IsDeleted = true;
        entity.DeletedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return ServiceResult<bool>.Ok(true);
    }

    private static decimal R(decimal v) => Math.Round(v, 2);

    private async Task<(PayrollPreviewDto? Dto, string? Error)> BuildAsync(long employeeId, int month, int year)
    {
        if (employeeId <= 0) return (null, "EmployeeId is required");
        if (month < 1 || month > 12) return (null, "Month must be between 1 and 12");
        if (year < 2000 || year > 2100) return (null, "Year is not valid");

        var emp = await _dbContext.Employee.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted);
        if (emp is null) return (null, $"Employee with Id {employeeId} was not found");

        var start = new DateTime(year, month, 1);
        var next = start.AddMonths(1);

        // an already approved payroll for this month
        var existing = await WithIncludes(_dbContext.EmployeePayroll.Where(p =>
                !p.IsDeleted && p.EmployeeId == employeeId && p.Month == month && p.Year == year))
            .AsNoTracking().FirstOrDefaultAsync();
        var pid = existing?.Id ?? 0;
        var locked = existing != null;

        // ---- commissions: an approved payroll shows its own rows;
        //      otherwise the commissions whose invoice was fully paid in this month ----
        var cq = _dbContext.EmployeeCommission.Where(c => !c.IsDeleted && c.EmployeeId == employeeId);
        cq = locked
            ? cq.Where(c => c.PayrollId == pid)
            : cq.Where(c => c.Status == CommissionStatus.Invoice_Paid && c.PaidAt >= start && c.PaidAt < next);

        var comms = await cq
            .Include(c => c.Invoice).ThenInclude(i => i!.Customer)
            .Include(c => c.Invoice).ThenInclude(i => i!.Order).ThenInclude(o => o!.Seller)
            .Include(c => c.Product)
            .AsNoTracking()
            .OrderBy(c => c.PaidAt).ThenBy(c => c.Id)
            .ToListAsync();

        // ---- additions and deductions (the loan-installment type is excluded:
        //      installments come from the loans below) ----
        var aq = _dbContext.EmployeePayrollAdjustment.Where(a =>
            !a.IsDeleted && a.EmployeeId == employeeId && a.Type != PayrollAdjustmentType.LoanInstallment);
        aq = locked
            ? aq.Where(a => a.PayrollId == pid)
            : aq.Where(a => a.PayrollId == null && a.OperationDate >= start && a.OperationDate < next);

        var adjs = await aq.Include(a => a.ApprovedBy).AsNoTracking()
            .OrderBy(a => a.OperationDate).ThenBy(a => a.Id).ToListAsync();

        // ---- loans: pending installments due in the month, or the ones this payroll deducted ----
        var iq = _dbContext.EmployeeLoan
            .Where(l => !l.IsDeleted && l.EmployeeId == employeeId)
            .SelectMany(l => l.Installments, (l, i) => new { l, i });
        // if EmployeeLoanInstallment has IsDeleted, add: iq = iq.Where(x => !x.i.IsDeleted);
        iq = iq.Where(x => !x.i.IsDeleted);
        iq = locked
            ? iq.Where(x => x.i.PayrollId == pid)
            : iq.Where(x => x.i.Status == InstallmentStatus.Pending && x.i.DueDate >= start && x.i.DueDate < next);

        var loans = await iq.OrderBy(x => x.i.DueDate).ThenBy(x => x.i.Id)
            .Select(x => new PayrollLoanRowDto
            {
                Id = x.i.Id,
                LoanId = x.l.Id,
                InstallmentNumber = x.i.InstallmentNumber,
                DueDate = x.i.DueDate,
                Amount = x.i.Amount,
                Notes = x.l.Notes
            }).ToListAsync();
        loans.AddRange(await _dbContext.EmployeeLoan
                                .Where(l => !l.IsDeleted && l.EmployeeId == employeeId && l.InstallmentCount == 0 && l.StartDate >= start && l.StartDate < next)
                                .Select(x => new PayrollLoanRowDto
                                {
                                    Id = x.Id,
                                    LoanId = x.Id,
                                    InstallmentNumber = 0,
                                    DueDate = x.StartDate,
                                    Amount = x.Amount,
                                    Notes = x.Notes
                                }).ToListAsync());

        // ---- lunch: the shifts this employee was in during the month ----
        var lunchShifts = await _dbContext.ShiftTech
            .Where(t => !t.IsDeleted && t.TechId == employeeId
                     && !t.Shift!.IsDeleted && t.Shift.Date >= start && t.Shift.Date < next)
            .Select(t => t.ShiftId).Distinct().CountAsync();

        // ---- commissions still waiting for their invoice to be paid (shown for information) ----
        var pending = await _dbContext.EmployeeCommission
            .Where(c => !c.IsDeleted && c.EmployeeId == employeeId && c.Status == CommissionStatus.Pending_Payment)
            .SumAsync(c => (decimal?)(c.Amount + c.Extra_Amount)) ?? 0;

        decimal Sum(CommissionRole r) => R(comms.Where(c => c.EmployeeRole == r).Sum(c => c.Amount + c.Extra_Amount));
        decimal Adj(PayrollAdjustmentKind k) => R(adjs.Where(a => a.Kind == k).Sum(a => a.Amount));

        var dto = new PayrollPreviewDto
        {
            EmployeeId = employeeId,
            EmployeeName = emp.Name,
            Month = month,
            Year = year,
            LunchAmount = emp.LunchAmount,
            LunchShifts = lunchShifts,
            PendingCommissionTotal = R(pending),
            ExistingPayroll = existing == null ? null : ToDto(existing),
            Loans = loans,

            // total of the invoices behind the listed commissions, each invoice counted once
            InvoicesTotal = R(comms.Where(c => c.Invoice != null)
                .GroupBy(c => c.InvoiceId).Sum(g => g.First().Invoice!.NetTotal)),

            Commissions = comms.Select(c => new PayrollCommissionRowDto
            {
                Id = c.Id,
                Role = c.EmployeeRole.ToString(),
                OrderId = c.OrderId,
                InvoiceId = c.InvoiceId,
                InvoiceNumber = c.Invoice?.Number ?? 0,
                OrderType = c.Invoice?.Order?.Type.ToString() ?? "",
                CustomerName = c.Invoice?.Customer?.Name ?? "",
                SellerName = c.Invoice?.Order?.Seller?.Name ?? "",
                ProductName = c.Product?.Name ?? "",
                ProductTotal = c.ProductTotal,
                Amount = c.Amount,
                ExtraAmount = c.Extra_Amount,
                Total = c.Amount + c.Extra_Amount,
                PaidAt = c.PaidAt
            }).ToList(),

            Adjustments = adjs.Select(a => new EmployeeAdjustmentDto
            {
                Id = a.Id,
                EmployeeId = a.EmployeeId,
                OperationDate = a.OperationDate,
                Kind = a.Kind,
                Type = a.Type,
                Amount = a.Amount,
                Reason = a.Reason,
                ApprovedById = a.ApprovedById,
                ApprovedByName = a.ApprovedBy?.Name ?? ""
            }).ToList(),
        };

        if (existing != null)
        {
            // an approved payroll keeps the figures it was approved with
            dto.BaseSalary = existing.BaseSalary;
            dto.SalesCommissionTotal = existing.SalesCommissionTotal;
            dto.TechCommissionTotal = existing.TechCommissionTotal;
            dto.ManagerCommissionTotal = existing.ManagerCommissionTotal;
            dto.BonusTotal = existing.BonusTotal;
            dto.DeductionsTotal = existing.DeductionsTotal;
            dto.LoansTotal = existing.LoansTotal;
            dto.LunchTotal = existing.LunchTotal;
            dto.NetTotal = existing.NetTotal;
        }
        else
        {
            dto.BaseSalary = R(emp.Salary);
            dto.SalesCommissionTotal = Sum(CommissionRole.Sales);
            dto.TechCommissionTotal = Sum(CommissionRole.Tech);
            dto.ManagerCommissionTotal = Sum(CommissionRole.Manager);
            dto.BonusTotal = Adj(PayrollAdjustmentKind.Addition);
            dto.DeductionsTotal = Adj(PayrollAdjustmentKind.Deduction);
            dto.LoansTotal = R(loans.Sum(l => l.Amount));
            dto.LunchTotal = R(lunchShifts * emp.LunchAmount);
            dto.NetTotal = ComputeNetTotal(new EmployeePayrollDto
            {
                BaseSalary = dto.BaseSalary,
                SalesCommissionTotal = dto.SalesCommissionTotal,
                TechCommissionTotal = dto.TechCommissionTotal,
                ManagerCommissionTotal = dto.ManagerCommissionTotal,
                BonusTotal = dto.BonusTotal,
                DeductionsTotal = dto.DeductionsTotal,
                LoansTotal = dto.LoansTotal,
                LunchTotal = dto.LunchTotal
            });
        }

        return (dto, null);
    }

    public async Task<ServiceResult<PayrollPreviewDto>> PreviewAsync(long employeeId, int month, int year)
    {
        var (dto, error) = await BuildAsync(employeeId, month, year);
        return error is not null ? ServiceResult<PayrollPreviewDto>.Failed(error) : ServiceResult<PayrollPreviewDto>.Ok(dto!);
    }

    public async Task<ServiceResult<EmployeePayrollDto>> ReviewAsync(PayrollReviewRequestDto req)
    {
        if (req is null) return ServiceResult<EmployeePayrollDto>.Failed("Payload is required");
        if (req.RevisedById <= 0 || !await _dbContext.Employee.AnyAsync(e => e.Id == req.RevisedById && !e.IsDeleted))
            return ServiceResult<EmployeePayrollDto>.Failed("Reviser (Employee) was not found");

        var (p, error) = await BuildAsync(req.EmployeeId, req.Month, req.Year);
        if (error is not null) return ServiceResult<EmployeePayrollDto>.Failed(error);
        if (p!.ExistingPayroll != null)
            return ServiceResult<EmployeePayrollDto>.Failed($"A payroll for {req.Month}/{req.Year} was already approved for this employee");

        var now = DateTime.UtcNow;
        var payroll = new EmployeePayroll
        {
            EmployeeId = p.EmployeeId,
            Month = p.Month,
            Year = p.Year,
            BaseSalary = p.BaseSalary,
            SalesCommissionTotal = p.SalesCommissionTotal,
            TechCommissionTotal = p.TechCommissionTotal,
            ManagerCommissionTotal = p.ManagerCommissionTotal,
            BonusTotal = p.BonusTotal,
            DeductionsTotal = p.DeductionsTotal,
            LoansTotal = p.LoansTotal,
            LunchTotal = p.LunchTotal,
            NetTotal = p.NetTotal,
            Status = PayrollStatus.Revised,
            RevisedAt = now,
            RevisedById = req.RevisedById
        };
        _dbContext.EmployeePayroll.Add(payroll);

        var cIds = p.Commissions.Select(c => c.Id).ToList();
        foreach (var c in await _dbContext.EmployeeCommission.Where(c => cIds.Contains(c.Id)).ToListAsync())
        { c.Status = CommissionStatus.Invoice_Paid; c.WithdrawnAt = now; c.Payroll = payroll; }

        var aIds = p.Adjustments.Select(a => a.Id).ToList();
        foreach (var a in await _dbContext.EmployeePayrollAdjustment.Where(a => aIds.Contains(a.Id)).ToListAsync())
            a.Payroll = payroll;

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Payroll {PayrollId} approved for employee {EmployeeId} ({Month}/{Year})", payroll.Id, payroll.EmployeeId, payroll.Month, payroll.Year);

        var created = await WithIncludes(_dbContext.EmployeePayroll).AsNoTracking().FirstAsync(x => x.Id == payroll.Id);
        return ServiceResult<EmployeePayrollDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> WithdrawAsync(long id, PayrollWithdrawRequestDto req)
    {
        var p = await _dbContext.EmployeePayroll.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (p is null) return ServiceResult<bool>.NotFound("Payroll record not found");
        if (p.WithdrawnAt.HasValue) return ServiceResult<bool>.Failed("This payroll has already been withdrawn");
        if (p.NetTotal <= 0) return ServiceResult<bool>.Failed("The net total must be greater than zero to withdraw");

        var safe = await _dbContext.MoneySafe.AsNoTracking().FirstOrDefaultAsync(m => m.Id == req.MoneySafeId && !m.IsDeleted);
        if (safe is null) return ServiceResult<bool>.Failed("Money safe was not found");
        if (!safe.Active) return ServiceResult<bool>.Failed($"Money safe '{safe.Name}' is not active");
        if (!await _dbContext.Employee.AnyAsync(e => e.Id == req.WithdrawnById && !e.IsDeleted))
            return ServiceResult<bool>.Failed("Withdrawer (Employee) was not found");

        p.Status = PayrollStatus.Withdrawn;
        p.MoneySafeId = req.MoneySafeId;
        p.WithdrawnAt = (req.Date ?? DateTime.UtcNow).Date == DateTime.UtcNow.Date ? DateTime.UtcNow : req.Date!.Value;
        p.WithdrawnById = req.WithdrawnById;
        await _dbContext.SaveChangesAsync();
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<PagedResult<EmployeePayrollDto>>> GetPagedAsync(PayrollFilterDto f)
    {
        var q = _dbContext.EmployeePayroll.Where(p => !p.IsDeleted);
        if (f.EmployeeId.HasValue) q = q.Where(p => p.EmployeeId == f.EmployeeId.Value);
        if (f.Month.HasValue) q = q.Where(p => p.Month == f.Month.Value);
        if (f.Year.HasValue) q = q.Where(p => p.Year == f.Year.Value);
        if (f.Status.HasValue) q = q.Where(p => p.Status == f.Status.Value);

        var page = await q.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ThenByDescending(p => p.Id)
            .Select(p => new EmployeePayrollDto
            {
                Id = p.Id,
                EmployeeId = p.EmployeeId,
                EmployeeName = p.Employee!.Name,
                Month = p.Month,
                Year = p.Year,
                BaseSalary = p.BaseSalary,
                SalesCommissionTotal = p.SalesCommissionTotal,
                TechCommissionTotal = p.TechCommissionTotal,
                ManagerCommissionTotal = p.ManagerCommissionTotal,
                LoansTotal = p.LoansTotal,
                BonusTotal = p.BonusTotal,
                DeductionsTotal = p.DeductionsTotal,
                LunchTotal = p.LunchTotal,
                NetTotal = p.NetTotal,
                Status = p.Status,
                MoneySafeId = p.MoneySafeId,
                MoneySafeName = p.MoneySafe == null ? "" : p.MoneySafe.Name,
                WithdrawnAt = p.WithdrawnAt
            }).ToPagedAsync(f);

        return ServiceResult<PagedResult<EmployeePayrollDto>>.Ok(page);
    }

    public async Task<ServiceResult<bool>> SetCommissionExtraAsync(long commissionId, decimal extra)
    {
        var c = await _dbContext.EmployeeCommission.FirstOrDefaultAsync(x => x.Id == commissionId && !x.IsDeleted);
        if (c is null) return ServiceResult<bool>.NotFound("Commission not found");
        if (c.Status == CommissionStatus.Withdrawn) return ServiceResult<bool>.Failed("This commission was already approved in a payroll");
        if (c.Amount + extra < 0) return ServiceResult<bool>.Failed("The extra amount cannot make the commission negative");
        c.Extra_Amount = extra;
        await _dbContext.SaveChangesAsync();
        return ServiceResult<bool>.Ok(true);
    }
    public async Task<ServiceResult<EmployeeAdjustmentDto>> AddAdjustmentAsync(EmployeeAdjustmentDto dto)
    {
        if (dto is null) return ServiceResult<EmployeeAdjustmentDto>.Failed("Payload is required");
        if (!Enum.IsDefined(typeof(PayrollAdjustmentKind), dto.Kind)) return ServiceResult<EmployeeAdjustmentDto>.Failed("Kind is not valid");
        if (!Enum.IsDefined(typeof(PayrollAdjustmentType), dto.Type)) return ServiceResult<EmployeeAdjustmentDto>.Failed("Type is not valid");
        if (dto.Type == PayrollAdjustmentType.LoanInstallment)
            return ServiceResult<EmployeeAdjustmentDto>.Failed("Loan installments are deducted automatically from the loans");

        // the type must match the kind: deductions 2..4, additions 20..23, Other for both
        var t = (int)dto.Type;
        var ok = dto.Type == PayrollAdjustmentType.Other
              || (dto.Kind == PayrollAdjustmentKind.Deduction && t is >= 2 and <= 4)
              || (dto.Kind == PayrollAdjustmentKind.Addition && t is >= 20 and <= 23);
        if (!ok) return ServiceResult<EmployeeAdjustmentDto>.Failed("The type does not match the kind");

        if (dto.Amount <= 0) return ServiceResult<EmployeeAdjustmentDto>.Failed("Amount must be greater than zero");
        if (dto.OperationDate == default) return ServiceResult<EmployeeAdjustmentDto>.Failed("Date is required");
        if (dto.Reason?.Length > 2000) return ServiceResult<EmployeeAdjustmentDto>.Failed("Reason cannot exceed 2000 characters");
        if (!await _dbContext.Employee.AnyAsync(e => e.Id == dto.EmployeeId && !e.IsDeleted))
            return ServiceResult<EmployeeAdjustmentDto>.Failed("Employee was not found");
        if (dto.ApprovedById is null || !await _dbContext.Employee.AnyAsync(e => e.Id == dto.ApprovedById && !e.IsDeleted))
            return ServiceResult<EmployeeAdjustmentDto>.Failed("Manager (Employee) was not found");

        var d = dto.OperationDate.Date;
        if (await _dbContext.EmployeePayroll.AnyAsync(p => !p.IsDeleted && p.EmployeeId == dto.EmployeeId && p.Month == d.Month && p.Year == d.Year))
            return ServiceResult<EmployeeAdjustmentDto>.Failed("A payroll for this month was already approved. Delete it first");

        var e = new EmployeePayrollAdjustment
        {
            EmployeeId = dto.EmployeeId,
            OperationDate = d,
            Kind = dto.Kind,
            Type = dto.Type,
            Amount = dto.Amount,
            Reason = dto.Reason ?? "",
            ApprovedById = dto.ApprovedById
        };
        _dbContext.EmployeePayrollAdjustment.Add(e);
        await _dbContext.SaveChangesAsync();
        dto.Id = e.Id;
        return ServiceResult<EmployeeAdjustmentDto>.Ok(dto);
    }

    public async Task<ServiceResult<bool>> DeleteAdjustmentAsync(long id)
    {
        var a = await _dbContext.EmployeePayrollAdjustment.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (a is null) return ServiceResult<bool>.NotFound("Not found");
        if (a.PayrollId != null) return ServiceResult<bool>.Failed("This item is part of an approved payroll");
        a.IsDeleted = true; a.DeletedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<RecalcResultDto>> RecalculateCommissionsAsync(long employeeId, int month, int year)
    {
        if (month < 1 || month > 12) return ServiceResult<RecalcResultDto>.Failed("Month must be between 1 and 12");
        if (year < 2000 || year > 2100) return ServiceResult<RecalcResultDto>.Failed("Year is not valid");
        if (!await _dbContext.Employee.AnyAsync(e => e.Id == employeeId && !e.IsDeleted))
            return ServiceResult<RecalcResultDto>.Failed("Employee was not found");

        var start = new DateTime(year, month, 1);
        var next = start.AddMonths(1);

        if (await _dbContext.EmployeePayroll.AnyAsync(p => !p.IsDeleted && p.EmployeeId == employeeId && p.Month == month && p.Year == year))
            return ServiceResult<RecalcResultDto>.Failed("A payroll for this month was already approved. Delete it first");

        // every sale invoice that can carry a commission of this employee in this month
        var invoiceIds = new HashSet<long>();

        // 1) the commissions he already has: invoice dated or paid in the month
        invoiceIds.UnionWith(await _dbContext.EmployeeCommission
            .Where(c => !c.IsDeleted && c.EmployeeId == employeeId
                     && ((c.PaidAt >= start && c.PaidAt < next) || (c.Invoice!.Date >= start && c.Invoice.Date < next)))
            .Select(c => c.InvoiceId).ToListAsync());

        // 2) as a seller
        invoiceIds.UnionWith(await _dbContext.SaleInvoice
            .Where(i => !i.IsDeleted && i.Type == InvoiceType.Sale && i.Date >= start && i.Date < next
                     && i.Order!.SellerId == employeeId)
            .Select(i => i.Id).ToListAsync());

        // 3) as a manager: the invoices of the sellers in his group
        var sellers = await _dbContext.SalesManagerGroupSeller
            .Where(m => !m.IsDeleted && !m.Group!.IsDeleted && m.Group.ManagerId == employeeId)
            .Select(m => m.EmployeeId).ToListAsync();
        if (sellers.Count > 0)
            invoiceIds.UnionWith(await _dbContext.SaleInvoice
                .Where(i => !i.IsDeleted && i.Type == InvoiceType.Sale && i.Date >= start && i.Date < next
                         && sellers.Contains(i.Order!.SellerId))
                .Select(i => i.Id).ToListAsync());

        // 4) as a tech: every shift he was in this month
        var shiftIds = (await _dbContext.ShiftTech
            .Where(t => !t.IsDeleted && t.TechId == employeeId && !t.Shift!.IsDeleted && t.Shift.Date >= start && t.Shift.Date < next)
            .Select(t => t.ShiftId).Distinct().ToListAsync()).ToHashSet();

        var found = await _dbContext.SaleInvoice
            .Where(i => !i.IsDeleted && invoiceIds.Contains(i.Id))
            .Select(i => new { i.Id, i.ShiftId }).ToListAsync();
        foreach (var i in found.Where(x => x.ShiftId.HasValue)) shiftIds.Add(i.ShiftId!.Value);
        var looseIds = found.Where(x => !x.ShiftId.HasValue).Select(x => x.Id).ToList();

        var result = new RecalcResultDto();

        // a shift is recalculated as a whole, because the order of the products inside the shift matters
        foreach (var sid in shiftIds)
        {
            if (await _commission.CheckUnlockedAsync(sid, Array.Empty<long>()) is not null) { result.Skipped++; continue; }
            await _commission.RecalculateAsync(sid, Array.Empty<long>());
            result.Shifts++;
        }

        var open = new List<long>();
        foreach (var id in looseIds)
        {
            if (await _commission.CheckUnlockedAsync(null, new[] { id }) is not null) { result.Skipped++; continue; }
            open.Add(id);
        }
        if (open.Count > 0) { await _commission.RecalculateAsync(null, open); result.Invoices = open.Count; }

        return ServiceResult<RecalcResultDto>.Ok(result);
    }

    #region Export
    private static string RoleAr(string r) => r switch { "Sales" => "بيع", "Tech" => "تركيب", "Manager" => "مجموعة", _ => r };
    private static string KindAr(PayrollAdjustmentKind k) => k == PayrollAdjustmentKind.Addition ? "اضافة" : "خصم";
    private static string TypeAr(PayrollAdjustmentType t) => t switch
    {
        PayrollAdjustmentType.Absence => "غياب",
        PayrollAdjustmentType.Penalty => "جزاء",
        PayrollAdjustmentType.Insurance => "تأمين",
        PayrollAdjustmentType.Incentive => "حافز",
        PayrollAdjustmentType.Bonus => "مكافأة",
        PayrollAdjustmentType.Commission => "عمولة",
        PayrollAdjustmentType.Overtime => "اضافي",
        PayrollAdjustmentType.LoanInstallment => "قسط سلفة",
        _ => "اخرى"
    };

    public async Task<ExportResult> ExportReviewAsync(long employeeId, int month, int year)
    {
        var (d, error) = await BuildAsync(employeeId, month, year);
        if (error is not null) return new ExportResult(null, error, "");

        var title = $"مرتب {d!.EmployeeName} - {d.Month}/{d.Year}";
        var status = d.ExistingPayroll is null ? "مراجعة - لم يتم الاعتماد بعد"
                   : d.ExistingPayroll.Status == PayrollStatus.Withdrawn ? "تم الصرف" : "تم الاعتماد";
        var sub = $"الحالة: {status}   |   التاريخ: {DateTime.Now:yyyy-MM-dd HH:mm}";

        using var wb = new XLWorkbook();

        // ---- summary ----
        TableExcel.FillSheet(wb, "الملخص", title, sub,
            new List<XCol> { new("البند", 42), new("القيمة", 18, TableExcel.Money, true) },
            new List<object?[]>
            {
            new object?[] { "اجمالي الاوردرات / الفواتير", d.InvoicesTotal },
            new object?[] { "اجمالي عمولات البيع", d.SalesCommissionTotal },
            new object?[] { "اجمالي عمولات التركيب", d.TechCommissionTotal },
            new object?[] { "عمولة المجموعة", d.ManagerCommissionTotal },
            new object?[] { "الراتب الاساسي", d.BaseSalary },
            new object?[] { "الاضافات", d.BonusTotal },
            new object?[] { "الخصومات", d.DeductionsTotal },
            new object?[] { "السلف (اقساط الشهر)", d.LoansTotal },
            new object?[] { $"الغدا ({d.LunchShifts} وردية × {d.LunchAmount:0.##})", d.LunchTotal },
            new object?[] { "باقي العمولات - فواتير غير مدفوعة (للعلم فقط)", d.PendingCommissionTotal },
            },
            new object?[] { null, d.NetTotal }, "الاجمالي (الصافي)");

        // ---- commissions ----
        TableExcel.FillSheet(wb, "العمولات", title, "تفاصيل العمولات",
            new List<XCol>
            {
            new("النوع", 10, null, true), new("الاوردر", 10, null, true), new("الفاتورة", 10, null, true),
            new("جملة / تركيب", 14), new("العميل", 26), new("البائع", 20), new("الصنف", 30),
            new("اجمالي الصنف", 15, TableExcel.Money), new("العمولة", 13, TableExcel.Money),
            new("اضافي", 12, TableExcel.Money), new("المعدلة", 13, TableExcel.Money), new("تاريخ السداد", 14, TableExcel.Day, true),
            },
            d.Commissions.Select(c => new object?[]
            {
            RoleAr(c.Role), c.OrderId, c.InvoiceNumber, c.OrderType.Replace('_', ' '), c.CustomerName, c.SellerName,
            c.ProductName, c.ProductTotal, c.Amount, c.ExtraAmount, c.Total, c.PaidAt
            }),
            new object?[] { null, null, null, null, null, null, null, null,
                        d.Commissions.Sum(c => c.Amount), d.Commissions.Sum(c => c.ExtraAmount), d.Commissions.Sum(c => c.Total), null },
            $"الاجمالي ({d.Commissions.Count} عمولة)");

        // ---- additions and deductions ----
        var net = d.Adjustments.Sum(a => a.Kind == PayrollAdjustmentKind.Addition ? a.Amount : -a.Amount);
        TableExcel.FillSheet(wb, "الاضافات والخصومات", title, "تفاصيل الخصومات والاضافات",
            new List<XCol>
            {
            new("المدير", 22), new("التاريخ", 14, TableExcel.Day, true), new("الفئة", 12, null, true),
            new("النوع", 16), new("القيمة", 14, TableExcel.Money), new("السبب", 40),
            },
            d.Adjustments.Select(a => new object?[]
                { a.ApprovedByName, a.OperationDate, KindAr(a.Kind), TypeAr(a.Type), a.Amount, a.Reason }),
            new object?[] { null, null, null, null, net, null }, "الصافي (اضافات - خصومات)");

        // ---- loans ----
        TableExcel.FillSheet(wb, "السلف", title, "اقساط هذا الشهر",
            new List<XCol>
            {
            new("السلفة", 12, null, true), new("القسط", 10, null, true), new("تاريخ الاستحقاق", 16, TableExcel.Day, true),
            new("القيمة", 14, TableExcel.Money), new("ملاحظات", 40),
            },
            d.Loans.Select(l => new object?[] { l.LoanId, l.InstallmentNumber, l.DueDate, l.Amount, l.Notes }),
            new object?[] { null, null, null, d.Loans.Sum(l => l.Amount), null }, "الاجمالي");

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return new ExportResult(ms.ToArray(), null, $"payroll-{employeeId}-{year}-{month:00}.xlsx");
    }
    #endregion
}