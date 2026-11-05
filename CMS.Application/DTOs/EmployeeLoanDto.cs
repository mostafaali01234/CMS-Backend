// Models/DTOs/EmployeeLoanDto.cs
using CMS.Application.DTOs;
using CMS.Domain.Enums;
using System.Text.Json.Serialization;

namespace CMS.Domain.DTOs
{
    public class EmployeeLoanDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("employee_id")]
        public long EmployeeId { get; set; }

        [JsonPropertyName("employee_name")]
        public string EmployeeName { get; set; } = string.Empty;

        [JsonPropertyName("approved_by_id")]
        public long? ApprovedById { get; set; }

        [JsonPropertyName("approved_by_name")]
        public string ApprovedByName { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("money_safe_id")]
        public long? MoneySafeId { get; set; }

        [JsonPropertyName("money_safe_name")]
        public string MoneySafeName { get; set; } = string.Empty;

        [JsonPropertyName("installment_count")]
        public int InstallmentCount { get; set; }

        [JsonPropertyName("start_date")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("project_id")]
        public long? ProjectId { get; set; }

        [JsonPropertyName("project_name")]
        public string ProjectName { get; set; } = string.Empty;

        [JsonPropertyName("shift_id")]
        public long? ShiftId { get; set; }

        [JsonPropertyName("installments")]
        public List<LoanInstallmentSummaryDto> Installments { get; set; } = new();

        [JsonPropertyName("created_at_utc")]
        public DateTime? CreatedAtUtc { get; set; }

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updated_at_utc")]
        public DateTime? UpdatedAtUtc { get; set; }

        [JsonPropertyName("updated_by")]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class LoanInstallmentSummaryDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("installment_number")]
        public int InstallmentNumber { get; set; }

        [JsonPropertyName("due_date")]
        public DateTime DueDate { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("status")]
        public InstallmentStatus Status { get; set; }
    }

    public class EmployeeLoanFilterDto : PagedFilterDto
    {
        public DateTime? DateFrom { get; set; }      // on the loan's start date
        public DateTime? DateTo { get; set; }
        public long? EmployeeId { get; set; }
        public string? Mode { get; set; }            // "safe" | "installments" | null = all
        public string? Search { get; set; }          // employee name
    }

    public class EmployeeLoanListItemDto
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("employee_id")] public long EmployeeId { get; set; }
        [JsonPropertyName("employee_name")] public string EmployeeName { get; set; } = "";
        [JsonPropertyName("approved_by_name")] public string ApprovedByName { get; set; } = "";
        [JsonPropertyName("amount")] public decimal Amount { get; set; }
        [JsonPropertyName("money_safe_name")] public string MoneySafeName { get; set; } = "";
        [JsonPropertyName("installment_count")] public int InstallmentCount { get; set; }
        [JsonPropertyName("start_date")] public DateTime StartDate { get; set; }
        [JsonPropertyName("created_at_utc")] public DateTime CreatedAtUtc { get; set; }
        [JsonPropertyName("project_name")] public string ProjectName { get; set; } = "";
        [JsonPropertyName("deducted_count")] public int DeductedCount { get; set; }
        [JsonPropertyName("skipped_count")] public int SkippedCount { get; set; }
        [JsonPropertyName("pending_amount")] public decimal PendingAmount { get; set; }
    }
}