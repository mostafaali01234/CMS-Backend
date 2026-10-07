// Models/DTOs/SaleInvoiceDto.cs
using System.Text.Json.Serialization;
using CMS.Domain.Enums;

namespace CMS.Domain.DTOs
{
    public class SaleInvoiceDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("number")]
        public long Number { get; set; }

        [JsonPropertyName("type")]
        public InvoiceType Type { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("customer_id")]
        public long CustomerId { get; set; }

        [JsonPropertyName("customer_name")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("auditor_id")]
        public long AuditorId { get; set; }

        [JsonPropertyName("auditor_name")]
        public string AuditorName { get; set; } = string.Empty;

        [JsonPropertyName("order_id")]
        public long? OrderId { get; set; }

        [JsonPropertyName("original_invoice_id")]
        public long? OriginalInvoiceId { get; set; }

        [JsonPropertyName("original_invoice_number")]
        public long? OriginalInvoiceNumber { get; set; }

        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("discount")]
        public decimal Discount { get; set; }

        [JsonPropertyName("net_total")]
        public decimal NetTotal { get; set; }

        [JsonPropertyName("order_net_total")]

        public decimal OrderNetTotal { get; set; } = 0;

        [JsonPropertyName("net_total_currency")]
        public decimal NetTotalCurrency { get; set; }

        [JsonPropertyName("paid_amount")]
        public decimal PaidAmount { get; set; }

        [JsonPropertyName("remaining_amount")]
        public decimal RemainingAmount { get; set; }

        [JsonPropertyName("shift_id")]
        public long? ShiftId { get; set; }

        [JsonPropertyName("items")]
        public List<SaleInvoiceItemDto> Items { get; set; } = new();

        [JsonPropertyName("order_items")]
        public List<SaleInvoiceItemDto> OrderItems { get; set; } = new();

        [JsonPropertyName("payment")]
        public List<SaleInvoicePaymentDto> Payments { get; set; } = new();

        [JsonPropertyName("created_at_utc")]
        public DateTime? CreatedAtUtc { get; set; }

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updated_at_utc")]
        public DateTime? UpdatedAtUtc { get; set; }

        [JsonPropertyName("updated_by")]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class SaleInvoiceItemDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("product_id")]
        public long ProductId { get; set; }

        [JsonPropertyName("product_name")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("store_id")]
        public long StoreId { get; set; }

        [JsonPropertyName("store_name")]
        public string StoreName { get; set; } = string.Empty;

        [JsonPropertyName("project_id")]
        public long? ProjectId { get; set; }

        [JsonPropertyName("project_name")]
        public string ProjectName { get; set; } = string.Empty;

        [JsonPropertyName("product_notes")]
        public string ProductNotes { get; set; } = string.Empty;

        [JsonPropertyName("price_type")]
        public PriceType PriceType { get; set; }

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

    public class SaleInvoicePaymentDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("payment_date")]
        public DateTime PaymentDate { get; set; }

        [JsonPropertyName("money_safe_id")]
        public long MoneySafeId { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("amount_currency")]
        public decimal AmountCurrency { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("auditor_id")]
        public long AuditorId { get; set; }

        [JsonPropertyName("auditor_name")]
        public string AuditorName { get; set; } = string.Empty;

        [JsonPropertyName("created_at_utc")]
        public DateTime? CreatedAtUtc { get; set; }

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updated_at_utc")]
        public DateTime? UpdatedAtUtc { get; set; }

        [JsonPropertyName("updated_by")]
        public string UpdatedBy { get; set; } = string.Empty;
    }
}