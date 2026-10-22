// Models/DTOs/OrderDto.cs
using System.Text.Json.Serialization;
using CMS.Domain.Enums;


namespace CMS.Application.DTOs
{
    public class OrderDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("installation_date")]
        public DateTime InstallationDate { get; set; }

        [JsonPropertyName("status")]
        public OrderStatus? Status { get; set; }

        [JsonPropertyName("source")]
        public OrderSource Source { get; set; }

        [JsonPropertyName("type")]
        public OrderType Type { get; set; }

        [JsonPropertyName("category")]
        public OrderCategory Category { get; set; }

        [JsonPropertyName("items_category")]
        public string ItemsCategory { get; set; } = string.Empty;

        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("discount")]
        public decimal Discount { get; set; }

        [JsonPropertyName("net_total")]
        public decimal NetTotal { get; set; }

        [JsonPropertyName("shipping")]
        public bool Shipping { get; set; }

        [JsonPropertyName("customer_id")]
        public long CustomerId { get; set; }

        [JsonPropertyName("customer_name")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("customer_phone")]
        public string CustomerPhone { get; set; } = string.Empty;

        [JsonPropertyName("city_id")]
        public long CityId { get; set; }

        [JsonPropertyName("city_name")]
        public string CityName { get; set; } = string.Empty;

        [JsonPropertyName("country_state_name")]
        public string CountryStateName { get; set; } = string.Empty;

        [JsonPropertyName("line_id")]
        public long? LineId { get; set; }

        [JsonPropertyName("line_name")]
        public string LineName { get; set; } = string.Empty;

        [JsonPropertyName("location_notes")]
        public string LocationNotes { get; set; } = string.Empty;

        [JsonPropertyName("seller_id")]
        public long SellerId { get; set; }

        [JsonPropertyName("seller_name")]
        public string SellerName { get; set; } = string.Empty;

        [JsonPropertyName("tech_id")]
        public long? TechId { get; set; }

        [JsonPropertyName("tech_name")]
        public string TechName { get; set; } = string.Empty;

        [JsonPropertyName("auditor_id")]
        public long AuditorId { get; set; }

        [JsonPropertyName("auditor_name")]
        public string AuditorName { get; set; } = string.Empty;

        [JsonPropertyName("attachment_image")]
        public string AttachmentImage { get; set; } = string.Empty;

        [JsonPropertyName("items")]
        public List<OrderItemDto> Items { get; set; } = new();

        [JsonPropertyName("created_at_utc")]
        public DateTime? CreatedAtUtc { get; set; }

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updated_at_utc")]
        public DateTime? UpdatedAtUtc { get; set; }

        [JsonPropertyName("updated_by")]
        public string UpdatedBy { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public List<OrderNoteHistoryDto> Notes { get; set; } = new();

        [JsonPropertyName("old_order_id")]
        public long? OldOrderId { get; set; }
    }

    public class OrderItemDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("product_id")]
        public long ProductId { get; set; }

        [JsonPropertyName("product_name")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("product_price")]
        public decimal ProductPrice { get; set; }

        [JsonPropertyName("product_quantity")]
        public decimal ProductQuantity { get; set; }

        [JsonPropertyName("product_total")]
        public decimal ProductTotal { get; set; }

        [JsonPropertyName("product_discount")]
        public decimal ProductDiscount { get; set; }

        [JsonPropertyName("product_net_total")]
        public decimal ProductNetTotal { get; set; }
    }

    public class OrderMaintenanceRequestDto
    {
        [JsonPropertyName("old_order_id")]
        public long OldOrderId { get; set; }

        [JsonPropertyName("auditor_id")]
        public long AuditorId { get; set; }

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;
    }

    public class OrderCancelDto
    {
        [JsonPropertyName("order_id")]
        public long OrderId { get; set; }

        [JsonPropertyName("auditor_id")]
        public long AuditorId { get; set; }

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class OrderQueryDto
    {
        //[JsonPropertyName("page")]
        public int Page { get; set; } = 1;
        //[JsonPropertyName("page_size")] 
        public int PageSize { get; set; } = 20;

        //[JsonPropertyName("date_from")]
        public DateTime? DateFrom { get; set; }
        //[JsonPropertyName("date_to")] 
        public DateTime? DateTo { get; set; }

        //[JsonPropertyName("search")] 
        public string? Search { get; set; }
        //[JsonPropertyName("items_category")] 
        public string? ItemsCategory { get; set; }
        //[JsonPropertyName("status")] 
        public OrderStatus? Status { get; set; }
        //[JsonPropertyName("type")] 
        public OrderType? Type { get; set; }
        public OrderCategory? CategoryType { get; set; }
        //[JsonPropertyName("source")] 
        public OrderSource? Source { get; set; }
        //[JsonPropertyName("seller_id")]
        public long? SellerId { get; set; }
        //[JsonPropertyName("tech_id")] 
        public long? TechId { get; set; }
        //[JsonPropertyName("country_state_id")] 
        public long? CountryStateId { get; set; }
    }
    public class PagedResult<T>
    {
        [JsonPropertyName("items")] public List<T> Items { get; set; } = new();
        [JsonPropertyName("total_count")] public int TotalCount { get; set; }
        [JsonPropertyName("page")] public int Page { get; set; }
        [JsonPropertyName("page_size")] public int PageSize { get; set; }
        [JsonPropertyName("total_pages")] public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public class OrderStatsDto
    {
        [JsonPropertyName("total_today")] public int TotalToday { get; set; }
        [JsonPropertyName("total_month")] public int TotalMonth { get; set; }
        [JsonPropertyName("completed_today")] public int CompletedToday { get; set; }
        [JsonPropertyName("completed_month")] public int CompletedMonth { get; set; }
        [JsonPropertyName("cancelled_today")] public int CancelledToday { get; set; }
        [JsonPropertyName("cancelled_month")] public int CancelledMonth { get; set; }
        [JsonPropertyName("maintenance_month")] public int MaintenanceMonth { get; set; }
        [JsonPropertyName("maintenance_completed_month")] public int MaintenanceCompletedMonth { get; set; }
    }
}