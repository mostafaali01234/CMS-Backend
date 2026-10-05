// Models/DTOs/SupplierPaymentDto.cs
using CMS.Domain.Enums;
using System.Text.Json.Serialization;

namespace CMS.Domain.DTOs
{
    public class SupplierPaymentDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("type")]
        public PaymentType Type { get; set; }

        [JsonPropertyName("supplier_id")]
        public long SupplierId { get; set; }

        [JsonPropertyName("supplier_name")]
        public string SupplierName { get; set; } = string.Empty;

        [JsonPropertyName("invoice_id")]
        public long? InvoiceId { get; set; }

        [JsonPropertyName("money_safe_id")]
        public long MoneySafeId { get; set; }

        [JsonPropertyName("money_safe_name")]
        public string MoneySafeName { get; set; } = string.Empty;

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