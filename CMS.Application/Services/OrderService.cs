// Services/OrderService.cs
using CMS.Application.DTOs.Responses;
using CMS.Domain.Enums;
using CMS.Domain.Models;
using CMS.Application.DTOs;
using CMS.Application.Interfaces.Configuration;
using CMS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMS.Application.Services;

public class OrderService : IOrderService
{
    private readonly IAppDbContext _dbContext;
    private readonly IOrderNoteHistoryService _noteService;
    private readonly IOrderTechHistoryService _techService;
    private readonly ILogger<OrderService> _logger;

    private const int LocationNotesMaxLength = 2000;
    private const int AttachmentImageMaxLength = 500;
    private const decimal RoundingTolerance = 0.01m;

    public OrderService(
        IAppDbContext dbContext,
        IOrderNoteHistoryService noteService,
        IOrderTechHistoryService techService,
        ILogger<OrderService> logger)
    {
        _dbContext = dbContext;
        _noteService = noteService;
        _techService = techService;
        _logger = logger;
    }

    private OrderDto ToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            InstallationDate = order.InstallationDate,
            Status = order.Status,
            Source = order.Source,
            Type = order.Type,
            Total = order.Total,
            Discount = order.Discount,
            NetTotal = order.NetTotal,
            Shipping = order.Shipping,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer?.Name ?? "",
            CityId = order.CityId,
            CityName = order.City?.Name ?? "",
            LineId = order.LineId,
            LineName = order.line?.Name ?? "",
            LocationNotes = order.LocationNotes,
            SellerId = order.SellerId,
            SellerName = order.Seller?.Name ?? "",
            TechId = order.TechHistory.FirstOrDefault(z => z.Status == TechStatus.current)?.TechId ?? 0,
            TechName = order.TechHistory.FirstOrDefault(z => z.Status == TechStatus.current)?.Tech.Name ?? "",
            AuditorId = order.AuditorId,
            AuditorName = order.Auditor?.Name ?? "",
            AttachmentImage = order.AttachmentImage,
            Items = order.Items?.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? "",
                ProductPrice = i.ProductPrice,
                ProductQuantity = i.ProductQuantity,
                ProductTotal = i.ProductTotal,
                ProductDiscount = i.ProductDiscount,
                ProductNetTotal = i.ProductNetTotal
            }).ToList() ?? new List<OrderItemDto>(),
            CreatedAtUtc = order.CreatedAtUtc,
            CreatedBy = order.CreatedBy ?? "",
            UpdatedAtUtc = order.UpdatedAtUtc,
            UpdatedBy = order.UpdatedBy ?? "",
            Notes = order.Notes?.Select(n => new OrderNoteHistoryDto
            {
                Id = n.Id,
                Notes = n.Notes ?? "",
                CreatedAtUtc = n.CreatedAtUtc,
                CreatedBy = n.CreatedBy ?? ""
            }).ToList() ?? new List<OrderNoteHistoryDto>(),
        };
    }

    private IQueryable<Order> WithIncludes(IQueryable<Order> query)
    {
        return query
            .Include(o => o.Customer)
            .Include(o => o.City)
            .Include(o => o.line)
            .Include(o => o.Seller)
            .Include(o => o.Auditor)
            .Include(o => o.Notes)
            .Include(o => o.TechHistory)
            .Include(o => o.Items!.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Product);
    }

    // Recomputes each item's totals from price/quantity/discount, then recomputes the
    // order-level Total/Discount/NetTotal as the sum of the (now-trusted) item values.
    // This means a caller only needs to send price/quantity/discount per item — the
    // various totals are never trusted as client input.
    private void RecalculateTotals(OrderDto dto)
    {
        foreach (var item in dto.Items)
        {
            item.ProductTotal = Math.Round(item.ProductPrice * item.ProductQuantity, 2);
            item.ProductNetTotal = Math.Round(item.ProductTotal - item.ProductDiscount, 2);
        }

        dto.Total = Math.Round(dto.Items.Sum(i => i.ProductTotal), 2);
        dto.Discount = Math.Round(dto.Items.Sum(i => i.ProductDiscount), 2);
        dto.NetTotal = Math.Round(dto.Total - dto.Discount, 2);
    }

    private (long lineId, long techId, string message) getLineTech(OrderDto dto)
    {
        var cityId = dto.CityId;
        var line = _dbContext.OrderLine.Include(z => z.Manager).FirstOrDefault(l => l.Cities.Any(z => z.Id == cityId));
        if (line == null) {
            _logger.LogWarning("No line found for cityId {CityId}", cityId);
            return (0, 0, "");
        }
        var message = $"تم تعيين الخط {line.Name} والفني {line.Manager.Name} تلقائيا لاوردر رقم #OrderId";

        return (line.Id, line.ManagerId, message);
    }

    // Centralized validation used by both Create and Update.
    private async Task<string?> ValidateAsync(OrderDto dto)
    {
        if (dto is null)
            return "Order payload is required";

        // --- InstallationDate ---
        if (dto.InstallationDate == default)
            return "InstallationDate is required";

        // --- Enums ---
        if (dto.Status != null && !Enum.IsDefined(typeof(OrderStatus), dto.Status))
            return "Status is not a valid value";

        if (!Enum.IsDefined(typeof(OrderSource), dto.Source))
            return "Source is not a valid value";

        if (!Enum.IsDefined(typeof(OrderType), dto.Type))
            return "Type is not a valid value";

        // --- Customer (required FK) ---
        if (dto.CustomerId <= 0)
            return "CustomerId is required";

        var customerExists = await _dbContext.Customer
            .AnyAsync(c => c.Id == dto.CustomerId && !c.IsDeleted);

        if (!customerExists)
            return $"Customer with Id {dto.CustomerId} was not found";

        // --- City (required FK) ---
        if (dto.CityId <= 0)
            return "CityId is required";

        var cityExists = await _dbContext.City.AnyAsync(c => c.Id == dto.CityId);
        if (!cityExists)
            return $"City with Id {dto.CityId} was not found";

        // --- Line (required FK) ---
        if (dto.LineId <= 0)
            return "LineId is required";

        var lineExists = await _dbContext.OrderLine.AnyAsync(l => l.Id == dto.LineId);
        if (!lineExists)
            return $"Line with Id {dto.LineId} was not found";

        // --- Seller (required FK) ---
        if (dto.SellerId <= 0)
            return "SellerId is required";

        var sellerExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == dto.SellerId && !e.IsDeleted);

        if (!sellerExists)
            return $"Seller (Employee) with Id {dto.SellerId} was not found";

        // --- Auditor (required FK) ---
        if (dto.AuditorId <= 0)
            return "AuditorId is required";

        var auditorExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == dto.AuditorId && !e.IsDeleted);

        if (!auditorExists)
            return $"Auditor (Employee) with Id {dto.AuditorId} was not found";

        // --- LocationNotes (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(dto.LocationNotes) && dto.LocationNotes.Length > LocationNotesMaxLength)
            return $"LocationNotes cannot exceed {LocationNotesMaxLength} characters";

        // --- AttachmentImage (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(dto.AttachmentImage) && dto.AttachmentImage.Length > AttachmentImageMaxLength)
            return $"AttachmentImage path/URL cannot exceed {AttachmentImageMaxLength} characters";

        // --- Items (at least one required) ---
        if (dto.Items is null || dto.Items.Count == 0)
            return "At least one order item is required";

        var seenProductIds = new HashSet<long>();

        foreach (var item in dto.Items)
        {
            if (item.ProductId <= 0)
                return "Each order item must have a valid ProductId";

            if (!seenProductIds.Add(item.ProductId))
                return $"Product Id {item.ProductId} appears more than once in the order items";

            if (item.ProductPrice < 0)
                return $"ProductPrice for product Id {item.ProductId} cannot be negative";

            if (item.ProductQuantity <= 0)
                return $"ProductQuantity for product Id {item.ProductId} must be greater than zero";

            if (item.ProductDiscount < 0)
                return $"ProductDiscount for product Id {item.ProductId} cannot be negative";

            var lineTotal = Math.Round(item.ProductPrice * item.ProductQuantity, 2);
            if (item.ProductDiscount > lineTotal)
                return $"ProductDiscount for product Id {item.ProductId} cannot exceed the line total ({lineTotal})";

            var productExists = await _dbContext.Product
                .AnyAsync(p => p.Id == item.ProductId && !p.IsDeleted);

            if (!productExists)
                return $"Product with Id {item.ProductId} was not found";
        }

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<OrderDto>>> GetAllAsync()
    {
        var orders = await WithIncludes(_dbContext.Order.Where(z => !z.IsDeleted))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<OrderDto>>.Ok(orders.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<OrderDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<OrderDto?>.Failed("A valid order Id is required");

        var order = await WithIncludes(_dbContext.Order.Where(z => !z.IsDeleted && z.Id == id))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (order is null)
            return ServiceResult<OrderDto?>.NotFound("Order not found");

        return ServiceResult<OrderDto?>.Ok(ToDto(order));
    }

    public async Task<ServiceResult<OrderDto>> CreateAsync(OrderDto dto)
    {
        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating order: {Error}", validationError);
            return ServiceResult<OrderDto>.Failed(validationError);
        }

        RecalculateTotals(dto);

        long lineId, techId = 0;
        string techNote = "";
        (lineId, techId, techNote) = getLineTech(dto);

        var order = new Order
        {
            InstallationDate = dto.InstallationDate,
            Status = OrderStatus.جديد,
            Source = dto.Source,
            Type = dto.Type,
            Total = dto.Total,
            Discount = dto.Discount,
            NetTotal = dto.NetTotal,
            Shipping = dto.Shipping,
            CustomerId = dto.CustomerId,
            CityId = dto.CityId,
            LineId = lineId,
            LocationNotes = dto.LocationNotes,
            SellerId = dto.SellerId,
            AuditorId = dto.AuditorId,
            AttachmentImage = dto.AttachmentImage,
            Items = dto.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductPrice = i.ProductPrice,
                ProductQuantity = i.ProductQuantity,
                ProductTotal = i.ProductTotal,
                ProductDiscount = i.ProductDiscount,
                ProductNetTotal = i.ProductNetTotal
            }).ToList()
        };

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

            _dbContext.Order.Add(order);
            await _dbContext.SaveChangesAsync();

            await dbTransaction.CommitAsync();
        });

        #region Notes
        await _noteService.CreateAsync(new OrderNoteHistoryDto
        {
            OrderId = order.Id,
            Notes = $"تم إنشاء الطلب رقم {order.Id} ({order.Items.Count} منتجات) بنجاح. {techNote}",
            CreatedBy = dto.CreatedBy ?? "System",
            AuditorId = dto.AuditorId
        });
        #endregion

        #region Tech
        var techAdded = await _techService.CreateAsync(new OrderTechHistoryDto
        {
            OrderId = order.Id,
            TechId = techId,
            Status = TechStatus.current,
            AuditorId = dto.AuditorId,
            CreatedBy = dto.CreatedBy ?? "System"
        });
        if(techAdded != null)
        {
            _logger.LogInformation("Tech history created for Order Id {OrderId} with Tech Id {TechId}", order.Id, techId);
            await _noteService.CreateAsync(new OrderNoteHistoryDto
            {
                OrderId = order.Id,
                Notes = techNote.Replace("#OrderId", order.Id.ToString()),
                CreatedBy = dto.CreatedBy ?? "System",
                AuditorId = dto.AuditorId
            });

            order.Status = OrderStatus.قيد_التنفيذ;
            await _dbContext.SaveChangesAsync();

        }
        #endregion

        _logger.LogInformation("Order created with Id {OrderId} ({ItemCount} items)", order.Id, order.Items.Count);

        var created = await WithIncludes(_dbContext.Order)
            .AsNoTracking()
            .FirstAsync(o => o.Id == order.Id);

        return ServiceResult<OrderDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, OrderDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Order payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.Order
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        if (existing is null)
            return ServiceResult<bool>.NotFound("Order not found");

        var validationError = await ValidateAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating order {OrderId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        RecalculateTotals(dto);

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

            existing.InstallationDate = dto.InstallationDate;
            //existing.Status = (OrderStatus)dto.Status;
            existing.Source = dto.Source;
            existing.Type = dto.Type;
            existing.Total = dto.Total;
            existing.Discount = dto.Discount;
            existing.NetTotal = dto.NetTotal;
            existing.Shipping = dto.Shipping;
            existing.CustomerId = dto.CustomerId;
            existing.CityId = dto.CityId;
            //existing.LineId = (long)dto.LineId;
            existing.LocationNotes = dto.LocationNotes;
            existing.SellerId = dto.SellerId;
            //existing.AuditorId = dto.AuditorId;
            //existing.AttachmentImage = dto.AttachmentImage;

            // Replace all order items: remove existing, add the new set
            if (existing.Items is { Count: > 0 })
            {
                _dbContext.OrderItem.RemoveRange(existing.Items);
            }

            var newItems = dto.Items.Select(i => new OrderItem
            {
                OrderId = existing.Id,
                ProductId = i.ProductId,
                ProductPrice = i.ProductPrice,
                ProductQuantity = i.ProductQuantity,
                ProductTotal = i.ProductTotal,
                ProductDiscount = i.ProductDiscount,
                ProductNetTotal = i.ProductNetTotal
            }).ToList();

            await _dbContext.OrderItem.AddRangeAsync(newItems);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error occurred while updating order with Id {OrderId}", id);
                throw;
            }

            await dbTransaction.CommitAsync();
        });

        _logger.LogInformation("Order with Id {OrderId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> CancelAsync(long id)
    {
        var order = await _dbContext.Order
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        if (order is null)
            return ServiceResult<bool>.NotFound("Order not found");

        order.Status = OrderStatus.ملغي;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Order with Id {OrderId} canceled successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var order = await _dbContext.Order
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        if (order is null)
            return ServiceResult<bool>.NotFound("Order not found");

        var now = DateTime.UtcNow;

        order.IsDeleted = true;
        order.DeletedAtUtc = now;
        order.Status = OrderStatus.محذوف;

        if (order.Items is { Count: > 0 })
        {
            foreach (var item in order.Items.Where(i => !i.IsDeleted))
            {
                item.IsDeleted = true;
                item.DeletedAtUtc = now;
            }
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Order with Id {OrderId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}