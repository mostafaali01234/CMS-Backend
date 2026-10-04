// Services/OrderTechHistoryService.cs
using CMS.Application.DTOs.Responses;
using CMS.Domain.Enums;
using CMS.Domain.Models;
using CMS.Application.DTOs;
using CMS.Application.Interfaces.Configuration;
using CMS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMS.Application.Services;

public class OrderTechHistoryService : IOrderTechHistoryService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<OrderTechHistoryService> _logger;

    public OrderTechHistoryService(
        IAppDbContext dbContext,
        ILogger<OrderTechHistoryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private OrderTechHistoryDto ToDto(OrderTechHistory h)
    {
        return new OrderTechHistoryDto
        {
            Id = h.Id,
            OrderId = h.OrderId,
            TechId = h.TechId,
            TechName = h.Tech?.Name ?? "",
            AuditorId = h.AuditorId,
            AuditorName = h.Auditor?.Name ?? "",
            Status = h.Status,
            CreatedAtUtc = h.CreatedAtUtc,
            CreatedBy = h.CreatedBy ?? "",
            UpdatedAtUtc = h.UpdatedAtUtc,
            UpdatedBy = h.UpdatedBy ?? ""
        };
    }

    private OrderTechHistory ToEntity(OrderTechHistoryDto dto)
    {
        return new OrderTechHistory
        {
            Id = dto.Id,
            OrderId = dto.OrderId,
            TechId = dto.TechId,
            AuditorId = dto.AuditorId,
            Status = dto.Status
        };
    }

    private void changeOlderTechStatus(long orderId)
    {
        var olderTechHistories = _dbContext.OrderTechHistory
            .Where(h => h.OrderId == orderId && !h.IsDeleted)
            .ToList();
        foreach (var history in olderTechHistories)
        {
            if (history.Status != TechStatus.current)
            {
                history.Status = TechStatus.old;
            }
        }
        _dbContext.SaveChangesAsync();
    }

    private IQueryable<OrderTechHistory> WithIncludes(IQueryable<OrderTechHistory> query)
    {
        return query
            .Include(h => h.Tech)
            .Include(h => h.Auditor);
    }

    // Centralized validation used by both Create and Update.
    private async Task<string?> ValidateAsync(OrderTechHistoryDto dto)
    {
        if (dto is null)
            return "Order tech history payload is required";

        // --- Order (required FK) ---
        if (dto.OrderId <= 0)
            return "OrderId is required";

        var orderExists = await _dbContext.Order
            .AnyAsync(o => o.Id == dto.OrderId && !o.IsDeleted);

        if (!orderExists)
            return $"Order with Id {dto.OrderId} was not found";

        // --- Tech (required FK) ---
        if (dto.TechId <= 0)
            return "TechId is required";

        var techExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == dto.TechId && !e.IsDeleted);

        if (!techExists)
            return $"Tech (Employee) with Id {dto.TechId} was not found";

        // --- Auditor (required FK) ---
        if (dto.AuditorId <= 0)
            return "AuditorId is required";

        var auditorExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == dto.AuditorId && !e.IsDeleted);

        if (!auditorExists)
            return $"Auditor (Employee) with Id {dto.AuditorId} was not found";

        // --- Status ---
        if (!Enum.IsDefined(typeof(TechStatus), dto.Status))
            return "Status is not a valid value";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<OrderTechHistoryDto>>> GetAllAsync()
    {
        var items = await WithIncludes(_dbContext.OrderTechHistory.Where(z => !z.IsDeleted))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<OrderTechHistoryDto>>.Ok(items.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<OrderTechHistoryDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<OrderTechHistoryDto?>.Failed("A valid Id is required");

        var item = await WithIncludes(_dbContext.OrderTechHistory.Where(z => !z.IsDeleted && z.Id == id))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (item is null)
            return ServiceResult<OrderTechHistoryDto?>.NotFound("Order tech history record not found");

        return ServiceResult<OrderTechHistoryDto?>.Ok(ToDto(item));
    }

    public async Task<ServiceResult<OrderTechHistoryDto>> CreateAsync(OrderTechHistoryDto dto)
    {
        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating order tech history: {Error}", validationError);
            return ServiceResult<OrderTechHistoryDto>.Failed(validationError);
        }

        var entity = ToEntity(dto);

        changeOlderTechStatus(dto.OrderId);

        _dbContext.OrderTechHistory.Add(entity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Order tech history created with Id {HistoryId} for Order {OrderId}", entity.Id, entity.OrderId);

        var created = await WithIncludes(_dbContext.OrderTechHistory)
            .AsNoTracking()
            .FirstAsync(h => h.Id == entity.Id);

        return ServiceResult<OrderTechHistoryDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, OrderTechHistoryDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Order tech history payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.OrderTechHistory.FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Order tech history record not found");

        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating order tech history {HistoryId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        // Update mutable fields explicitly rather than blindly overwriting audit/soft-delete fields
        existing.OrderId = dto.OrderId;
        existing.TechId = dto.TechId;
        existing.AuditorId = dto.AuditorId;
        existing.Status = dto.Status;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating order tech history with Id {HistoryId}", id);
            throw;
        }

        _logger.LogInformation("Order tech history with Id {HistoryId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var entity = await _dbContext.OrderTechHistory.FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted);
        if (entity is null)
            return ServiceResult<bool>.NotFound("Order tech history record not found");

        entity.IsDeleted = true;
        entity.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Order tech history with Id {HistoryId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}