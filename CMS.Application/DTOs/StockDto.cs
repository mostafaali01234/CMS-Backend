// CMS.Application/DTOs/StockDto.cs
using System.Text.Json.Serialization;
namespace CMS.Application.DTOs;

public class StockFilterDto : PagedFilterDto
{
    public long? StoreId { get; set; }
    public long? CategoryId { get; set; }
    public string? Search { get; set; }
    public bool IncludeZero { get; set; }
}

public class MovesFilterDto : PagedFilterDto
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public long? StoreId { get; set; }
    public long? ProductId { get; set; }
    public long? OrderId { get; set; }
    public string? OperationType { get; set; }     // store_transaction | buy_invoice | sale_invoice
}

public class StockRowDto
{
    [JsonPropertyName("product_id")] public long ProductId { get; set; }
    [JsonPropertyName("product_name")] public string ProductName { get; set; } = "";
    [JsonPropertyName("category_name")] public string CategoryName { get; set; } = "";
    [JsonPropertyName("barcode")] public string Barcode { get; set; } = "";
    [JsonPropertyName("quantity")] public decimal Quantity { get; set; }
}
public class StockSummaryDto { [JsonPropertyName("total_quantity")] public decimal TotalQuantity { get; set; } }

public class StockMatrixStoreDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}
public class StockMatrixRowDto
{
    [JsonPropertyName("product_id")] public long ProductId { get; set; }
    [JsonPropertyName("product_name")] public string ProductName { get; set; } = "";
    [JsonPropertyName("category_name")] public string CategoryName { get; set; } = "";
    [JsonPropertyName("barcode")] public string Barcode { get; set; } = "";
    [JsonPropertyName("counts")] public List<decimal> Counts { get; set; } = new();  
    [JsonPropertyName("total")] public decimal Total { get; set; }
}
public class StockMatrixHeaderDto
{
    [JsonPropertyName("stores")] public List<StockMatrixStoreDto> Stores { get; set; } = new();
    [JsonPropertyName("totals")] public List<decimal> Totals { get; set; } = new();   // per store, over every filtered row
    [JsonPropertyName("grand_total")] public decimal GrandTotal { get; set; }
}

public class StockMoveDto
{
    [JsonPropertyName("date")] public DateTime Date { get; set; }
    [JsonPropertyName("operation_type")] public string OperationType { get; set; } = "";
    [JsonPropertyName("doc_id")] public long DocId { get; set; }
    [JsonPropertyName("doc_number")] public long? DocNumber { get; set; }
    [JsonPropertyName("is_return")] public bool IsReturn { get; set; }
    [JsonPropertyName("order_id")] public long? OrderId { get; set; }
    [JsonPropertyName("product_id")] public long ProductId { get; set; }
    [JsonPropertyName("product_name")] public string ProductName { get; set; } = "";
    [JsonPropertyName("category_name")] public string CategoryName { get; set; } = "";
    [JsonPropertyName("product_count")] public decimal ProductCount { get; set; }
    [JsonPropertyName("out_store_name")] public string OutStoreName { get; set; } = "";
    [JsonPropertyName("in_store_name")] public string InStoreName { get; set; } = "";
    [JsonPropertyName("direction")] public string Direction { get; set; } = "";       // in | out, towards the chosen store
    [JsonPropertyName("stock_after")] public decimal StockAfter { get; set; }          // the product's stock after this move

    // invoice moves only
    [JsonPropertyName("customer_name")] public string? CustomerName { get; set; }
    [JsonPropertyName("supplier_name")] public string? SupplierName { get; set; }
    [JsonPropertyName("seller_name")] public string? SellerName { get; set; }
    [JsonPropertyName("product_price")] public decimal? ProductPrice { get; set; }
    [JsonPropertyName("invoice_total")] public decimal? InvoiceTotal { get; set; }
    [JsonPropertyName("invoice_paid")] public decimal? InvoicePaid { get; set; }
    [JsonPropertyName("invoice_remaining")] public decimal? InvoiceRemaining { get; set; }
    [JsonPropertyName("commission")] public decimal Commission { get; set; }          // 0 for now
}

public class StockMovesSummaryDto
{
    [JsonPropertyName("total_in")] public decimal TotalIn { get; set; }
    [JsonPropertyName("total_out")] public decimal TotalOut { get; set; }
}