// Models/DTOs/EmployeeCommissionDto.cs
using System.Text.Json.Serialization;
using CMS.Domain.Enums;

namespace CMS.Domain.DTOs
{
    public class CommissionPageDto
    {
        [JsonPropertyName("invoices_total")]
        public decimal InvoicesTotal { get; set; }

        [JsonPropertyName("commission_total")]
        public decimal CommissionTotal { get; set; }

        [JsonPropertyName("special_commission_total")]
        public decimal SpecialCommissionTotal { get; set; }

        [JsonPropertyName("sales_total")]
        public decimal SalesTotal { get; set; }

        [JsonPropertyName("tech_total")]
        public decimal TechTotal { get; set; }

        [JsonPropertyName("employee_base_salary")]
        public decimal EmployeeBaseSalary { get; set; }

        [JsonPropertyName("sales_commissions")]
        public List<EmployeeCommissionDto> SalesCommissions { get; set; } = new List<EmployeeCommissionDto>();

        [JsonPropertyName("tech_commissions")]
        public List<EmployeeCommissionDto> TechCommissions { get; set; } = new List<EmployeeCommissionDto>();
    }
    public class EmployeeCommissionDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("order_id")]
        public long OrderId { get; set; }

        [JsonPropertyName("invoice_id")]
        public long InvoiceId { get; set; }

        [JsonPropertyName("invoice_number")]
        public long InvoiceNumber { get; set; }

        [JsonPropertyName("employee_id")]
        public long EmployeeId { get; set; }

        [JsonPropertyName("employee_name")]
        public string EmployeeName { get; set; } = string.Empty;

        [JsonPropertyName("employee_role")]
        public CommissionRole EmployeeRole { get; set; }

        [JsonPropertyName("product_id")]
        public long ProductId { get; set; }

        [JsonPropertyName("product_name")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("product_total")]
        public decimal ProductTotal { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("extra_amount")]
        public decimal ExtraAmount { get; set; } = 0;

        [JsonPropertyName("status")]
        public CommissionStatus Status { get; set; }

        [JsonPropertyName("paid_at")]
        public DateTime? PaidAt { get; set; }

        [JsonPropertyName("withdrawn_at")]
        public DateTime? WithdrawnAt { get; set; }

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