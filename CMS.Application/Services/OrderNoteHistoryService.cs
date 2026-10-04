// Services/OrderNoteHistoryService.cs
using CMS.Application.DTOs.Responses;
using CMS.Domain.Models;
using CMS.Application.DTOs;
using CMS.Application.Interfaces.Configuration;
using CMS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMS.Application.Services;

public class OrderNoteHistoryService : IOrderNoteHistoryService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<OrderNoteHistoryService> _logger;

    private const int NotesMaxLength = 2000;

    public OrderNoteHistoryService(
        IAppDbContext dbContext,
        ILogger<OrderNoteHistoryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private OrderNoteHistoryDto ToDto(OrderNoteHistory n)
    {
        return new OrderNoteHistoryDto
        {
            Id = n.Id,
            OrderId = n.OrderId,
            AuditorId = n.AuditorId,
            AuditorName = n.Auditor?.Name ?? "",
            Notes = n.Notes,
            CreatedAtUtc = n.CreatedAtUtc,
            CreatedBy = n.CreatedBy ?? "",
            UpdatedAtUtc = n.UpdatedAtUtc,
            UpdatedBy = n.UpdatedBy ?? ""
        };
    }

    private OrderNoteHistory ToEntity(OrderNoteHistoryDto dto)
    {
        return new OrderNoteHistory
        {
            Id = dto.Id,
            OrderId = dto.OrderId,
            AuditorId = dto.AuditorId,
            Notes = dto.Notes
        };
    }

    private IQueryable<OrderNoteHistory> WithIncludes(IQueryable<OrderNoteHistory> query)
    {
        return query.Include(n => n.Auditor);
    }

    // Centralized validation used by both Create and Update.
    private async Task<string?> ValidateAsync(OrderNoteHistoryDto dto)
    {
        if (dto is null)
            return "Order note history payload is required";

        // --- Order (required FK) ---
        if (dto.OrderId <= 0)
            return "OrderId is required";

        var orderExists = await _dbContext.Order
            .AnyAsync(o => o.Id == dto.OrderId && !o.IsDeleted);

        if (!orderExists)
            return $"Order with Id {dto.OrderId} was not found";

        // --- Auditor (required FK) ---
        if (dto.AuditorId <= 0)
            return "AuditorId is required";

        var auditorExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == dto.AuditorId && !e.IsDeleted);

        if (!auditorExists)
            return $"Auditor (Employee) with Id {dto.AuditorId} was not found";

        // --- Notes (required, length only) ---
        if (string.IsNullOrWhiteSpace(dto.Notes))
            return "Notes is required";

        if (dto.Notes.Length > NotesMaxLength)
            return $"Notes cannot exceed {NotesMaxLength} characters";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<OrderNoteHistoryDto>>> GetAllAsync()
    {
        var items = await WithIncludes(_dbContext.OrderNoteHistory.Where(z => !z.IsDeleted))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<OrderNoteHistoryDto>>.Ok(items.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<OrderNoteHistoryDto?>> GetByOrderIdAsync(long orderId)
    {
        if (orderId <= 0)
            return ServiceResult<OrderNoteHistoryDto?>.Failed("A valid Id is required");

        var item = await WithIncludes(_dbContext.OrderNoteHistory.Where(z => !z.IsDeleted && z.OrderId == orderId))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (item is null)
            return ServiceResult<OrderNoteHistoryDto?>.NotFound("Order note history record not found");

        return ServiceResult<OrderNoteHistoryDto?>.Ok(ToDto(item));
    }

    public async Task<ServiceResult<OrderNoteHistoryDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<OrderNoteHistoryDto?>.Failed("A valid Id is required");

        var item = await WithIncludes(_dbContext.OrderNoteHistory.Where(z => !z.IsDeleted && z.Id == id))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (item is null)
            return ServiceResult<OrderNoteHistoryDto?>.NotFound("Order note history record not found");

        return ServiceResult<OrderNoteHistoryDto?>.Ok(ToDto(item));
    }

    public async Task<ServiceResult<OrderNoteHistoryDto>> CreateAsync(OrderNoteHistoryDto dto)
    {
        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating order note history: {Error}", validationError);
            return ServiceResult<OrderNoteHistoryDto>.Failed(validationError);
        }

        var entity = ToEntity(dto);

        _dbContext.OrderNoteHistory.Add(entity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Order note history created with Id {NoteId} for Order {OrderId}", entity.Id, entity.OrderId);

        var created = await WithIncludes(_dbContext.OrderNoteHistory)
            .AsNoTracking()
            .FirstAsync(n => n.Id == entity.Id);

        return ServiceResult<OrderNoteHistoryDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, OrderNoteHistoryDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Order note history payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.OrderNoteHistory.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Order note history record not found");

        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating order note history {NoteId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        existing.OrderId = dto.OrderId;
        existing.AuditorId = dto.AuditorId;
        existing.Notes = dto.Notes;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating order note history with Id {NoteId}", id);
            throw;
        }

        _logger.LogInformation("Order note history with Id {NoteId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var entity = await _dbContext.OrderNoteHistory.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
        if (entity is null)
            return ServiceResult<bool>.NotFound("Order note history record not found");

        entity.IsDeleted = true;
        entity.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Order note history with Id {NoteId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}