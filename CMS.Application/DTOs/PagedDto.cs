// CMS.Domain/DTOs/PagedDto.cs
using CMS.Domain.Enums;
using System.Text.Json.Serialization;
namespace CMS.Application.DTOs;

// bound from the query string (?page=2&pageSize=20), so the property names are the keys
public class PagedFilterDto
{
    public const int MaxPageSize = 200;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public void Normalize()
    {
        if (Page < 1) Page = 1;
        if (PageSize < 1) PageSize = 20;
        if (PageSize > MaxPageSize) PageSize = MaxPageSize;
    }
}

public class PagedResult<T>
{
    [JsonPropertyName("items")] public List<T> Items { get; set; } = new();
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("page")] public int Page { get; set; }
    [JsonPropertyName("page_size")] public int PageSize { get; set; }
}

// a page plus totals for the whole filter (not just the page)
public class PagedResult<T, TSummary> : PagedResult<T>
{
    [JsonPropertyName("summary")] public TSummary? Summary { get; set; }
}

public class InvoiceListSummaryDto
{
    [JsonPropertyName("net")] public decimal Net { get; set; }                 // returns count as negative
    [JsonPropertyName("net_currency")] public decimal NetCurrency { get; set; }
    [JsonPropertyName("paid")] public decimal Paid { get; set; }
    [JsonPropertyName("remaining")] public decimal Remaining { get; set; }
    [JsonPropertyName("remaining_currency")] public decimal RemainingCurrency { get; set; }
}

public class BuyInvoiceFilterDto : PagedFilterDto
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public long? SupplierId { get; set; }
    public InvoiceType? Type { get; set; }
    public string? PayStatus { get; set; }       // "unpaid" | "paid"
    public string? Search { get; set; }
    public long? Number { get; set; }            // exact invoice number, ignores the dates
}

public class SaleInvoiceFilterDto : PagedFilterDto
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public long? CustomerId { get; set; }
    public InvoiceType? Type { get; set; }
    public string? PayStatus { get; set; }
    public string? Search { get; set; }
    public long? Number { get; set; }
    public long? OrderId { get; set; }           // exact order id, ignores the dates
}