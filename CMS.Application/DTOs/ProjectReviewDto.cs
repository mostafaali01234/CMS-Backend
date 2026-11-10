using CMS.Domain.Enums;
using System.Text.Json.Serialization;

namespace CMS.Application.DTOs
{
    public class ProjectReviewFilterDto
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }

    // the numbers for one project (used by the list and by the single-project review)
    public class ProjectReviewSummaryDto
    {
        [JsonPropertyName("project_id")] public long ProjectId { get; set; }
        [JsonPropertyName("project_name")] public string ProjectName { get; set; } = string.Empty;

        [JsonPropertyName("sales_total")] public decimal SalesTotal { get; set; }
        [JsonPropertyName("sales_returns_total")] public decimal SalesReturnsTotal { get; set; }
        [JsonPropertyName("net_sales")] public decimal NetSales { get; set; }

        [JsonPropertyName("purchases_total")] public decimal PurchasesTotal { get; set; }
        [JsonPropertyName("purchases_returns_total")] public decimal PurchasesReturnsTotal { get; set; }
        [JsonPropertyName("net_purchases")] public decimal NetPurchases { get; set; }

        [JsonPropertyName("expenses_total")] public decimal ExpensesTotal { get; set; }
        [JsonPropertyName("expenses_total_currency")] public decimal ExpensesTotalCurrency { get; set; }

        [JsonPropertyName("total_cost")] public decimal TotalCost { get; set; }        // net purchases + expenses
        [JsonPropertyName("profit")] public decimal Profit { get; set; }                // net sales - total cost
        [JsonPropertyName("margin_percent")] public decimal MarginPercent { get; set; } // profit / net sales

        // not part of the profit: money moved for the project
        [JsonPropertyName("loans_total")] public decimal LoansTotal { get; set; }
        [JsonPropertyName("transfers_total")] public decimal TransfersTotal { get; set; }

        [JsonPropertyName("shifts_count")] public int ShiftsCount { get; set; }
        [JsonPropertyName("km_total")] public decimal KmTotal { get; set; }
    }

    public class ProjectExpenseTypeTotalDto
    {
        [JsonPropertyName("expense_type")] public string ExpenseType { get; set; } = string.Empty;
        [JsonPropertyName("count")] public int Count { get; set; }
        [JsonPropertyName("amount")] public decimal Amount { get; set; }
        [JsonPropertyName("amount_currency")] public decimal AmountCurrency { get; set; }
    }

    // a sale or buy invoice line; returns carry negative quantity / total so a column sum is the net
    public class ProjectInvoiceLineDto
    {
        [JsonPropertyName("invoice_id")] public long InvoiceId { get; set; }
        [JsonPropertyName("invoice_number")] public long InvoiceNumber { get; set; }
        [JsonPropertyName("date")] public DateTime Date { get; set; }
        [JsonPropertyName("type")] public InvoiceType Type { get; set; }
        [JsonPropertyName("party")] public string Party { get; set; } = string.Empty;   // customer / supplier
        [JsonPropertyName("product")] public string Product { get; set; } = string.Empty;
        [JsonPropertyName("notes")] public string Notes { get; set; } = string.Empty;
        [JsonPropertyName("quantity")] public decimal Quantity { get; set; }
        [JsonPropertyName("price")] public decimal Price { get; set; }
        [JsonPropertyName("discount")] public decimal Discount { get; set; }
        [JsonPropertyName("net_total")] public decimal NetTotal { get; set; }
    }

    public class ProjectExpenseLineDto
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("date")] public DateTime Date { get; set; }
        [JsonPropertyName("expense_type")] public string ExpenseType { get; set; } = string.Empty;
        [JsonPropertyName("notes")] public string Notes { get; set; } = string.Empty;
        [JsonPropertyName("money_safe")] public string MoneySafe { get; set; } = string.Empty;
        [JsonPropertyName("amount")] public decimal Amount { get; set; }
        [JsonPropertyName("amount_currency")] public decimal AmountCurrency { get; set; }
    }

    public class ProjectLoanLineDto
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("date")] public DateTime Date { get; set; }          // created date (loans have no date of their own)
        [JsonPropertyName("employee")] public string Employee { get; set; } = string.Empty;
        [JsonPropertyName("money_safe")] public string MoneySafe { get; set; } = string.Empty;   // empty => paid back by installments
        [JsonPropertyName("installment_count")] public int InstallmentCount { get; set; }
        [JsonPropertyName("notes")] public string Notes { get; set; } = string.Empty;
        [JsonPropertyName("amount")] public decimal Amount { get; set; }
    }

    public class ProjectTransactionLineDto
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("date")] public DateTime Date { get; set; }
        [JsonPropertyName("transaction_type")] public MoneySafeTransactionType TransactionType { get; set; }
        [JsonPropertyName("out_money_safe")] public string OutMoneySafe { get; set; } = string.Empty;
        [JsonPropertyName("in_money_safe")] public string InMoneySafe { get; set; } = string.Empty;
        [JsonPropertyName("notes")] public string Notes { get; set; } = string.Empty;
        [JsonPropertyName("amount")] public decimal Amount { get; set; }
        [JsonPropertyName("amount_currency")] public decimal AmountCurrency { get; set; }
    }

    public class ProjectShiftLineDto
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("date")] public DateTime Date { get; set; }
        [JsonPropertyName("state")] public string State { get; set; } = string.Empty;
        [JsonPropertyName("car")] public string Car { get; set; } = string.Empty;
        [JsonPropertyName("store")] public string Store { get; set; } = string.Empty;
        [JsonPropertyName("km_start")] public decimal KmStart { get; set; }
        [JsonPropertyName("km_end")] public decimal KmEnd { get; set; }
        [JsonPropertyName("km")] public decimal Km { get; set; }
        [JsonPropertyName("notes")] public string Notes { get; set; } = string.Empty;
    }

    public class ProjectReviewDto
    {
        [JsonPropertyName("project_id")] public long ProjectId { get; set; }
        [JsonPropertyName("project_name")] public string ProjectName { get; set; } = string.Empty;
        [JsonPropertyName("project_notes")] public string ProjectNotes { get; set; } = string.Empty;
        [JsonPropertyName("date_from")] public DateTime? DateFrom { get; set; }
        [JsonPropertyName("date_to")] public DateTime? DateTo { get; set; }

        [JsonPropertyName("summary")] public ProjectReviewSummaryDto Summary { get; set; } = new();
        [JsonPropertyName("expenses_by_type")] public List<ProjectExpenseTypeTotalDto> ExpensesByType { get; set; } = new();

        [JsonPropertyName("sales")] public List<ProjectInvoiceLineDto> Sales { get; set; } = new();
        [JsonPropertyName("purchases")] public List<ProjectInvoiceLineDto> Purchases { get; set; } = new();
        [JsonPropertyName("expenses")] public List<ProjectExpenseLineDto> Expenses { get; set; } = new();
        [JsonPropertyName("loans")] public List<ProjectLoanLineDto> Loans { get; set; } = new();
        [JsonPropertyName("transactions")] public List<ProjectTransactionLineDto> Transactions { get; set; } = new();
        [JsonPropertyName("shifts")] public List<ProjectShiftLineDto> Shifts { get; set; } = new();
    }
}
