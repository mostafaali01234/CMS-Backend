// Models/DTOs/MoneySafeTransactionDto.cs
using CMS.Domain.Enums;
using System.Text.Json.Serialization;

namespace CMS.Api.Models.DTOs
{
    public class MoneySafeTransactionDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("transaction_date")]
        public DateTime TransactionDate { get; set; }

        [JsonPropertyName("transaction_notes")]
        public string TransactionNotes { get; set; } = string.Empty;

        [JsonPropertyName("transaction_amount")]
        public decimal TransactionAmount { get; set; }

        [JsonPropertyName("transaction_amount_currency")]
        public decimal TransactionAmountCurrency { get; set; }

        [JsonPropertyName("transaction_type")]
        public MoneySafeTransactionType TransactionType { get; set; }

        [JsonPropertyName("auditor_id")]
        public long AuditorId { get; set; }

        [JsonPropertyName("auditor_name")]
        public string AuditorName { get; set; } = string.Empty;

        [JsonPropertyName("out_money_safe_id")]
        public long OutMoneySafeId { get; set; }

        [JsonPropertyName("out_money_safe_name")]
        public string OutMoneySafeName { get; set; } = string.Empty;

        [JsonPropertyName("in_money_safe_id")]
        public long InMoneySafeId { get; set; }

        [JsonPropertyName("in_money_safe_name")]
        public string InMoneySafeName { get; set; } = string.Empty;

        [JsonPropertyName("project_id")]
        public long? ProjectId { get; set; }

        [JsonPropertyName("project_name")]
        public string ProjectName { get; set; } = string.Empty;

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