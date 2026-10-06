// Models/DTOs/EmployeePayrollDto.cs
using System.Text.Json.Serialization;
using CMS.Domain.Enums;

namespace CMS.Domain.DTOs
{
    public class EmployeePayrollDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("employee_id")]
        public long EmployeeId { get; set; }

        [JsonPropertyName("employee_name")]
        public string EmployeeName { get; set; } = string.Empty;

        [JsonPropertyName("month")]
        public int Month { get; set; }

        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("base_salary")]
        public decimal BaseSalary { get; set; }

        [JsonPropertyName("sales_commission_total")]
        public decimal SalesCommissionTotal { get; set; }

        [JsonPropertyName("tech_commission_total")]
        public decimal TechCommissionTotal { get; set; }

        [JsonPropertyName("loans_total")]
        public decimal LoansTotal { get; set; }

        [JsonPropertyName("bonus_total")]
        public decimal BonusTotal { get; set; }

        [JsonPropertyName("deductions_total")]
        public decimal DeductionsTotal { get; set; }

        [JsonPropertyName("lunch_total")]
        public decimal LunchTotal { get; set; }

        [JsonPropertyName("net_total")]
        public decimal NetTotal { get; set; }

        [JsonPropertyName("status")]
        public PayrollStatus Status { get; set; }

        [JsonPropertyName("revised_by_id")]
        public long? RevisedById { get; set; }

        [JsonPropertyName("revised_by_name")]
        public string RevisedByName { get; set; } = string.Empty;

        [JsonPropertyName("money_safe_id")]
        public long? MoneySafeId { get; set; }

        [JsonPropertyName("money_safe_name")]
        public string MoneySafeName { get; set; } = string.Empty;

        [JsonPropertyName("withdrawn_at")]
        public DateTime? WithdrawnAt { get; set; }

        [JsonPropertyName("withdrawn_by_id")]
        public long? WithdrawnById { get; set; }

        [JsonPropertyName("withdrawn_by_name")]
        public string WithdrawnByName { get; set; } = string.Empty;

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