using CMS.Domain.Enums;
using System.Text.Json.Serialization;

namespace CMS.Application.DTOs;

// ------------------------------------------------------------------ filters (bound from the query string)

public class ReportDateFilterDto
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class MonthlySalesFilterDto
{
    public int? Year { get; set; }          // default: this year
    public string? Half { get; set; }       // "first" (Jan-Jun, default) | "last" (Jul-Dec)
    public long? SellerId { get; set; }     // empty / 0 = all sellers
}

public enum ReportSource { Orders, Invoices }
public enum ReportRowBy { State, Seller, Tech }

public class CategoryMatrixFilterDto : ReportDateFilterDto
{
    // "all" (default) | "regular" (no maintenance) | an OrderType name (regular orders of that type) | "maintenance"
    public string? Kind { get; set; }
    public long? SellerId { get; set; }          // empty / 0 = all sellers
    public string? Measure { get; set; }         // "count" (default) | "amount"
    public bool IncludeCancelled { get; set; }   // orders only
}

// ------------------------------------------------------------------ rows x columns table (reports 4 - 10)

public class CrossTabDto
{
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("row_label")] public string RowLabel { get; set; } = string.Empty;
    [JsonPropertyName("measure")] public string Measure { get; set; } = "count";      // count | amount
    [JsonPropertyName("date_from")] public DateTime? DateFrom { get; set; }
    [JsonPropertyName("date_to")] public DateTime? DateTo { get; set; }
    [JsonPropertyName("columns")] public List<string> Columns { get; set; } = new();
    [JsonPropertyName("rows")] public List<CrossTabRowDto> Rows { get; set; } = new();
    [JsonPropertyName("column_totals")] public List<decimal> ColumnTotals { get; set; } = new();
    [JsonPropertyName("grand_total")] public decimal GrandTotal { get; set; }
}

public class CrossTabRowDto
{
    [JsonPropertyName("id")] public long Id { get; set; }              // 0 = the "not assigned" row
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("values")] public List<decimal> Values { get; set; } = new();
    [JsonPropertyName("total")] public decimal Total { get; set; }
}

// ------------------------------------------------------------------ 1: unfinished orders that have sale invoices

public class UnfinishedOrderRowDto
{
    [JsonPropertyName("order_id")] public long OrderId { get; set; }
    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
    [JsonPropertyName("installation_date")] public DateTime InstallationDate { get; set; }
    [JsonPropertyName("status")] public OrderStatus Status { get; set; }
    [JsonPropertyName("type")] public OrderType Type { get; set; }
    [JsonPropertyName("category")] public OrderCategory Category { get; set; }
    [JsonPropertyName("customer_name")] public string CustomerName { get; set; } = string.Empty;
    [JsonPropertyName("customer_phone")] public string CustomerPhone { get; set; } = string.Empty;
    [JsonPropertyName("state_name")] public string StateName { get; set; } = string.Empty;
    [JsonPropertyName("city_name")] public string CityName { get; set; } = string.Empty;
    [JsonPropertyName("seller_name")] public string SellerName { get; set; } = string.Empty;
    [JsonPropertyName("tech_name")] public string TechName { get; set; } = string.Empty;
    [JsonPropertyName("order_net_total")] public decimal OrderNetTotal { get; set; }
    [JsonPropertyName("invoices_count")] public int InvoicesCount { get; set; }
    [JsonPropertyName("invoice_numbers")] public List<long> InvoiceNumbers { get; set; } = new();
    [JsonPropertyName("invoices_net_total")] public decimal InvoicesNetTotal { get; set; }
}

// ------------------------------------------------------------------ 2: orders with an advance payment that is not on an invoice

public class AdvanceOrderRowDto
{
    [JsonPropertyName("order_id")] public long OrderId { get; set; }
    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
    [JsonPropertyName("status")] public OrderStatus Status { get; set; }
    [JsonPropertyName("type")] public OrderType Type { get; set; }
    [JsonPropertyName("customer_name")] public string CustomerName { get; set; } = string.Empty;
    [JsonPropertyName("customer_phone")] public string CustomerPhone { get; set; } = string.Empty;
    [JsonPropertyName("state_name")] public string StateName { get; set; } = string.Empty;
    [JsonPropertyName("seller_name")] public string SellerName { get; set; } = string.Empty;
    [JsonPropertyName("order_net_total")] public decimal OrderNetTotal { get; set; }
    [JsonPropertyName("advance_count")] public int AdvanceCount { get; set; }
    [JsonPropertyName("advance_total")] public decimal AdvanceTotal { get; set; }
    [JsonPropertyName("advance_total_currency")] public decimal AdvanceTotalCurrency { get; set; }
    [JsonPropertyName("first_advance_date")] public DateTime FirstAdvanceDate { get; set; }
    [JsonPropertyName("last_advance_date")] public DateTime LastAdvanceDate { get; set; }
    [JsonPropertyName("sale_invoices_count")] public int SaleInvoicesCount { get; set; }   // live sale invoices of the order
}

// ------------------------------------------------------------------ 3: unpaid sale invoices

public class UnpaidInvoiceRowDto
{
    [JsonPropertyName("invoice_id")] public long InvoiceId { get; set; }
    [JsonPropertyName("number")] public long Number { get; set; }
    [JsonPropertyName("date")] public DateTime Date { get; set; }
    [JsonPropertyName("age_days")] public int AgeDays { get; set; }
    [JsonPropertyName("customer_id")] public long CustomerId { get; set; }
    [JsonPropertyName("customer_name")] public string CustomerName { get; set; } = string.Empty;
    [JsonPropertyName("customer_phone")] public string CustomerPhone { get; set; } = string.Empty;
    [JsonPropertyName("order_id")] public long? OrderId { get; set; }
    [JsonPropertyName("seller_name")] public string SellerName { get; set; } = string.Empty;
    [JsonPropertyName("net_total")] public decimal NetTotal { get; set; }
    [JsonPropertyName("net_total_currency")] public decimal NetTotalCurrency { get; set; }
    [JsonPropertyName("returned_total")] public decimal ReturnedTotal { get; set; }
    [JsonPropertyName("paid_total")] public decimal PaidTotal { get; set; }
    [JsonPropertyName("remaining")] public decimal Remaining { get; set; }
    [JsonPropertyName("remaining_currency")] public decimal RemainingCurrency { get; set; }
}

// ------------------------------------------------------------------ 11: maintenance within 30 days of the original order

public class MaintenanceReportDto
{
    [JsonPropertyName("date_from")] public DateTime? DateFrom { get; set; }
    [JsonPropertyName("date_to")] public DateTime? DateTo { get; set; }
    [JsonPropertyName("window_days")] public int WindowDays { get; set; }
    [JsonPropertyName("rows")] public List<MaintenanceTechRowDto> Rows { get; set; } = new();
    [JsonPropertyName("total")] public MaintenanceTechRowDto Total { get; set; } = new();
}

public class MaintenanceTechRowDto
{
    [JsonPropertyName("tech_id")] public long TechId { get; set; }                   // 0 = no tech
    [JsonPropertyName("tech_name")] public string TechName { get; set; } = string.Empty;
    [JsonPropertyName("maintenance_count")] public int MaintenanceCount { get; set; }
    [JsonPropertyName("maintenance_order_ids")] public List<long> MaintenanceOrderIds { get; set; } = new();
    [JsonPropertyName("original_order_ids")] public List<long> OriginalOrderIds { get; set; } = new();
    [JsonPropertyName("done_count")] public int DoneCount { get; set; }
    [JsonPropertyName("done_order_ids")] public List<long> DoneOrderIds { get; set; } = new();
    [JsonPropertyName("percent")] public decimal Percent { get; set; }                // maintenance_count / done_count * 100
}
