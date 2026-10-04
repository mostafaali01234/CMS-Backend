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

        [JsonPropertyName("city_id")]
        public long CityId { get; set; }

        [JsonPropertyName("city_name")]
        public string CityName { get; set; } = string.Empty;

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
}