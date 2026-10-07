// Services/ShiftService.cs
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using CMS.Domain.DTOs;
using CMS.Domain.Enums;
using CMS.Domain.Models;
using CMS.Application.Interfaces.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CMS.Application.DTOs;

namespace CMS_Backend.Services;

public class ShiftService : IShiftService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<ShiftService> _logger;

    private const int NotesMaxLength = 2000;

    public ShiftService(
        IAppDbContext dbContext,
        ILogger<ShiftService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private ShiftDto ToDto(Shift shift)
    {
        return new ShiftDto
        {
            Id = shift.Id,
            Date = shift.Date,
            CountryStateId = shift.CountryStateId,
            CountryStateName = shift.CountryState?.Name ?? "",
            CarId = shift.CarId,
            CarName = shift.Car?.Name ?? "",
            StoreId = shift.StoreId,
            StoreName = shift.Store?.Name ?? "",
            KmStart = shift.KmStart,
            KmEnd = shift.KmEnd,
            Notes = shift.Notes,
            ProjectId = shift.ProjectId,
            ProjectName = shift.Project?.Name ?? "",
            Techs = shift.Techs?.Where(t => !t.IsDeleted).Select(t => new ShiftTechDto
            {
                Id = t.Id,
                Date = t.Date,
                TechId = t.TechId,
                TechName = t.Tech?.Name ?? "",
                TechType = t.TechType
            }).ToList() ?? new List<ShiftTechDto>(),
            Invoices = shift.Invoices?.Where(i => !i.IsDeleted).Select(i => new SaleInvoiceDto
            {
                Id = i.Id,
                OrderId = i.OrderId,
                CustomerName = i.Customer?.Name ?? "",
                NetTotal = i.NetTotal,
                OrderNetTotal = i.Order?.NetTotal ?? 0,
                Items = i.Items?.Where(it => !it.IsDeleted).Select(it => new SaleInvoiceItemDto
                {
                    Id = it.Id,
                    ProductId = it.ProductId,
                    ProductName = it.Product?.Name ?? "",
                    ProductQuantity = it.ProductQuantity,
                }).ToList() ?? new List<SaleInvoiceItemDto>(),
                Payments = i.Payments?.Where(p => !p.IsDeleted).Select(p => new SaleInvoicePaymentDto
                {
                    Id = p.Id,
                    Amount = p.Amount,
                }).ToList() ?? new List<SaleInvoicePaymentDto>(),
            }).ToList() ?? new List<SaleInvoiceDto>(),
            Expenses = shift.Expenses?.Where(e => !e.IsDeleted).Select(e => new ExpenseDto
            {
                Id = e.Id,
                Amount = e.Amount,
                ExpenseTypeId = e.ExpenseTypeId,
            }).ToList() ?? new List<ExpenseDto>(),
            Loans = shift.Loans?.Where(l => !l.IsDeleted).Select(l => new EmployeeLoanDto
            {
                Id = l.Id,
                Amount = l.Amount,
                EmployeeId = l.EmployeeId
            }).ToList() ?? new List<EmployeeLoanDto>(),
            Transactions = shift.Transactions?.Where(t => !t.IsDeleted).Select(t => new MoneySafeTransactionDto
            {
                Id = t.Id,
                TransactionAmount = t.TransactionAmount,
                TransactionDate = t.TransactionDate,
                InMoneySafeId = t.InMoneySafeId,
                OutMoneySafeId = t.OutMoneySafeId,
            }).ToList() ?? new List<MoneySafeTransactionDto>(),
            CreatedAtUtc = shift.CreatedAtUtc,
            CreatedBy = shift.CreatedBy ?? "",
            UpdatedAtUtc = shift.UpdatedAtUtc,
            UpdatedBy = shift.UpdatedBy ?? ""
        };
    }

    private IQueryable<Shift> WithIncludes(IQueryable<Shift> query)
    {
        return query
            .Include(s => s.CountryState)
            .Include(s => s.Car)
            .Include(s => s.Store)
            .Include(s => s.Invoices)
            .Include(s => s.Invoices).ThenInclude(z => z.Items)
            .Include(s => s.Invoices).ThenInclude(z => z.Payments)
            .Include(s => s.Invoices).ThenInclude(z => z.Order)
            .Include(s => s.Invoices).ThenInclude(z => z.Order).ThenInclude(z => z.Items)
            .Include(s => s.Expenses)
            .Include(s => s.Loans)
            .Include(s => s.Transactions)
            .Include(s => s.Project)
            .Include(s => s.Techs!.Where(t => !t.IsDeleted))
                .ThenInclude(t => t.Tech);
    }

    // Centralized validation used by both Create and Update.
    private async Task<string?> ValidateAsync(ShiftDto dto)
    {
        if (dto is null)
            return "Shift payload is required";

        // --- Date ---
        if (dto.Date == default)
            return "Date is required";

        // --- CountryState (required FK) ---
        if (dto.CountryStateId <= 0)
            return "CountryStateId is required";

        var countryStateExists = await _dbContext.CountryState
            .AnyAsync(cs => cs.Id == dto.CountryStateId);

        if (!countryStateExists)
            return $"Country state with Id {dto.CountryStateId} was not found";

        // --- Car (required FK) ---
        if (dto.CarId <= 0)
            return "CarId is required";

        var carExists = await _dbContext.Car
            .AnyAsync(c => c.Id == dto.CarId && !c.IsDeleted);

        if (!carExists)
            return $"Car with Id {dto.CarId} was not found";

        // --- Store (required FK) ---
        if (dto.StoreId <= 0)
            return "StoreId is required";

        var storeExists = await _dbContext.Store
            .AnyAsync(s => s.Id == dto.StoreId && !s.IsDeleted);

        if (!storeExists)
            return $"Store with Id {dto.StoreId} was not found";

        // --- KmStart / KmEnd ---
        if (dto.KmStart < 0)
            return "KmStart cannot be negative";

        if (dto.KmEnd < 0)
            return "KmEnd cannot be negative";

        if (dto.KmEnd < dto.KmStart)
            return "KmEnd cannot be less than KmStart";

        // --- Notes (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Length > NotesMaxLength)
            return $"Notes cannot exceed {NotesMaxLength} characters";

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

        // --- Techs (at least one required) ---
        if (dto.Techs is null || dto.Techs.Count == 0)
            return "At least one technician is required for the shift";

        var seenTechIds = new HashSet<long>();

        foreach (var tech in dto.Techs)
        {
            if (tech.TechId <= 0)
                return "Each shift technician must have a valid TechId";

            if (!seenTechIds.Add(tech.TechId))
                return $"Technician Id {tech.TechId} appears more than once in this shift";

            var techExists = await _dbContext.Employee
                .AnyAsync(e => e.Id == tech.TechId && !e.IsDeleted);

            if (!techExists)
                return $"Technician (Employee) with Id {tech.TechId} was not found";

            if (!Enum.IsDefined(typeof(TechType), tech.TechType))
                return $"TechType for technician Id {tech.TechId} is not a valid value";

            if (tech.Date == default)
                tech.Date = dto.Date; // default a tech's date to the shift's date if not supplied
        }

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<ShiftDto>>> GetAllAsync()
    {
        var shifts = await WithIncludes(_dbContext.Shift.Where(z => !z.IsDeleted))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<ShiftDto>>.Ok(shifts.Select(ToDto).ToList());
    }
    
    public async Task<ServiceResult<List<ShiftDto>>> GetAllByDateAsync(DateTime date)
    {
        var shifts = await WithIncludes(_dbContext.Shift.Where(z => !z.IsDeleted && z.Date.Date == date.Date))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<ShiftDto>>.Ok(shifts.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<ShiftDto?>> GetByStoreIdAndDateAsync(long storeId, DateTime date)
    {
        if (storeId <= 0)
            return ServiceResult<ShiftDto?>.Failed("A valid store Id is required");

        if(date == default)
            return ServiceResult<ShiftDto?>.Failed("A valid date is required");

        var shift = await WithIncludes(_dbContext.Shift.Where(z => !z.IsDeleted && z.Date.Date == date.Date && z.StoreId == storeId))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (shift is null)
            return ServiceResult<ShiftDto?>.NotFound("Shift not found");

        return ServiceResult<ShiftDto?>.Ok(ToDto(shift));
    }

    public async Task<ServiceResult<ShiftDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<ShiftDto?>.Failed("A valid shift Id is required");

        var shift = await WithIncludes(_dbContext.Shift.Where(z => !z.IsDeleted && z.Id == id))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (shift is null)
            return ServiceResult<ShiftDto?>.NotFound("Shift not found");

        return ServiceResult<ShiftDto?>.Ok(ToDto(shift));
    }

    public async Task<ServiceResult<ShiftDto>> CreateAsync(ShiftDto dto)
    {
        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating shift: {Error}", validationError);
            return ServiceResult<ShiftDto>.Failed(validationError);
        }

        var shift = new Shift
        {
            Date = dto.Date,
            CountryStateId = dto.CountryStateId,
            CarId = dto.CarId,
            StoreId = dto.StoreId,
            KmStart = dto.KmStart,
            KmEnd = dto.KmEnd,
            Notes = dto.Notes,
            ProjectId = dto.ProjectId,
            Techs = dto.Techs.Select(t => new ShiftTech
            {
                Date = t.Date,
                TechId = t.TechId,
                TechType = t.TechType
            }).ToList()
        };

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

            _dbContext.Shift.Add(shift);
            await _dbContext.SaveChangesAsync();

            await dbTransaction.CommitAsync();
        });

        _logger.LogInformation("Shift created with Id {ShiftId} ({TechCount} technicians)", shift.Id, shift.Techs.Count);

        var created = await WithIncludes(_dbContext.Shift)
            .AsNoTracking()
            .FirstAsync(s => s.Id == shift.Id);

        return ServiceResult<ShiftDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, ShiftDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Shift payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.Shift
            .Include(s => s.Techs)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (existing is null)
            return ServiceResult<bool>.NotFound("Shift not found");

        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating shift {ShiftId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

            existing.Date = dto.Date;
            existing.CountryStateId = dto.CountryStateId;
            existing.CarId = dto.CarId;
            existing.StoreId = dto.StoreId;
            existing.KmStart = dto.KmStart;
            existing.KmEnd = dto.KmEnd;
            existing.Notes = dto.Notes;
            existing.ProjectId = dto.ProjectId;

            var existingTechs = existing.Techs.Where(t => !t.IsDeleted).ToList();
            var incomingIds = dto.Techs.Where(t => t.Id > 0).Select(t => t.Id).ToHashSet();

            // Soft-delete techs that are no longer present in the incoming list
            foreach (var tech in existingTechs.Where(t => !incomingIds.Contains(t.Id)))
            {
                tech.IsDeleted = true;
                tech.DeletedAtUtc = DateTime.UtcNow;
            }

            // Update existing techs (matched by Id) and add new ones (Id == 0)
            foreach (var incoming in dto.Techs)
            {
                if (incoming.Id > 0)
                {
                    var match = existingTechs.FirstOrDefault(t => t.Id == incoming.Id);
                    if (match is not null)
                    {
                        match.Date = incoming.Date;
                        match.TechId = incoming.TechId;
                        match.TechType = incoming.TechType;
                    }
                    // An incoming Id that doesn't match any existing tech on this shift is silently
                    // ignored rather than erroring — treat it as a new row instead.
                }
                else
                {
                    existing.Techs.Add(new ShiftTech
                    {
                        ShiftId = existing.Id,
                        Date = incoming.Date,
                        TechId = incoming.TechId,
                        TechType = incoming.TechType
                    });
                }
            }

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error occurred while updating shift with Id {ShiftId}", id);
                throw;
            }

            await dbTransaction.CommitAsync();
        });

        _logger.LogInformation("Shift with Id {ShiftId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var shift = await _dbContext.Shift
            .Include(s => s.Techs)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (shift is null)
            return ServiceResult<bool>.NotFound("Shift not found");

        var now = DateTime.UtcNow;

        shift.IsDeleted = true;
        shift.DeletedAtUtc = now;

        foreach (var tech in shift.Techs.Where(t => !t.IsDeleted))
        {
            tech.IsDeleted = true;
            tech.DeletedAtUtc = now;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Shift with Id {ShiftId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}