// Models/DTOs/EmployeeSpecialCommissionDefinitionDto.cs
using System.Text.Json.Serialization;

namespace CMS.Domain.DTOs
{
    public class EmployeeSpecialCommissionDefinitionDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("manager_id")]
        public long ManagerId { get; set; }

        [JsonPropertyName("manager_name")]
        public string ManagerName { get; set; } = string.Empty;

        [JsonPropertyName("commission_rate")]
        public decimal CommissionRate { get; set; }

        [JsonPropertyName("employee_ids")]
        public List<long> EmployeeIds { get; set; } = new();

        [JsonPropertyName("employees")]
        public List<SpecialCommissionEmployeeDto> Employees { get; set; } = new();

        [JsonPropertyName("product_ids")]
        public List<long> ProductIds { get; set; } = new();

        [JsonPropertyName("products")]
        public List<SpecialCommissionProductDto> Products { get; set; } = new();

        [JsonPropertyName("created_at_utc")]
        public DateTime? CreatedAtUtc { get; set; }

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updated_at_utc")]
        public DateTime? UpdatedAtUtc { get; set; }

        [JsonPropertyName("updated_by")]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class SpecialCommissionEmployeeDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class SpecialCommissionProductDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}