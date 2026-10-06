// Services/EmployeeCommissionService.cs
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using CMS.Domain.DTOs;
using CMS.Domain.Enums;
using CMS.Domain.Models;
using CMS.Application.Interfaces.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMS.Application.Services;

public class EmployeeCommissionService : IEmployeeCommissionService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<EmployeeCommissionService> _logger;

    public EmployeeCommissionService(
        IAppDbContext dbContext,
        ILogger<EmployeeCommissionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private EmployeeCommissionDto ToDto(EmployeeCommission c)
    {
        return new EmployeeCommissionDto
        {
            Id = c.Id,
            OrderId = c.OrderId,
            InvoiceId = c.InvoiceId,
            InvoiceNumber = c.Invoice?.Number ?? 0,
            EmployeeId = c.EmployeeId,
            EmployeeName = c.Employee?.Name ?? "",
            EmployeeRole = c.EmployeeRole,
            ProductId = c.ProductId,
            ProductName = c.Product?.Name ?? "",
            ProductTotal = c.ProductTotal,
            Amount = c.Amount,
            ExtraAmount = c.Extra_Amount,
            Status = c.Status,
            PaidAt = c.PaidAt,
            WithdrawnAt = c.WithdrawnAt,
            CreatedAtUtc = c.CreatedAtUtc,
            CreatedBy = c.CreatedBy ?? "",
            UpdatedAtUtc = c.UpdatedAtUtc,
            UpdatedBy = c.UpdatedBy ?? ""
        };
    }

    private IQueryable<EmployeeCommission> WithIncludes(IQueryable<EmployeeCommission> query)
    {
        return query
            .Include(c => c.Invoice)
            .Include(c => c.Employee)
            .Include(c => c.Product);
    }

    // Centralized validation used by both Create and Update.
    private async Task<string?> ValidateAsync(EmployeeCommissionDto dto, bool isUpdate, long? currentId = null)
    {
        if (dto is null)
            return "Commission payload is required";

        // --- Order (required FK) ---
        if (dto.OrderId <= 0)
            return "OrderId is required";

        var orderExists = await _dbContext.Order
            .AnyAsync(o => o.Id == dto.OrderId && !o.IsDeleted);

        if (!orderExists)
            return $"Order with Id {dto.OrderId} was not found";

        // --- Invoice (required FK, must belong to the same order) ---
        if (dto.InvoiceId <= 0)
            return "InvoiceId is required";

        var invoice = await _dbContext.SaleInvoice
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == dto.InvoiceId && !i.IsDeleted);

        if (invoice is null)
            return $"Invoice with Id {dto.InvoiceId} was not found";

        if (invoice.OrderId != dto.OrderId)
            return $"Invoice {dto.InvoiceId} does not belong to order {dto.OrderId}";

        // --- Employee (required FK) ---
        if (dto.EmployeeId <= 0)
            return "EmployeeId is required";

        var employeeExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == dto.EmployeeId && !e.IsDeleted);

        if (!employeeExists)
            return $"Employee with Id {dto.EmployeeId} was not found";

        // --- EmployeeRole ---
        if (!Enum.IsDefined(typeof(CommissionRole), dto.EmployeeRole))
            return "EmployeeRole is not a valid value";

        // --- Product (required FK, must appear on the invoice) ---
        if (dto.ProductId <= 0)
            return "ProductId is required";

        var productOnInvoice = await _dbContext.SaleInvoiceItem
            .AnyAsync(i => !i.IsDeleted && i.InvoiceId == dto.InvoiceId && i.ProductId == dto.ProductId);

        if (!productOnInvoice)
            return $"Product with Id {dto.ProductId} was not found on invoice {dto.InvoiceId}";

        // --- Uniqueness: one commission per (Invoice, Employee, Role, Product) ---
        // Allows the same employee to earn commission under different roles (e.g. Seller
        // and Tech) on the same product, but not twice under the same role.
        var duplicateQuery = _dbContext.EmployeeCommission.Where(c =>
            !c.IsDeleted &&
            c.InvoiceId == dto.InvoiceId &&
            c.EmployeeId == dto.EmployeeId &&
            c.EmployeeRole == dto.EmployeeRole &&
            c.ProductId == dto.ProductId);

        if (isUpdate && currentId.HasValue)
            duplicateQuery = duplicateQuery.Where(c => c.Id != currentId.Value);

        if (await duplicateQuery.AnyAsync())
            return $"A commission for employee {dto.EmployeeId} with role '{dto.EmployeeRole}' on product {dto.ProductId} already exists for this invoice";

        // --- ProductTotal / Amount / ExtraAmount ---
        if (dto.ProductTotal < 0)
            return "ProductTotal cannot be negative";

        if (dto.Amount < 0)
            return "Amount cannot be negative";

        //if (dto.ExtraAmount < 0)
        //    return "ExtraAmount cannot be negative";

        // --- Status ---
        if (!Enum.IsDefined(typeof(CommissionStatus), dto.Status))
            return "Status is not a valid value";

        // --- PaidAt / WithdrawnAt consistency ---
        // Assumed: WithdrawnAt can only be set once the commission has actually been PaidAt.
        if (dto.WithdrawnAt.HasValue && !dto.PaidAt.HasValue)
            return "WithdrawnAt cannot be set without PaidAt also being set";

        if (dto.PaidAt.HasValue && dto.WithdrawnAt.HasValue && dto.WithdrawnAt.Value < dto.PaidAt.Value)
            return "WithdrawnAt cannot be earlier than PaidAt";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<EmployeeCommissionDto>>> GetAllAsync()
    {
        var items = await WithIncludes(_dbContext.EmployeeCommission.Where(z => !z.IsDeleted))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<EmployeeCommissionDto>>.Ok(items.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<CommissionPageDto>> GetByEmployeeAndMonthAsync(
    long employeeId, int year, int month)
    {
        if (employeeId <= 0)
            return ServiceResult<CommissionPageDto>.Failed("A valid employee Id is required");
        var employee = await _dbContext.Employee
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted);
        if (employee == null)
            return ServiceResult<CommissionPageDto>.Failed($"Employee with Id {employeeId} was not found");

        if (month < 1 || month > 12)
            return ServiceResult<CommissionPageDto>.Failed("Month must be between 1 and 12");

        if (year < 2000 || year > 2100)
            return ServiceResult<CommissionPageDto>.Failed("Year is not valid");

        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var items = await WithIncludes(
                _dbContext.EmployeeCommission.Where(c =>
                    !c.IsDeleted &&
                    c.EmployeeId == employeeId &&
                    c.CreatedAtUtc >= start &&
                    c.CreatedAtUtc < end))
            .AsNoTracking()
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync();
        var list = items.Select(ToDto).ToList();
        var result = new CommissionPageDto() {
            CommissionTotal = list.Sum(c => c.Amount + c.ExtraAmount),
            SalesCommissions = list.Where(c => c.EmployeeRole == CommissionRole.Sales).ToList(),
            SalesTotal = list.Where(c => c.EmployeeRole == CommissionRole.Sales).Sum(c => c.Amount + c.ExtraAmount),
            TechCommissions = list.Where(c => c.EmployeeRole == CommissionRole.Tech).ToList(),
            TechTotal = list.Where(c => c.EmployeeRole == CommissionRole.Tech).Sum(c => c.Amount + c.ExtraAmount),
            EmployeeBaseSalary = employee.Salary,
            InvoicesTotal = items.Select(z => z.Invoice)?.Distinct().Sum(c => c?.NetTotal ?? 0) ?? 0
        };

        return ServiceResult<CommissionPageDto>.Ok(result);
    }

    public async Task<ServiceResult<EmployeeCommissionDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<EmployeeCommissionDto?>.Failed("A valid commission Id is required");

        var item = await WithIncludes(_dbContext.EmployeeCommission.Where(z => !z.IsDeleted && z.Id == id))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (item is null)
            return ServiceResult<EmployeeCommissionDto?>.NotFound("Commission not found");

        return ServiceResult<EmployeeCommissionDto?>.Ok(ToDto(item));
    }

    public async Task<ServiceResult<EmployeeCommissionDto>> CreateAsync(EmployeeCommissionDto dto)
    {
        var validationError = await ValidateAsync(dto, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating commission: {Error}", validationError);
            return ServiceResult<EmployeeCommissionDto>.Failed(validationError);
        }

        var entity = new EmployeeCommission
        {
            OrderId = dto.OrderId,
            InvoiceId = dto.InvoiceId,
            EmployeeId = dto.EmployeeId,
            EmployeeRole = dto.EmployeeRole,
            ProductId = dto.ProductId,
            ProductTotal = dto.ProductTotal,
            Amount = dto.Amount,
            Extra_Amount = dto.ExtraAmount,
            Status = dto.Status,
            PaidAt = dto.PaidAt,
            WithdrawnAt = dto.WithdrawnAt
        };

        _dbContext.EmployeeCommission.Add(entity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Commission created with Id {CommissionId} for Employee {EmployeeId} on Invoice {InvoiceId}",
            entity.Id, entity.EmployeeId, entity.InvoiceId);

        var created = await WithIncludes(_dbContext.EmployeeCommission)
            .AsNoTracking()
            .FirstAsync(c => c.Id == entity.Id);

        return ServiceResult<EmployeeCommissionDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, EmployeeCommissionDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Commission payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.EmployeeCommission.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Commission not found");

        // Once withdrawn, the commission is a closed financial record.
        if (existing.WithdrawnAt.HasValue)
            return ServiceResult<bool>.Failed("This commission has already been withdrawn and cannot be edited");

        var validationError = await ValidateAsync(dto, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating commission {CommissionId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        existing.OrderId = dto.OrderId;
        existing.InvoiceId = dto.InvoiceId;
        existing.EmployeeId = dto.EmployeeId;
        existing.EmployeeRole = dto.EmployeeRole;
        existing.ProductId = dto.ProductId;
        existing.ProductTotal = dto.ProductTotal;
        existing.Amount = dto.Amount;
        existing.Extra_Amount = dto.ExtraAmount;
        existing.Status = dto.Status;
        existing.PaidAt = dto.PaidAt;
        existing.WithdrawnAt = dto.WithdrawnAt;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating commission with Id {CommissionId}", id);
            throw;
        }

        _logger.LogInformation("Commission with Id {CommissionId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var entity = await _dbContext.EmployeeCommission.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (entity is null)
            return ServiceResult<bool>.NotFound("Commission not found");

        if (entity.WithdrawnAt.HasValue)
            return ServiceResult<bool>.Failed("This commission has already been withdrawn and cannot be deleted");

        entity.IsDeleted = true;
        entity.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Commission with Id {CommissionId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}