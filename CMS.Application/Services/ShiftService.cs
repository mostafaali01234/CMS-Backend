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
    private readonly IStockService _stock;
    private readonly IExpenseService _expenses;
    private readonly IEmployeeLoanService _loans;
    private readonly ISaleInvoiceService _invoices;
    private readonly IMoneySafeTransactionService _transactions;
    private readonly ILogger<ShiftService> _logger;

    private const int NotesMaxLength = 2000;

    public ShiftService(
        IAppDbContext dbContext,
        IStockService stock, 
        IExpenseService _expenses, 
        IEmployeeLoanService _loans, 
        ISaleInvoiceService _invoices,
        IMoneySafeTransactionService _transactions,
        ILogger<ShiftService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _stock = stock;
        this._expenses = _expenses;
        this._loans = _loans;
        this._invoices = _invoices;
        this._transactions = _transactions;
    }

    private ShiftDto ToDto(Shift shift, long safeId = 0)
    {
        var live = shift.Transactions?.Where(t => !t.IsDeleted).ToList() ?? new List<MoneySafeTransaction>();

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
            Invoices = shift.Invoices?.Where(i => !i.IsDeleted && i.Type == InvoiceType.Sale).Select(i => new SaleInvoiceDto
            {
                Id = i.Id,
                Number = i.Number,
                Date = i.Date,
                OrderId = i.OrderId,
                CustomerName = i.Customer?.Name ?? "",
                NetTotal = i.NetTotal,
                OrderNetTotal = i.Order?.NetTotal ?? 0,
                Items = i.Items?.Where(it => !it.IsDeleted).Select(it => new SaleInvoiceItemDto
                { Id = it.Id, ProductId = it.ProductId, ProductName = it.Product?.Name ?? "", ProductQuantity = it.ProductQuantity })
        .ToList() ?? new(),
                Payments = i.Payments?.Where(p => !p.IsDeleted).Select(p => new SaleInvoicePaymentDto { Id = p.Id, Amount = p.Amount })
        .ToList() ?? new(),
            }).ToList() ?? new(),
            Expenses = shift.Expenses?.Where(e => !e.IsDeleted).Select(e => new ExpenseDto
            {
                Id = e.Id,
                Amount = e.Amount,
                ExpenseTypeId = e.ExpenseTypeId,
                ExpenseTypeName = e.ExpenseType?.Name ?? "",
                Notes = e.Notes
            }).ToList() ?? new(),
            Loans = shift.Loans?.Where(l => !l.IsDeleted).Select(l => new EmployeeLoanDto
            {
                Id = l.Id,
                Amount = l.Amount,
                EmployeeId = l.EmployeeId,
                EmployeeName = l.Employee?.Name ?? "",
                Notes = l.Notes
            }).ToList() ?? new(),
            MoneySafeId = safeId > 0 ? safeId : null,
            Custody = safeId > 0 ? live.Where(t => t.InMoneySafeId == safeId).Sum(t => t.TransactionAmount) : 0,
            Delivered = safeId > 0 ? live.Where(t => t.OutMoneySafeId == safeId).Sum(t => t.TransactionAmount) : 0,
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
            .Include(s => s.Invoices).ThenInclude(z => z.Customer)
            .Include(s => s.Invoices).ThenInclude(z => z.Items).ThenInclude(z => z.Product)
            .Include(s => s.Invoices).ThenInclude(z => z.Payments)
            .Include(s => s.Invoices).ThenInclude(z => z.Order)
            .Include(s => s.Invoices).ThenInclude(z => z.Order).ThenInclude(z => z.Items)
            .Include(s => s.Expenses).ThenInclude(z => z.ExpenseType)
            .Include(s => s.Loans).ThenInclude(z => z.Employee)
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
        var leads = dto.Techs.Count(t => t.TechType is TechType.Tech or TechType.Tech_And_Helper);
        if (leads != 1)
            return "The shift needs exactly one primary technician (Tech or Tech_And_Helper)";

        var dayStart = dto.Date.Date; var dayEnd = dayStart.AddDays(1);
        var clash = await _dbContext.Shift
            .Where(s => !s.IsDeleted && s.Id != dto.Id && s.Date >= dayStart && s.Date < dayEnd
                     && (s.StoreId == dto.StoreId || s.CarId == dto.CarId))
            .Select(s => new { s.StoreId }).FirstOrDefaultAsync();
        if (clash != null)
            return clash.StoreId == dto.StoreId
                ? "A shift already exists for this store on this date"
                : "A shift already exists for this car on this date";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<ShiftDto>>> GetAllAsync()
    {
        var shifts = await WithIncludes(_dbContext.Shift.Where(z => !z.IsDeleted))
            .AsNoTracking()
            .ToListAsync();

        var safes = await SafeIdsAsync(shifts);
        return ServiceResult<List<ShiftDto>>.Ok(shifts.Select(s => ToDto(s, safes.GetValueOrDefault(s.Id))).ToList());
        //return ServiceResult<List<ShiftDto>>.Ok(shifts.Select(ToDto).ToList());
    }
    
    public async Task<ServiceResult<List<ShiftDto>>> GetAllByDateAsync(DateTime date)
    {
        var shifts = await WithIncludes(_dbContext.Shift.Where(z => !z.IsDeleted && z.Date.Date == date.Date))
            .AsNoTracking()
            .ToListAsync();

        var safes = await SafeIdsAsync(shifts);
        return ServiceResult<List<ShiftDto>>.Ok(shifts.Select(s => ToDto(s, safes.GetValueOrDefault(s.Id))).ToList());
        //return ServiceResult<List<ShiftDto>>.Ok(shifts.Select(ToDto).ToList());
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

        var safeId = (await SafeIdsAsync(new[] { shift })).GetValueOrDefault(shift.Id);
        return ServiceResult<ShiftDto?>.Ok(ToDto(shift, safeId));
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

        var safeId = (await SafeIdsAsync(new[] { shift })).GetValueOrDefault(shift.Id);
        return ServiceResult<ShiftDto?>.Ok(ToDto(shift, safeId));
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


        var safeId = (await SafeIdsAsync(new[] { shift })).GetValueOrDefault(shift.Id);
        return ServiceResult<ShiftDto>.Ok(ToDto(created, safeId));
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

    public async Task<ServiceResult<ShiftBoardDto>> GetBoardAsync(long storeId, DateTime date)
    {
        if (storeId <= 0) return ServiceResult<ShiftBoardDto>.Failed("A valid store Id is required");
        if (date == default) return ServiceResult<ShiftBoardDto>.Failed("A valid date is required");

        var day = date.Date; var next = day.AddDays(1);
        var shift = await WithIncludes(_dbContext.Shift
                .Where(z => !z.IsDeleted && z.StoreId == storeId && z.Date >= day && z.Date < next))
            .AsSplitQuery().AsNoTracking().FirstOrDefaultAsync();

        var invoiceIds = shift?.Invoices.Where(i => !i.IsDeleted && i.Type == InvoiceType.Sale).Select(i => i.Id).ToList()
                         ?? new List<long>();
        var items = await _stock.GetShiftItemsAsync(storeId, day, invoiceIds);
        var p = await PrimarySafeAsync(shift);

        if (shift != null)
        {
            // the order lines of the shift's invoices (WithIncludes already loads Order and its Items)
            var orderLines = shift.Invoices
                .Where(i => !i.IsDeleted && i.Type == InvoiceType.Sale && i.Order != null)
                .Select(i => i.Order!).DistinctBy(o => o.Id)
                .SelectMany(o => o.Items.Where(x => !x.IsDeleted)
                    .Select(x => new { OrderId = o.Id, x.ProductId, Qty = x.ProductQuantity }))
                .ToList();

            // a product that is only in an order (no stock move yet) still gets a row
            var known = items.Select(r => r.ProductId).ToHashSet();
            var missing = orderLines.Select(l => l.ProductId).Where(id => !known.Contains(id)).Distinct().ToList();
            if (missing.Count > 0)
            {
                var extra = await _dbContext.Product
                    .Where(p => missing.Contains(p.Id) && p.StorageType == StorageType.مخزني)
                    .Select(p => new { p.Id, p.Name }).ToListAsync();
                items.AddRange(extra.Select(p => new ShiftItemRowDto { ProductId = p.Id, ProductName = p.Name }));
                items = items.OrderBy(r => r.ProductName).ToList();
            }

            var byProduct = orderLines.GroupBy(l => l.ProductId).ToDictionary(g => g.Key,
                g => g.GroupBy(l => l.OrderId).ToDictionary(x => x.Key, x => x.Sum(l => l.Qty)));
            foreach (var r in items) r.OrderedByOrder = byProduct.GetValueOrDefault(r.ProductId) ?? new();
        }

        var board = new ShiftBoardDto { Shift = shift == null ? null : ToDto(shift, p?.SafeId ?? 0), Items = items };

        //var board = new ShiftBoardDto
        //{
        //    Shift = shift == null ? null : ToDto(shift, p?.SafeId ?? 0),
        //    Items = await _stock.GetShiftItemsAsync(storeId, day, invoiceIds)
        //};

        if (shift != null)
        {
            board.PrimaryTechName = p?.TechName; board.MoneySafeId = p?.SafeId; board.MoneySafeName = p?.SafeName;
            board.OpenOrders = await _dbContext.Order
                .Where(o => !o.IsDeleted && o.Status == OrderStatus.قيد_التنفيذ)
                .OrderBy(o => o.Id)
                .Select(o => new OpenOrderDto { Id = o.Id, CustomerName = o.Customer!.Name, NetTotal = o.NetTotal })
                .ToListAsync();
        }
        return ServiceResult<ShiftBoardDto>.Ok(board);
    }

    private static bool IsLead(ShiftTech t) => !t.IsDeleted && t.TechType is TechType.Tech or TechType.Tech_And_Helper;

    private async Task<(string TechName, long SafeId, string SafeName)?> PrimarySafeAsync(Shift s)
    {
        if (s == null)
            return null;
        var tech = s.Techs.FirstOrDefault(IsLead);
        if (tech is null) return null;
        var safe = await _dbContext.MoneySafe
            .Where(m => !m.IsDeleted && m.ManagerId == tech.TechId)
            .OrderBy(m => m.Id).Select(m => new { m.Id, m.Name }).FirstOrDefaultAsync();
        return safe is null ? null : (tech.Tech?.Name ?? "", safe.Id, safe.Name);
    }

    private async Task<(Shift? Shift, long SafeId, string? Error)> ContextAsync(long id)
    {
        var shift = await _dbContext.Shift.Include(s => s.Techs.Where(t => !t.IsDeleted)).ThenInclude(t => t.Tech)
            .AsNoTracking().FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (shift is null) return (null, 0, "Shift not found");
        var p = await PrimarySafeAsync(shift);
        return p is null ? (null, 0, "The primary technician has no money safe") : (shift, p.Value.SafeId, null);
    }

    public async Task<ServiceResult<ExpenseDto>> AddExpenseAsync(long shiftId, ExpenseDto dto)
    {
        var (shift, safeId, err) = await ContextAsync(shiftId);
        if (shift is null) return ServiceResult<ExpenseDto>.Failed(err!);
        dto.ShiftId = shiftId; dto.MoneySafeId = safeId; dto.Date = shift.Date; dto.ProjectId ??= shift.ProjectId;
        return await _expenses.CreateAsync(dto);
    }

    public async Task<ServiceResult<EmployeeLoanDto>> AddLoanAsync(long shiftId, EmployeeLoanDto dto)
    {
        var (shift, safeId, err) = await ContextAsync(shiftId);
        if (shift is null) return ServiceResult<EmployeeLoanDto>.Failed(err!);
        dto.ShiftId = shiftId; dto.MoneySafeId = safeId; dto.ProjectId ??= shift.ProjectId;
        dto.InstallmentCount = 0;
        if (dto.StartDate == default) dto.StartDate = new DateTime(shift.Date.Year, shift.Date.Month, 1).AddMonths(1);
        return await _loans.CreateAsync(dto);
    }

    public async Task<ServiceResult<SaleInvoiceDto>> AddInvoiceAsync(long shiftId, SaleInvoiceDto dto)
    {
        var (shift, safeId, err) = await ContextAsync(shiftId);
        if (shift is null) return ServiceResult<SaleInvoiceDto>.Failed(err!);
        if (dto.OrderId is null) return ServiceResult<SaleInvoiceDto>.Failed("An order is required");

        var open = await _dbContext.Order.AnyAsync(o => o.Id == dto.OrderId && !o.IsDeleted && o.Status == OrderStatus.قيد_التنفيذ);
        if (!open) return ServiceResult<SaleInvoiceDto>.Failed("The order is not in progress");

        dto.ShiftId = shiftId; dto.Type = InvoiceType.Sale; dto.Date = shift.Date;
        foreach (var i in dto.Items) if (i.StoreId <= 0) i.StoreId = shift.StoreId;
        foreach (var p in dto.Payments) { p.MoneySafeId = safeId; p.PaymentDate = shift.Date; }
        return await _invoices.CreateAsync(dto);
    }


    #region MS-Transactions
    // the primary tech's safe of many shifts in one query (same rule as PrimarySafeAsync: lowest id)
    private async Task<Dictionary<long, long>> SafeIdsAsync(IEnumerable<Shift> shifts)
    {
        var list = shifts.ToList();
        var techIds = list.SelectMany(s => s.Techs.Where(IsLead)).Select(t => (long?)t.TechId).Distinct().ToList();
        var safes = await _dbContext.MoneySafe
            .Where(m => !m.IsDeleted && techIds.Contains(m.ManagerId))
            .Select(m => new { m.Id, ManagerId = (long)m.ManagerId! }).ToListAsync();
        var byTech = safes.GroupBy(x => x.ManagerId).ToDictionary(g => g.Key, g => g.Min(x => x.Id));

        var result = new Dictionary<long, long>();
        foreach (var s in list)
        {
            var lead = s.Techs.FirstOrDefault(IsLead);
            if (lead != null && byTech.TryGetValue(lead.TechId, out var safeId)) result[s.Id] = safeId;
        }
        return result;
    }
    public Task<ServiceResult<MoneySafeTransactionDto>> AddCustodyAsync(long shiftId, ShiftTransferRequestDto r) =>
    TransferAsync(shiftId, r, toTech: true);

    public Task<ServiceResult<MoneySafeTransactionDto>> AddSettlementAsync(long shiftId, ShiftTransferRequestDto r) =>
        TransferAsync(shiftId, r, toTech: false);

    private async Task<ServiceResult<MoneySafeTransactionDto>> TransferAsync(long shiftId, ShiftTransferRequestDto r, bool toTech)
    {
        var (shift, safeId, err) = await ContextAsync(shiftId);
        if (shift is null) return ServiceResult<MoneySafeTransactionDto>.Failed(err!);
        if (r.Amount <= 0) return ServiceResult<MoneySafeTransactionDto>.Failed("Amount must be greater than zero");
        if (r.MoneySafeId <= 0 || r.MoneySafeId == safeId)
            return ServiceResult<MoneySafeTransactionDto>.Failed("Choose a money safe other than the technician's safe");
        if (!await _dbContext.MoneySafe.AnyAsync(m => m.Id == r.MoneySafeId && !m.IsDeleted))
            return ServiceResult<MoneySafeTransactionDto>.Failed("Money safe not found");

        // one handover per shift
        if (!toTech && await _dbContext.MoneySafeTransaction.AnyAsync(t =>
                t.ShiftId == shiftId && !t.IsDeleted && t.OutMoneySafeId == safeId))
            return ServiceResult<MoneySafeTransactionDto>.Failed("This shift already has an end-of-shift handover. Delete it first to add another");

        return await _transactions.CreateAsync(new MoneySafeTransactionDto
        {
            TransactionAmount = r.Amount,
            TransactionDate = shift.Date,
            InMoneySafeId = toTech ? safeId : r.MoneySafeId,
            OutMoneySafeId = toTech ? r.MoneySafeId : safeId,
            ShiftId = shiftId,
            TransactionNotes = r.Notes,
            AuditorId = r.AuditorId,
        });
    }

    public async Task<ServiceResult<bool>> DeleteTransactionAsync(long shiftId, long transactionId)
    {
        var belongs = await _dbContext.MoneySafeTransaction
            .AnyAsync(t => t.Id == transactionId && t.ShiftId == shiftId && !t.IsDeleted);
        if (!belongs) return ServiceResult<bool>.NotFound("Transaction not found in this shift");
        return await _transactions.DeleteAsync(transactionId);
    }
    #endregion
}