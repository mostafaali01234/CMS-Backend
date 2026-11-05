using CMS.Domain.Enums;
namespace CMS.Application.Helpers;

public static class StockOps
{
    public const string StoreTransaction = "store_transaction";
    public const string BuyInvoice = "buy_invoice";
    public const string SaleInvoice = "sale_invoice";
    public static readonly string[] All = { StoreTransaction, BuyInvoice, SaleInvoice };
}

/// <summary>a signed change of one product in one store</summary>
public record StockDelta(long StoreId, long ProductId, decimal Qty);
public record ExportResult(byte[]? Bytes, string? Error, string FileName);

/// <summary>documents whose own lines must be ignored when computing the balance (they are being changed)</summary>
public record StockExclusion(long? StoreTransactionId = null, long[]? BuyInvoiceIds = null, long[]? SaleInvoiceIds = null);

public static class StockDeltas
{
    public static List<StockDelta> Transfer(long outStore, long inStore, IEnumerable<(long ProductId, decimal Qty)> items) =>
        items.SelectMany(i => new[] { new StockDelta(outStore, i.ProductId, -i.Qty), new StockDelta(inStore, i.ProductId, i.Qty) }).ToList();

    public static List<StockDelta> Buy(InvoiceType type, IEnumerable<(long StoreId, long ProductId, decimal Qty)> lines) =>
        lines.Select(l => new StockDelta(l.StoreId, l.ProductId, type == InvoiceType.Buy ? l.Qty : -l.Qty)).ToList();

    public static List<StockDelta> Sale(InvoiceType type, IEnumerable<(long StoreId, long ProductId, decimal Qty)> lines) =>
        lines.Select(l => new StockDelta(l.StoreId, l.ProductId, type == InvoiceType.Sale ? -l.Qty : l.Qty)).ToList();

}