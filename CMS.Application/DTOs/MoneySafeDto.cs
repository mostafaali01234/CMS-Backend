// Models/DTOs/MoneySafeDto.cs
using System.Text.Json.Serialization;

namespace CMS.Application.DTOs
{
    public class MoneySafeDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("cod")]
        public long Cod { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("opening_balance")]
        public decimal OpeningBalance { get; set; }

        [JsonPropertyName("opening_balance_currency")]
        public decimal OpeningBalanceCurrency { get; set; }

        [JsonPropertyName("manager_id")]
        public long? ManagerId { get; set; }

        [JsonPropertyName("manager_name")]
        public string ManagerName { get; set; } = string.Empty;

        [JsonPropertyName("category_id")]
        public long? CategoryId { get; set; }

        [JsonPropertyName("category_name")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("type_id")]
        public long? TypeId { get; set; }

        [JsonPropertyName("type_name")]
        public string TypeName { get; set; } = string.Empty;

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