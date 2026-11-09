// CMS.Application/DTOs/PayrollDtos.cs
using System.Text.Json.Serialization;
using CMS.Domain.DTOs;
using CMS.Domain.Enums;
namespace CMS.Application.DTOs;

public class PayrollFilterDto : PagedFilterDto
{
    public long? EmployeeId { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
    public PayrollStatus? Status { get; set; }
}

public class PayrollReviewRequestDto
{
    [JsonPropertyName("employee_id")] public long EmployeeId { get; set; }
    [JsonPropertyName("month")] public int Month { get; set; }
    [JsonPropertyName("year")] public int Year { get; set; }
    [JsonPropertyName("revised_by_id")] public long RevisedById { get; set; }
}

public class PayrollWithdrawRequestDto
{
    [JsonPropertyName("money_safe_id")] public long MoneySafeId { get; set; }
    [JsonPropertyName("withdrawn_by_id")] public long WithdrawnById { get; set; }
    [JsonPropertyName("date")] public DateTime? Date { get; set; }
}

public class PayrollCommissionRowDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("role")] public string Role { get; set; } = "";               // Sales | Tech | Manager
    [JsonPropertyName("order_id")] public long OrderId { get; set; }
    [JsonPropertyName("invoice_id")] public long InvoiceId { get; set; }
    [JsonPropertyName("invoice_number")] public long InvoiceNumber { get; set; }
    [JsonPropertyName("order_type")] public string OrderType { get; set; } = "";
    [JsonPropertyName("customer_name")] public string CustomerName { get; set; } = "";
    [JsonPropertyName("seller_name")] public string SellerName { get; set; } = "";
    [JsonPropertyName("product_name")] public string ProductName { get; set; } = "";
    [JsonPropertyName("product_total")] public decimal ProductTotal { get; set; }
    [JsonPropertyName("amount")] public decimal Amount { get; set; }
    [JsonPropertyName("extra_amount")] public decimal ExtraAmount { get; set; }
    [JsonPropertyName("total")] public decimal Total { get; set; }                  // amount + extra
    [JsonPropertyName("paid_at")] public DateTime? PaidAt { get; set; }
}

public class PayrollLoanRowDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("loan_id")] public long LoanId { get; set; }
    [JsonPropertyName("installment_number")] public int InstallmentNumber { get; set; }
    [JsonPropertyName("due_date")] public DateTime DueDate { get; set; }
    [JsonPropertyName("amount")] public decimal Amount { get; set; }
    [JsonPropertyName("notes")] public string Notes { get; set; } = "";
}

public class EmployeeAdjustmentDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("employee_id")] public long EmployeeId { get; set; }
    [JsonPropertyName("operation_date")] public DateTime OperationDate { get; set; }
    [JsonPropertyName("kind")] public PayrollAdjustmentKind Kind { get; set; }
    [JsonPropertyName("type")] public PayrollAdjustmentType Type { get; set; }
    [JsonPropertyName("amount")] public decimal Amount { get; set; }
    [JsonPropertyName("reason")] public string Reason { get; set; } = "";
    [JsonPropertyName("approved_by_id")] public long? ApprovedById { get; set; }
    [JsonPropertyName("approved_by_name")] public string ApprovedByName { get; set; } = "";
}

public class PayrollPreviewDto
{
    [JsonPropertyName("employee_id")] public long EmployeeId { get; set; }
    [JsonPropertyName("employee_name")] public string EmployeeName { get; set; } = "";
    [JsonPropertyName("month")] public int Month { get; set; }
    [JsonPropertyName("year")] public int Year { get; set; }

    [JsonPropertyName("base_salary")] public decimal BaseSalary { get; set; }
    [JsonPropertyName("invoices_total")] public decimal InvoicesTotal { get; set; }
    [JsonPropertyName("sales_commission_total")] public decimal SalesCommissionTotal { get; set; }
    [JsonPropertyName("tech_commission_total")] public decimal TechCommissionTotal { get; set; }
    [JsonPropertyName("manager_commission_total")] public decimal ManagerCommissionTotal { get; set; }
    [JsonPropertyName("bonus_total")] public decimal BonusTotal { get; set; }
    [JsonPropertyName("deductions_total")] public decimal DeductionsTotal { get; set; }
    [JsonPropertyName("loans_total")] public decimal LoansTotal { get; set; }
    [JsonPropertyName("lunch_amount")] public decimal LunchAmount { get; set; }
    [JsonPropertyName("lunch_shifts")] public int LunchShifts { get; set; }
    [JsonPropertyName("lunch_total")] public decimal LunchTotal { get; set; }
    [JsonPropertyName("net_total")] public decimal NetTotal { get; set; }
    [JsonPropertyName("pending_commission_total")] public decimal PendingCommissionTotal { get; set; }   // باقي العمولات - فواتير غير مدفوعة

    [JsonPropertyName("existing_payroll")] public EmployeePayrollDto? ExistingPayroll { get; set; }       // already approved for this month
    [JsonPropertyName("commissions")] public List<PayrollCommissionRowDto> Commissions { get; set; } = new();
    [JsonPropertyName("adjustments")] public List<EmployeeAdjustmentDto> Adjustments { get; set; } = new();
    [JsonPropertyName("loans")] public List<PayrollLoanRowDto> Loans { get; set; } = new();
}