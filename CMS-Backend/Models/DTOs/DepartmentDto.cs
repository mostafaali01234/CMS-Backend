using System.Text.Json.Serialization;

namespace CMS_Backend.Models.DTOs
{
    public class DepartmentDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("manager_id")]
        public long ManagerId { get; set; }
        [JsonPropertyName("manager_name")]
        public string ManagerName { get; set; } = string.Empty;
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
