// Models/DTOs/OrderTechHistoryDto.cs
using System.Text.Json.Serialization;
using CMS.Domain.Enums;

namespace CMS.Application.DTOs
{
    public class OrderTechHistoryDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("order_id")]
        public long OrderId { get; set; }

        [JsonPropertyName("tech_id")]
        public long TechId { get; set; }

        [JsonPropertyName("tech_name")]
        public string TechName { get; set; } = string.Empty;

        [JsonPropertyName("auditor_id")]
        public long AuditorId { get; set; }

        [JsonPropertyName("auditor_name")]
        public string AuditorName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public TechStatus Status { get; set; }

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