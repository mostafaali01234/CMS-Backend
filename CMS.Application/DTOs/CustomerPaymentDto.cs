// Models/DTOs/CustomerPaymentDto.cs
using CMS.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS.Domain.DTOs
{
    public class CustomerPaymentDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("type")]
        public PaymentType Type { get; set; }

        [JsonPropertyName("customer_id")]
        public long CustomerId { get; set; }

        [JsonPropertyName("customer_name")]
        public string CustomerName { get; set; } = string.Empty;

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

        [JsonPropertyName("advance_payment_order_id")]
        public long? AdvancePaymentOrderId { get; set; }

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