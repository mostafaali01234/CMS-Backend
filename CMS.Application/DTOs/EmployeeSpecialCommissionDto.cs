// Models/DTOs/EmployeeSpecialCommissionDto.cs
using System.Text.Json.Serialization;

namespace CMS.Domain.DTOs
{
    public class EmployeeSpecialCommissionDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("manager_id")]
        public long ManagerId { get; set; }

        [JsonPropertyName("manager_name")]
        public string ManagerName { get; set; } = string.Empty;

        [JsonPropertyName("employee_id")]
        public long EmployeeId { get; set; }

        [JsonPropertyName("employee_name")]
        public string EmployeeName { get; set; } = string.Empty;

        [JsonPropertyName("month")]
        public int Month { get; set; }

        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("sales_total")]
        public decimal SalesTotal { get; set; }

        [JsonPropertyName("commission_rate")]
        public decimal CommissionRate { get; set; }

        [JsonPropertyName("commission_total")]
        public decimal CommissionTotal { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

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