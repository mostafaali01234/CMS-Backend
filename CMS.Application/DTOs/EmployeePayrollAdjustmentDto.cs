// Models/DTOs/EmployeePayrollAdjustmentDto.cs
using System.Text.Json.Serialization;
using CMS.Domain.Enums;

namespace CMS.Domain.DTOs
{
    public class EmployeePayrollAdjustmentDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("employee_id")]
        public long EmployeeId { get; set; }

        [JsonPropertyName("employee_name")]
        public string EmployeeName { get; set; } = string.Empty;

        [JsonPropertyName("operation_date")]
        public DateTime OperationDate { get; set; }

        [JsonPropertyName("kind")]
        public PayrollAdjustmentKind Kind { get; set; }

        [JsonPropertyName("type")]
        public PayrollAdjustmentType Type { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("approved_by_id")]
        public long? ApprovedById { get; set; }

        [JsonPropertyName("approved_by_name")]
        public string ApprovedByName { get; set; } = string.Empty;

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