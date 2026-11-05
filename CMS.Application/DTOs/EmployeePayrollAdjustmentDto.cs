// Models/DTOs/EmployeePayrollAdjustmentDto.cs
using CMS.Application.DTOs;
using CMS.Domain.Enums;
using System.Text.Json.Serialization;

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

    public class PayrollAdjustmentFilterDto : PagedFilterDto
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public long? EmployeeId { get; set; }
        public int? Kind { get; set; }        // 1 addition, 2 deduction
        public int? Type { get; set; }
        public string? Search { get; set; }   // employee name or reason
    }

    public class PayrollAdjustmentListItemDto
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("employee_id")] public long EmployeeId { get; set; }
        [JsonPropertyName("employee_name")] public string EmployeeName { get; set; } = "";
        [JsonPropertyName("operation_date")] public DateTime OperationDate { get; set; }
        [JsonPropertyName("created_at_utc")] public DateTime CreatedAtUtc { get; set; }
        [JsonPropertyName("kind")] public int Kind { get; set; }
        [JsonPropertyName("type")] public int Type { get; set; }
        [JsonPropertyName("amount")] public decimal Amount { get; set; }
        [JsonPropertyName("reason")] public string Reason { get; set; } = "";
        [JsonPropertyName("approved_by_name")] public string ApprovedByName { get; set; } = "";
        [JsonPropertyName("from_loan")] public bool FromLoan { get; set; }   // created from a loan installment
    }

    public class PayrollAdjustmentSummaryDto
    {
        [JsonPropertyName("total_additions")] public decimal TotalAdditions { get; set; }
        [JsonPropertyName("total_deductions")] public decimal TotalDeductions { get; set; }
    }
}