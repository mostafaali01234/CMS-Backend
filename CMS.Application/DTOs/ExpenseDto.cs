// Models/DTOs/ExpenseDto.cs
using System.Text.Json.Serialization;

namespace CMS.Domain.DTOs
{
    public class ExpenseDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("expense_type_id")]
        public long ExpenseTypeId { get; set; }

        [JsonPropertyName("expense_type_name")]
        public string ExpenseTypeName { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("amount_currency")]
        public decimal AmountCurrency { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public ExpenseDataDto? Data { get; set; }

        [JsonPropertyName("money_safe_id")]
        public long MoneySafeId { get; set; }

        [JsonPropertyName("money_safe_name")]
        public string MoneySafeName { get; set; } = string.Empty;

        [JsonPropertyName("auditor_id")]
        public long AuditorId { get; set; }

        [JsonPropertyName("auditor_name")]
        public string AuditorName { get; set; } = string.Empty;

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

    public class ExpenseDataDto
    {
        [JsonPropertyName("car_id")]
        public long? CarId { get; set; }
        [JsonPropertyName("car_name")]
        public string CarName { get; set; } = string.Empty;

        [JsonPropertyName("oil_start_km")]
        public decimal? OilStartKm { get; set; }

        [JsonPropertyName("oil_end_km")]
        public decimal? OilEndKm { get; set; }

        [JsonPropertyName("gas_current_km")]
        public decimal? GasCurrentKm { get; set; }

        [JsonPropertyName("gas_litres_count")]
        public decimal? GasLitresCount { get; set; }
    }
}