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

    private string getOrderCategory(Order order)
    {
        return (order.Items.OrderByDescending(z => z.ProductNetTotal).FirstOrDefault()?.Product?.Category?.Name ?? "");
    }

    private OrderDto ToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            InstallationDate = order.InstallationDate,
            Status = order.Status,
            Source = order.Source,
            Category = order.Category,
            ItemsCategory = order.ItemsCategory,
            Type = order.Type,
            Total = order.Total,
            Discount = order.Discount,
            NetTotal = order.NetTotal,
            Shipping = order.Shipping,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer?.Name ?? "",
            CustomerPhone = order.Customer?.Phone ?? "",
            CityId = order.CityId,
            CityName = order.City?.Name ?? "",
            CountryStateName = order.City?.State?.Name ?? "", 
            LineId = order.LineId,
            LineName = order.line?.Name ?? "",
            LocationNotes = order.LocationNotes,
            SellerId = order.SellerId,
            SellerName = order.Seller?.Name ?? "",
            TechId = order.TechHistory.FirstOrDefault(z => z.Status == TechStatus.current)?.TechId ?? 0,
            TechName = order.TechHistory.FirstOrDefault(z => z.Status == TechStatus.current)?.Tech?.Name ?? "",
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
            OldOrderId = order.OldOrderId
        };
    }

    private IQueryable<Order> WithIncludes(IQueryable<Order> query)
    {
        return query
            .Include(o => o.Customer)
            .Include(o => o.City).ThenInclude(c => c.State)
            .Include(o => o.line)
            .Include(o => o.Seller)
            .Include(o => o.Auditor)
            .Include(o => o.Notes)
            .Include(o => o.TechHistory)
            .Include(o => o.Items!.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Product).ThenInclude(z => z.Category);
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
        //if (dto.LineId <= 0)
        //    return "LineId is required";

        //var lineExists = await _dbContext.OrderLine.AnyAsync(l => l.Id == dto.LineId);
        //if (!lineExists)
        //    return $"Line with Id {dto.LineId} was not found";

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
    
    private async Task<string?> ValidateMaintenanceAsync(OrderMaintenanceRequestDto dto)
    {
        if (dto is null)
            return "Maintenance payload is required";

        var oldOrderExists = await _dbContext.Order.FirstOrDefaultAsync(c => c.Id == dto.OldOrderId && c.Status == OrderStatus.تام);
        if (oldOrderExists == null)
            return $"Old Order with Id {dto.OldOrderId} was not found";


        if (oldOrderExists.Type != OrderType.تركيب)
            return "لا يمكن عمل صيانة لاوردر جملة";

        // --- Auditor (required FK) ---
        if (dto.AuditorId <= 0)
            return "AuditorId is required";

        var auditorExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == dto.AuditorId && !e.IsDeleted);

        if (!auditorExists)
            return $"Auditor (Employee) with Id {dto.AuditorId} was not found";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<OrderDto>>> GetAllAsync()
    {
        var orders = await WithIncludes(_dbContext.Order.Where(z => !z.IsDeleted))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<OrderDto>>.Ok(orders.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<List<OrderDto>>> GetDoneByCustomerIdAsync(long customerId)
    {
        var orders = await WithIncludes(_dbContext.Order.Where(z => !z.IsDeleted && z.CustomerId == customerId && z.Status == OrderStatus.تام))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<OrderDto>>.Ok(orders.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<List<OrderDto>>> GetOldMaintenanceAsync(long orderId)
    {
        var orders = await WithIncludes(_dbContext.Order.Where(z => !z.IsDeleted && z.OldOrderId == orderId))
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<OrderDto>>.Ok(orders.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<PagedResult<OrderDto>>> GetPagedAsync(OrderQueryDto q)
    {
        var page = Math.Max(q.Page, 1);
        var pageSize = Math.Clamp(q.PageSize, 1, 100);

        var query = _dbContext.Order.Where(o => true);
        //var query = _dbContext.Order.Where(o => !o.IsDeleted);

        // ---- dates: default to today when neither is provided ----
        var from = q.DateFrom;
        var to = q.DateTo;
        if (from is null && to is null)
            from = to = DateTime.Today;

        if (from is not null)
        {
            var f = from.Value.Date;
            query = query.Where(o => o.CreatedAtUtc >= f);
        }
        if (to is not null)
        {
            var tEx = to.Value.Date.AddDays(1);     
            query = query.Where(o => o.CreatedAtUtc < tEx);
        }

        // ---- filters ----
        if (q.Status is not null) query = query.Where(o => o.Status == q.Status);
        if (q.Type is not null) query = query.Where(o => o.Type == q.Type);
        if (q.CategoryType is not null) query = query.Where(o => o.Category == q.CategoryType);
        if (q.Source is not null) query = query.Where(o => o.Source == q.Source);
        if (q.SellerId is > 0) query = query.Where(o => o.SellerId == q.SellerId);
        if (q.CountryStateId is > 0) query = query.Where(o => o.City!.StateId == q.CountryStateId); // adjust to your FK name
        if (!string.IsNullOrWhiteSpace(q.ItemsCategory))
        {
            var cat = q.ItemsCategory.Trim();
            query = query.Where(o => o.ItemsCategory == cat);
        }
        if (q.TechId is > 0)
        {
            var techId = q.TechId;
            query = query.Where(o => o.TechHistory.Any(t => t.Status == TechStatus.current && t.TechId == techId));
        }

        // ---- search ----
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            var isId = long.TryParse(s, out var searchId);
            query = query.Where(o =>
                (isId && o.Id == searchId) ||
                o.Customer!.Name.Contains(s) ||
                o.Customer.Phone.Contains(s) ||
                o.City!.Name.Contains(s) ||
                o.line!.Name.Contains(s) ||
                o.Seller!.Name.Contains(s) ||
                o.TechHistory.Any(t => t.Status == TechStatus.current && t.Tech!.Name.Contains(s)));
        }

        // count BEFORE includes (cheap query)
        var total = await query.CountAsync();

        var orders = await WithIncludes(
                query.OrderByDescending(o => o.CreatedAtUtc).ThenByDescending(o => o.Id)
                     .Skip((page - 1) * pageSize)
                     .Take(pageSize))
            //.AsSplitQuery()          // avoids cartesian explosion from Items/Notes/TechHistory
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<PagedResult<OrderDto>>.Ok(new PagedResult<OrderDto>
        {
            Items = orders.Select(ToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
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
            Category = dto.Shipping ? OrderCategory.شحن : (dto.OldOrderId != null ? OrderCategory.صيانة : OrderCategory.تركيب),
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

        var added = await WithIncludes(_dbContext.Order.Where(z => z.Id == order.Id)).FirstOrDefaultAsync();
        order.ItemsCategory = getOrderCategory(added);

        #region Notes
        await _noteService.CreateAsync(new OrderNoteHistoryDto
        {
            OrderId = order.Id,
            Notes = $"تم إنشاء الطلب رقم {order.Id} ({order.Items.Count} منتجات) بنجاح.",
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
        }, true);
        if(techAdded != null)
        {
            _logger.LogInformation("Tech history created for Order Id {OrderId} with Tech Id {TechId}", order.Id, techId);
            //await _noteService.CreateAsync(new OrderNoteHistoryDto
            //{
            //    OrderId = order.Id,
            //    Notes = techNote.Replace("#OrderId", order.Id.ToString()),
            //    CreatedBy = dto.CreatedBy ?? "System",
            //    AuditorId = dto.AuditorId
            //});

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

    public async Task<ServiceResult<OrderDto>> CreateMaintenanceAsync(OrderMaintenanceRequestDto dto)
    {
        var validationError = await ValidateMaintenanceAsync(dto);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating order: {Error}", validationError);
            return ServiceResult<OrderDto>.Failed(validationError);
        }
        var oldOrder = await WithIncludes(_dbContext.Order.Where(z => z.Id == dto.OldOrderId)).AsNoTracking().FirstOrDefaultAsync();

        var order = new Order
        {
            OldOrderId = oldOrder.Id,
            InstallationDate = DateTime.Today,
            Status = OrderStatus.جديد,
            Source = oldOrder.Source,
            Type = oldOrder.Type,
            Total = 0,
            Discount = 0,
            NetTotal = 0,
            Shipping = false,
            Category = OrderCategory.صيانة,
            ItemsCategory = oldOrder.ItemsCategory,
            CustomerId = oldOrder.CustomerId,
            CityId = oldOrder.CityId,
            LineId = oldOrder.LineId,
            LocationNotes = oldOrder.LocationNotes,
            SellerId = oldOrder.SellerId,
            AuditorId = dto.AuditorId,
            AttachmentImage = oldOrder.AttachmentImage,
            //Items = dto.Items.Select(i => new OrderItem
            //{
            //    ProductId = i.ProductId,
            //    ProductPrice = i.ProductPrice,
            //    ProductQuantity = i.ProductQuantity,
            //    ProductTotal = i.ProductTotal,
            //    ProductDiscount = i.ProductDiscount,
            //    ProductNetTotal = i.ProductNetTotal
            //}).ToList()
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
            Notes = $"تم إنشاء طلب الصيانة رقم {order.Id}  بنجاح.",
            CreatedBy = dto.CreatedBy ?? "System",
            AuditorId = dto.AuditorId
        });
        await _noteService.CreateAsync(new OrderNoteHistoryDto
        {
            OrderId = order.Id,
            Notes = dto.Notes,
            CreatedBy = dto.CreatedBy ?? "System",
            AuditorId = dto.AuditorId
        });
        #endregion

        #region Tech
        var techId = await _dbContext.OrderTechHistory.FirstOrDefaultAsync(z => z.OrderId == dto.OldOrderId && z.Status == TechStatus.current);
        if (techId != null)
        {
            var techAdded = await _techService.CreateAsync(new OrderTechHistoryDto
            {
                OrderId = order.Id,
                TechId = techId.TechId,
                Status = TechStatus.current,
                AuditorId = dto.AuditorId,
                CreatedBy = dto.CreatedBy ?? "System"
            }, true);
            if(techAdded != null)
            {
                _logger.LogInformation("Tech history created for Order Id {OrderId} with Tech Id {TechId}", order.Id, techId);

                order.Status = OrderStatus.قيد_التنفيذ;
                await _dbContext.SaveChangesAsync();
            }
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

        var existing = await WithIncludes(_dbContext.Order
            .Where(o => o.Id == id && !o.IsDeleted)).FirstOrDefaultAsync();

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

            existing.ItemsCategory = getOrderCategory(existing);

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

    public async Task<ServiceResult<bool>> CancelAsync(long id, OrderCancelDto body)
    {
        var order = await _dbContext.Order
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        if (order is null)
            return ServiceResult<bool>.NotFound("Order not found");

        order.Status = OrderStatus.ملغي;

        await _dbContext.SaveChangesAsync();


        await _noteService.CreateAsync(new OrderNoteHistoryDto
        {
            OrderId = order.Id,
            Notes = $"تم الغاء الطلب رقم {order.Id}.",
            CreatedBy = body.CreatedBy ?? "System",
            AuditorId = body.AuditorId
        });

        _logger.LogInformation("Order with Id {OrderId} canceled successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id, OrderCancelDto body)
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



        await _noteService.CreateAsync(new OrderNoteHistoryDto
        {
            OrderId = order.Id,
            Notes = $"تم حذف الطلب رقم {order.Id}.",
            CreatedBy = body.CreatedBy ?? "System",
            AuditorId = body.AuditorId
        });

        _logger.LogInformation("Order with Id {OrderId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}