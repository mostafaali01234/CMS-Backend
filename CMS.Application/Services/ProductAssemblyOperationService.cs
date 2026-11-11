// Services/ProductAssemblyOperationService.cs
using CMS.Application.Interfaces.Configuration;
using CMS.Domain.Models;
using CMS.Domain.Enums;
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;
using CMS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Extensions.Logging;

namespace CMS.Application.Services;

public class ProductAssemblyOperationService : IProductAssemblyOperationService
{
    private readonly IAppDbContext _dbContext;
    private readonly IStockService _stock;
    private readonly ILogger<ProductAssemblyOperationService> _logger;

    private const int NotesMaxLength = 2000;

    public ProductAssemblyOperationService(
        IAppDbContext dbContext,
        IStockService stock,
        ILogger<ProductAssemblyOperationService> logger)
    {
        _dbContext = dbContext;
        _stock = stock;
        _logger = logger;
    }

    private ProductAssemblyOperationDto ToDto(ProductAssemblyOperation op)
    {
        return new ProductAssemblyOperationDto
        {
            Id = op.Id,
            OutProductId = op.OutProductId,
            OutProductName = op.OutProduct?.Name ?? "",
            OutStoreId = op.OutStoreId,
            OutStoreName = op.OutStore?.Name ?? "",
            InStoreId = op.InStoreId,
            InStoreName = op.InStore?.Name ?? "",
            Quantity = op.Quantity,
            OperationDate = op.OperationDate,
            OperationType = op.OperationType,
            FirstTechId = op.FirstTechId,
            FirstTechName = op.FirstTech?.Name ?? "",
            SecondTechId = op.SecondTechId,
            SecondTechName = op.SecondTech?.Name ?? "",
            Notes = op.Notes,
            Items = (op.Items ?? new List<ProductAssemblyOperationItem>())
                .Where(i => !i.IsDeleted)
                .Select(i => new ProductAssemblyOperationItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? "",
                    UnitQuantity = i.UnitQuantity,
                    Quantity = i.Quantity
                }).ToList(),
            CreatedAtUtc = op.CreatedAtUtc,
            CreatedBy = op.CreatedBy ?? "",
            UpdatedAtUtc = op.UpdatedAtUtc,
            UpdatedBy = op.UpdatedBy ?? ""
        };
    }

    private ProductAssemblyOperation ToEntity(ProductAssemblyOperationDto dto)
    {
        return new ProductAssemblyOperation
        {
            Id = dto.Id,
            OutProductId = dto.OutProductId,
            OutStoreId = dto.OutStoreId,
            InStoreId = dto.InStoreId,
            Quantity = dto.Quantity,
            OperationDate = dto.OperationDate,
            OperationType = dto.OperationType,
            FirstTechId = NullIfZero(dto.FirstTechId),
            SecondTechId = NullIfZero(dto.SecondTechId),
            Notes = dto.Notes
        };
    }

    private const string LockedMessage = "تم سحب عمولة هذه العملية في كشف الرواتب، لا يمكن تعديلها أو حذفها";

    private Task<bool> CommissionsLockedAsync(long operationId) =>
        _dbContext.EmployeeCommission.AnyAsync(c =>
            !c.IsDeleted && c.AssemblyOperationId == operationId && c.Status == CommissionStatus.Withdrawn);

    // What each technician earns for an operation: quantity x the definition's rate.
    // The same employee as first and second technician earns both (the amounts are added, not overwritten).
    private async Task<Dictionary<long, decimal>> DesiredCommissionsAsync(long outProductId, long? firstTechId, long? secondTechId, decimal quantity)
    {
        var rates = await _dbContext.ProductAssemblyDefinition
            .Where(d => !d.IsDeleted && d.OutProductId == outProductId)
            .Select(d => new { d.FirstTechComm, d.SecondTechComm })
            .FirstOrDefaultAsync();

        var desired = new Dictionary<long, decimal>();
        if (rates is null) return desired;

        void Add(long? tech, decimal rate)
        {
            if (tech is not long id) return;
            desired[id] = desired.GetValueOrDefault(id) + Math.Round(quantity * rate, 2);
        }
        Add(firstTechId, rates.FirstTechComm);
        Add(secondTechId, rates.SecondTechComm);

        foreach (var k in desired.Where(x => x.Value <= 0).Select(x => x.Key).ToList()) desired.Remove(k);
        return desired;
    }

    // A commission is paid by the payroll of its month (PaidAt = the operation date). When the technician's payroll for
    // that month is already approved, a new commission would never be picked up by any payroll, so it is refused.
    private async Task<string?> ClosedPayrollMonthAsync(ProductAssemblyOperationDto dto, ProductAssemblyOperation? existing)
    {
        var desired = await DesiredCommissionsAsync(dto.OutProductId, NullIfZero(dto.FirstTechId), NullIfZero(dto.SecondTechId), dto.Quantity);
        var month = dto.OperationDate.Month;
        var year = dto.OperationDate.Year;

        // an update that keeps the month only has to check technicians that were not already on the operation
        var already = new HashSet<long>();
        if (existing is not null && existing.OperationDate.Month == month && existing.OperationDate.Year == year)
        {
            if (existing.FirstTechId is long a) already.Add(a);
            if (existing.SecondTechId is long b) already.Add(b);
        }

        foreach (var techId in desired.Keys.Where(t => !already.Contains(t)))
            if (await _dbContext.EmployeePayroll.AnyAsync(p => !p.IsDeleted && p.EmployeeId == techId && p.Month == month && p.Year == year))
                return "تم اعتماد كشف راتب الفني عن شهر هذه العملية، لا يمكن تسجيل عمولة جديدة فيه";

        return null;
    }

    // Brings the operation's technician commissions in line with it (quantity x the definition's rate).
    // Nothing is saved here: the caller saves, so the operation and its commissions go in together.
    // Commissions are payable right away (no invoice to wait for) and are picked up by the payroll of PaidAt's month.
    private async Task SyncCommissionsAsync(ProductAssemblyOperation op)
    {
        var desired = await DesiredCommissionsAsync(op.OutProductId, op.FirstTechId, op.SecondTechId, op.Quantity);

        var existing = op.Id == 0
            ? new List<EmployeeCommission>()
            : await _dbContext.EmployeeCommission
                .Where(c => !c.IsDeleted && c.AssemblyOperationId == op.Id)
                .ToListAsync();

        foreach (var c in existing)
        {
            if (desired.Remove(c.EmployeeId, out var amount))
            {
                c.Amount = amount;
                c.ProductId = op.OutProductId;
                c.Status = CommissionStatus.Invoice_Paid;
                c.PaidAt = op.OperationDate;
            }
            else if (c.Extra_Amount != 0) c.Amount = 0;               // keep the admin's extra amount
            else { c.IsDeleted = true; c.DeletedAtUtc = DateTime.UtcNow; }
        }

        foreach (var (employeeId, amount) in desired)
            _dbContext.EmployeeCommission.Add(new EmployeeCommission
            {
                AssemblyOperation = op,
                EmployeeId = employeeId,
                EmployeeRole = CommissionRole.Tech,
                ProductId = op.OutProductId,
                ProductTotal = 0,
                Amount = amount,
                Status = CommissionStatus.Invoice_Paid,
                PaidAt = op.OperationDate
            });
    }

    private static long? NullIfZero(long? id) => id.HasValue && id.Value > 0 ? id : null;

    // Centralized validation used by both Create and Update.
    private async Task<string?> ValidateAsync(ProductAssemblyOperationDto dto)
    {
        if (dto is null)
            return "Assembly operation payload is required";

        // --- OutProduct (required FK) ---
        if (dto.OutProductId <= 0)
            return "OutProductId is required";

        var outProductExists = await _dbContext.Product
            .AnyAsync(p => p.Id == dto.OutProductId && !p.IsDeleted);

        if (!outProductExists)
            return $"Out product with Id {dto.OutProductId} was not found";

        // --- OutStore (required FK) ---
        if (dto.OutStoreId <= 0)
            return "OutStoreId is required";

        var outStoreExists = await _dbContext.Store
            .AnyAsync(s => s.Id == dto.OutStoreId && !s.IsDeleted);

        if (!outStoreExists)
            return $"Out store with Id {dto.OutStoreId} was not found";

        // --- InStore (required FK) ---
        if (dto.InStoreId <= 0)
            return "InStoreId is required";

        var inStoreExists = await _dbContext.Store
            .AnyAsync(s => s.Id == dto.InStoreId && !s.IsDeleted);

        if (!inStoreExists)
            return $"In store with Id {dto.InStoreId} was not found";

        // --- Quantity ---
        if (dto.Quantity <= 0)
            return "Quantity must be greater than zero";

        // --- OperationDate ---
        if (dto.OperationDate == default)
            return "OperationDate is required";

        // --- Assembly definition must exist for this OutProductId ---
        // An assembly operation without a matching definition has nothing to "consume" from,
        // so it must reference a product that actually has a defined bill of materials.
        if (!Enum.IsDefined(typeof(ProductAssemblyOperationType), dto.OperationType))
            return "OperationType is not a valid value";

        var definitionType = await _dbContext.ProductAssemblyDefinition
            .Where(d => !d.IsDeleted && d.OutProductId == dto.OutProductId)
            .Select(d => (ProductAssemblyOperationType?)d.OperationType)
            .FirstOrDefaultAsync();

        if (definitionType is null)
            return $"Product with Id {dto.OutProductId} has no assembly definition; cannot record an assembly operation for it";

        if (definitionType != dto.OperationType)
            return $"OperationType does not match the product's assembly definition ({definitionType})";

        // --- Technicians (optional; second needs a first, and they must differ) ---
        var firstTechId = NullIfZero(dto.FirstTechId);
        var secondTechId = NullIfZero(dto.SecondTechId);

        if (secondTechId.HasValue && !firstTechId.HasValue)
            return "SecondTechId cannot be set without FirstTechId";

        //if (firstTechId.HasValue && firstTechId == secondTechId)
        //    return "FirstTechId and SecondTechId must be different employees";

        if (firstTechId.HasValue && !await _dbContext.Employee.AnyAsync(e => e.Id == firstTechId.Value && !e.IsDeleted))
            return $"First technician with Id {firstTechId.Value} was not found";

        if (secondTechId.HasValue && !await _dbContext.Employee.AnyAsync(e => e.Id == secondTechId.Value && !e.IsDeleted))
            return $"Second technician with Id {secondTechId.Value} was not found";

        // --- Notes (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Length > NotesMaxLength)
            return $"Notes cannot exceed {NotesMaxLength} characters";

        return null; // no validation errors
    }

    // ---------------------------------------------------------------------------------------------
    // Stock. The operation takes its components out of OutStore and puts the assembled product (OutProduct) into InStore.
    // The components are a snapshot of the definition kept in product_assembly_operation_item, so changing or deleting
    // the definition later does not change the stock of operations that were already saved.
    // ---------------------------------------------------------------------------------------------

    private sealed record Component(long ProductId, decimal Unit, decimal Qty);

    private static decimal Total(decimal unit, decimal quantity) => Math.Round(unit * quantity, 3);

    // The components the operation will consume once saved.
    // Same product as before: the recipe it was made with is kept (only the quantity scales).
    // New operation, another product, or an older operation saved before stock existed: the current definition.
    private async Task<List<Component>> PlanComponentsAsync(ProductAssemblyOperation? existing, ProductAssemblyOperationDto dto)
    {
        var live = existing?.Items.Where(i => !i.IsDeleted).ToList() ?? new List<ProductAssemblyOperationItem>();
        if (existing is not null && live.Count > 0 && existing.OutProductId == dto.OutProductId)
            return live.Select(i => new Component(i.ProductId, i.UnitQuantity, Total(i.UnitQuantity, dto.Quantity))).ToList();

        var recipe = await _dbContext.ProductAssemblyDefinition
            .Where(d => !d.IsDeleted && d.OutProductId == dto.OutProductId)
            .Select(d => new { d.InProductId, d.InQuantity })
            .ToListAsync();

        return recipe.Select(r => new Component(r.InProductId, r.InQuantity, Total(r.InQuantity, dto.Quantity))).ToList();
    }

    private static void ApplyComponents(ProductAssemblyOperation op, List<Component> plan)
    {
        var live = op.Items.Where(i => !i.IsDeleted).ToList();
        var now = DateTime.UtcNow;

        foreach (var item in live)
        {
            var c = plan.FirstOrDefault(p => p.ProductId == item.ProductId);
            if (c is null) { item.IsDeleted = true; item.DeletedAtUtc = now; }
            else { item.UnitQuantity = c.Unit; item.Quantity = c.Qty; }
        }

        foreach (var c in plan.Where(p => live.All(i => i.ProductId != p.ProductId)))
            op.Items.Add(new ProductAssemblyOperationItem { ProductId = c.ProductId, UnitQuantity = c.Unit, Quantity = c.Qty });
    }

    // The stock effect an already saved operation has now. An operation without a snapshot (saved before stock existed) has none.
    private static List<StockDelta> CurrentDeltas(ProductAssemblyOperation op)
    {
        var live = op.Items.Where(i => !i.IsDeleted).ToList();
        return live.Count == 0
            ? new List<StockDelta>()
            : StockDeltas.Assembly(op.OutStoreId, op.InStoreId, op.OutProductId, op.Quantity, live.Select(i => (i.ProductId, i.Quantity)));
    }

    private IQueryable<ProductAssemblyOperation> WithIncludes(IQueryable<ProductAssemblyOperation> q) => q
        .Include(o => o.OutProduct)
        .Include(o => o.OutStore)
        .Include(o => o.InStore)
        .Include(o => o.FirstTech)
        .Include(o => o.SecondTech)
        .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Product);

    public async Task<ServiceResult<List<ProductAssemblyOperationDto>>> GetAllAsync()
    {
        var operations = await WithIncludes(_dbContext.ProductAssemblyOperation.Where(z => !z.IsDeleted))
            .AsNoTracking()
            .ToListAsync();

        var dtos = operations.Select(ToDto).ToList();
        return ServiceResult<List<ProductAssemblyOperationDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<ProductAssemblyOperationDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<ProductAssemblyOperationDto?>.Failed("A valid operation Id is required");

        var operation = await WithIncludes(_dbContext.ProductAssemblyOperation.Where(z => !z.IsDeleted && z.Id == id))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (operation is null)
            return ServiceResult<ProductAssemblyOperationDto?>.NotFound("Assembly operation not found");

        return ServiceResult<ProductAssemblyOperationDto?>.Ok(ToDto(operation));
    }

    public Task<ServiceResult<ProductAssemblyOperationDto>> CreateAsync(ProductAssemblyOperationDto op) =>
        _stock.RunExclusiveAsync(() => CreateCoreAsync(op));

    private async Task<ServiceResult<ProductAssemblyOperationDto>> CreateCoreAsync(ProductAssemblyOperationDto op)
    {
        var validationError = await ValidateAsync(op);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating assembly operation: {Error}", validationError);
            return ServiceResult<ProductAssemblyOperationDto>.Failed(validationError);
        }

        var payrollError = await ClosedPayrollMonthAsync(op, null);
        if (payrollError is not null)
            return ServiceResult<ProductAssemblyOperationDto>.Failed(payrollError);

        var plan = await PlanComponentsAsync(null, op);
        var stockError = await _stock.CheckChangeAsync(new StockExclusion(), Array.Empty<StockDelta>(),
            StockDeltas.Assembly(op.OutStoreId, op.InStoreId, op.OutProductId, op.Quantity, plan.Select(c => (c.ProductId, c.Qty))));
        if (stockError is not null)
            return ServiceResult<ProductAssemblyOperationDto>.Failed(stockError);

        var operation = ToEntity(op);
        ApplyComponents(operation, plan);

        _dbContext.ProductAssemblyOperation.Add(operation);
        await SyncCommissionsAsync(operation);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Assembly operation created with Id {OperationId} for OutProductId {OutProductId}",
            operation.Id, operation.OutProductId);

        var created = await WithIncludes(_dbContext.ProductAssemblyOperation)
            .AsNoTracking()
            .FirstAsync(o => o.Id == operation.Id);

        return ServiceResult<ProductAssemblyOperationDto>.Ok(ToDto(created));
    }

    public Task<ServiceResult<bool>> UpdateAsync(long id, ProductAssemblyOperationDto op) =>
        _stock.RunExclusiveAsync(() => UpdateCoreAsync(id, op));

    private async Task<ServiceResult<bool>> UpdateCoreAsync(long id, ProductAssemblyOperationDto op)
    {
        if (op is null)
            return ServiceResult<bool>.Failed("Assembly operation payload is required");

        if (id != op.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.ProductAssemblyOperation
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Assembly operation not found");

        if (await CommissionsLockedAsync(id))
            return ServiceResult<bool>.Failed(LockedMessage);

        var validationError = await ValidateAsync(op);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating assembly operation {OperationId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        var payrollError = await ClosedPayrollMonthAsync(op, existing);
        if (payrollError is not null)
            return ServiceResult<bool>.Failed(payrollError);

        // the stock check compares how the operation affects stock now with how it will after the change
        var plan = await PlanComponentsAsync(existing, op);
        var stockError = await _stock.CheckChangeAsync(new StockExclusion(AssemblyOperationId: id),
            CurrentDeltas(existing),
            StockDeltas.Assembly(op.OutStoreId, op.InStoreId, op.OutProductId, op.Quantity, plan.Select(c => (c.ProductId, c.Qty))));
        if (stockError is not null)
            return ServiceResult<bool>.Failed(stockError);

        // Update mutable fields explicitly rather than blindly overwriting audit/soft-delete fields
        existing.OutProductId = op.OutProductId;
        existing.OutStoreId = op.OutStoreId;
        existing.InStoreId = op.InStoreId;
        existing.Quantity = op.Quantity;
        existing.OperationDate = op.OperationDate;
        existing.OperationType = op.OperationType;
        existing.FirstTechId = NullIfZero(op.FirstTechId);
        existing.SecondTechId = NullIfZero(op.SecondTechId);
        existing.Notes = op.Notes;

        ApplyComponents(existing, plan);
        await SyncCommissionsAsync(existing);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating assembly operation with Id {OperationId}", id);
            throw;
        }

        _logger.LogInformation("Assembly operation with Id {OperationId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public Task<ServiceResult<bool>> DeleteAsync(long id) =>
        _stock.RunExclusiveAsync(() => DeleteCoreAsync(id));

    private async Task<ServiceResult<bool>> DeleteCoreAsync(long id)
    {
        var operation = await _dbContext.ProductAssemblyOperation
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
        if (operation is null)
            return ServiceResult<bool>.NotFound("Assembly operation not found");

        if (await CommissionsLockedAsync(id))
            return ServiceResult<bool>.Failed(LockedMessage);

        // deleting takes the assembled product back out of stock: refused if it was already sold or moved on
        var stockError = await _stock.CheckChangeAsync(new StockExclusion(AssemblyOperationId: id),
            CurrentDeltas(operation), Array.Empty<StockDelta>());
        if (stockError is not null)
            return ServiceResult<bool>.Failed(stockError);

        operation.IsDeleted = true;
        operation.DeletedAtUtc = DateTime.UtcNow;

        foreach (var item in operation.Items.Where(i => !i.IsDeleted))
        {
            item.IsDeleted = true;
            item.DeletedAtUtc = operation.DeletedAtUtc;
        }

        foreach (var c in await _dbContext.EmployeeCommission.Where(c => !c.IsDeleted && c.AssemblyOperationId == id).ToListAsync())
        {
            c.IsDeleted = true;
            c.DeletedAtUtc = operation.DeletedAtUtc;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Assembly operation with Id {OperationId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}
