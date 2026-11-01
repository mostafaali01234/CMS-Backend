// Services/StockService.cs
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;
using CMS.Application.Interfaces;
using CMS.Application.Interfaces.Configuration;
using CMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Services;

public class StockMove
{
    public DateTime Date { get; set; }
    public int Rank { get; set; }                  // order inside one day: purchases, transfers, sales
    public long LineId { get; set; }
    public string Operation { get; set; } = "";
    public long DocId { get; set; }
    public long? DocNumber { get; set; }
    public bool IsReturn { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string? CategoryName { get; set; }
    public decimal Qty { get; set; }               // always positive: the direction is in the stores
    public decimal Price { get; set; }
    public long? OutStoreId { get; set; }
    public long? InStoreId { get; set; }
    public string? OutStoreName { get; set; }
    public string? InStoreName { get; set; }
    public long? OrderId { get; set; }
    public string? PartyName { get; set; }
    public string? SellerName { get; set; }
}

public class MoveScope
{
    public string? Operation { get; set; }
    public long? StoreId { get; set; }
    public long? ProductId { get; set; }
    public long[]? ProductIds { get; set; }
    public long? OrderId { get; set; }
    public DateTime? From { get; set; }             // inclusive
    public DateTime? ToExclusive { get; set; }
    public StockExclusion? Exclude { get; set; }
}

public class StockLine
{
    public long StoreId { get; set; }
    public long ProductId { get; set; }
    public decimal Qty { get; set; }
}

public class StockService : IStockService
{
    private readonly IAppDbContext _db;
    public StockService(IAppDbContext db) { _db = db; }

    // ===================== the one definition of "a stock move" =====================
    // When product assembly arrives, add its source here (and a rank): everything else follows.

    private IQueryable<StockMove> BuyMoves(MoveScope s)
    {
        var store = s.StoreId; var product = s.ProductId; var products = s.ProductIds;
        var from = s.From; var to = s.ToExclusive; var order = s.OrderId;
        var skip = s.Exclude?.BuyInvoiceIds ?? Array.Empty<long>();

        var q = _db.BuyInvoiceItem.Where(i => !i.IsDeleted && !i.Invoice!.IsDeleted
            && !skip.Contains(i.InvoiceId) && i.Product!.StorageType == StorageType.مخزني);

        if (store.HasValue) q = q.Where(i => i.StoreId == store.Value);
        if (product.HasValue) q = q.Where(i => i.ProductId == product.Value);
        if (products != null) q = q.Where(i => products.Contains(i.ProductId));
        if (order.HasValue) q = q.Where(i => i.Invoice!.OrderId == order.Value);
        if (from.HasValue) q = q.Where(i => i.Invoice!.Date >= from.Value);
        if (to.HasValue) q = q.Where(i => i.Invoice!.Date < to.Value);

        return q.Select(i => new StockMove
        {
            Date = i.Invoice!.Date,
            Rank = 1,
            LineId = i.Id,
            Operation = StockOps.BuyInvoice,
            DocId = i.InvoiceId,
            DocNumber = i.Invoice.Number,
            IsReturn = i.Invoice.Type == InvoiceType.Buy_Return,
            ProductId = i.ProductId,
            ProductName = i.Product!.Name,
            CategoryName = i.Product.Category!.Name,
            Qty = i.ProductQuantity,
            Price = i.ProductPrice,
            InStoreId = i.Invoice.Type == InvoiceType.Buy ? i.StoreId : (long?)null,
            OutStoreId = i.Invoice.Type == InvoiceType.Buy_Return ? i.StoreId : (long?)null,
            InStoreName = i.Invoice.Type == InvoiceType.Buy ? i.Store!.Name : null,
            OutStoreName = i.Invoice.Type == InvoiceType.Buy_Return ? i.Store!.Name : null,
            OrderId = i.Invoice.OrderId,
            PartyName = i.Invoice.Supplier!.Name,
            SellerName = null
        });
    }

    private IQueryable<StockMove> SaleMoves(MoveScope s)
    {
        var store = s.StoreId; var product = s.ProductId; var products = s.ProductIds;
        var from = s.From; var to = s.ToExclusive; var order = s.OrderId;
        var skip = s.Exclude?.SaleInvoiceIds ?? Array.Empty<long>();

        var q = _db.SaleInvoiceItem.Where(i => !i.IsDeleted && !i.Invoice!.IsDeleted
            && !skip.Contains(i.InvoiceId) && i.Product!.StorageType == StorageType.مخزني);

        if (store.HasValue) q = q.Where(i => i.StoreId == store.Value);
        if (product.HasValue) q = q.Where(i => i.ProductId == product.Value);
        if (products != null) q = q.Where(i => products.Contains(i.ProductId));
        if (order.HasValue) q = q.Where(i => i.Invoice!.OrderId == order.Value);
        if (from.HasValue) q = q.Where(i => i.Invoice!.Date >= from.Value);
        if (to.HasValue) q = q.Where(i => i.Invoice!.Date < to.Value);

        return q.Select(i => new StockMove
        {
            Date = i.Invoice!.Date,
            Rank = 3,
            LineId = i.Id,
            Operation = StockOps.SaleInvoice,
            DocId = i.InvoiceId,
            DocNumber = i.Invoice.Number,
            IsReturn = i.Invoice.Type == InvoiceType.Sale_Return,
            ProductId = i.ProductId,
            ProductName = i.Product!.Name,
            CategoryName = i.Product.Category!.Name,
            Qty = i.ProductQuantity,
            Price = i.ProductPrice,
            InStoreId = i.Invoice.Type == InvoiceType.Sale_Return ? i.StoreId : (long?)null,
            OutStoreId = i.Invoice.Type == InvoiceType.Sale ? i.StoreId : (long?)null,
            InStoreName = i.Invoice.Type == InvoiceType.Sale_Return ? i.Store!.Name : null,
            OutStoreName = i.Invoice.Type == InvoiceType.Sale ? i.Store!.Name : null,
            OrderId = i.Invoice.OrderId,
            PartyName = i.Invoice.Customer!.Name,
            SellerName = i.Invoice.Order!.Seller!.Name
        });
    }

    private IQueryable<StockMove> TransferMoves(MoveScope s)
    {
        var store = s.StoreId; var product = s.ProductId; var products = s.ProductIds;
        var from = s.From; var to = s.ToExclusive; var order = s.OrderId;
        var skip = s.Exclude?.StoreTransactionId ?? 0;

        var q = _db.StoreTransactionItem.Where(i => !i.IsDeleted && !i.Transaction!.IsDeleted
            && i.TransactionId != skip && i.Product!.StorageType == StorageType.مخزني);

        if (order.HasValue) q = q.Where(i => false);          // transfers have no order
        if (store.HasValue) q = q.Where(i => i.Transaction!.OutStoreId == store.Value || i.Transaction.InStoreId == store.Value);
        if (product.HasValue) q = q.Where(i => i.ProductId == product.Value);
        if (products != null) q = q.Where(i => products.Contains(i.ProductId));
        if (from.HasValue) q = q.Where(i => i.Transaction!.TransactionDate >= from.Value);
        if (to.HasValue) q = q.Where(i => i.Transaction!.TransactionDate < to.Value);

        return q.Select(i => new StockMove
        {
            Date = i.Transaction!.TransactionDate,
            Rank = 2,
            LineId = i.Id,
            Operation = StockOps.StoreTransaction,
            DocId = i.TransactionId,
            DocNumber = i.TransactionId,
            IsReturn = false,
            ProductId = i.ProductId,
            ProductName = i.Product!.Name,
            CategoryName = i.Product.Category!.Name,
            Qty = i.Quantity,
            Price = 0,
            InStoreId = i.Transaction.InStoreId,
            OutStoreId = i.Transaction.OutStoreId,
            InStoreName = i.Transaction.InStore!.Name,
            OutStoreName = i.Transaction.OutStore!.Name,
            OrderId = null,
            PartyName = null,
            SellerName = null
        });
    }

    private IQueryable<StockMove> Moves(MoveScope s)
    {
        IQueryable<StockMove>? q = null;
        void Add(IQueryable<StockMove> next) => q = q == null ? next : q.Concat(next);

        if (s.Operation is null or StockOps.BuyInvoice) Add(BuyMoves(s));
        if (s.Operation is null or StockOps.StoreTransaction) Add(TransferMoves(s));
        if (s.Operation is null or StockOps.SaleInvoice) Add(SaleMoves(s));
        return q ?? BuyMoves(s).Where(m => false);
    }

    private static MoveScope ScopeOf(MovesFilterDto f) => new()
    {
        Operation = string.IsNullOrWhiteSpace(f.OperationType) ? null : f.OperationType,
        StoreId = f.StoreId,
        ProductId = f.ProductId,
        OrderId = f.OrderId,
        From = f.DateFrom?.Date,
        ToExclusive = f.DateTo?.Date.AddDays(1)
    };

    // ===================== balances and the stock check =====================

    /// <summary>(store, product) -> quantity, from every move except the excluded documents</summary>
    private async Task<Dictionary<(long Store, long Product), decimal>> BalancesAsync(
        long[]? storeIds, long[]? productIds, StockExclusion? ex = null)
    {
        var m = Moves(new MoveScope { ProductIds = productIds, Exclude = ex });

        var ins = m.Where(x => x.InStoreId != null)
            .Select(x => new StockLine { StoreId = x.InStoreId ?? 0, ProductId = x.ProductId, Qty = x.Qty });
        var outs = m.Where(x => x.OutStoreId != null)
            .Select(x => new StockLine { StoreId = x.OutStoreId ?? 0, ProductId = x.ProductId, Qty = -x.Qty });

        var lines = ins.Concat(outs);
        if (storeIds != null) lines = lines.Where(l => storeIds.Contains(l.StoreId));

        var rows = await lines.GroupBy(l => new { l.StoreId, l.ProductId })
            .Select(g => new { g.Key.StoreId, g.Key.ProductId, Qty = g.Sum(x => x.Qty) })
            .ToListAsync();

        return rows.ToDictionary(r => (r.StoreId, r.ProductId), r => r.Qty);
    }

    private static Dictionary<(long, long), decimal> Sum(IEnumerable<StockDelta> d) =>
        d.GroupBy(x => (x.StoreId, x.ProductId)).ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

    public async Task<string?> CheckChangeAsync(StockExclusion ex, IEnumerable<StockDelta> before, IEnumerable<StockDelta> after)
    {
        var b = Sum(before);
        var a = Sum(after);
        var pairs = b.Keys.Union(a.Keys).ToList();
        if (pairs.Count == 0) return null;

        // services have no stock
        var wanted = pairs.Select(p => p.Item2).Distinct().ToArray();
        var stocked = await _db.Product
            .Where(p => wanted.Contains(p.Id) && p.StorageType == StorageType.مخزني)
            .Select(p => p.Id).ToListAsync();
        pairs = pairs.Where(p => stocked.Contains(p.Item2)).ToList();
        if (pairs.Count == 0) return null;

        var balances = await BalancesAsync(pairs.Select(p => p.Item1).Distinct().ToArray(), stocked.ToArray(), ex);

        foreach (var (store, product) in pairs)
        {
            var without = balances.GetValueOrDefault((store, product));            // the document taken out
            var current = without + b.GetValueOrDefault((store, product));         // how it is now
            var result = without + a.GetValueOrDefault((store, product));          // how it will be

            // only a change that makes the stock negative AND lower than it is now is refused
            if (result < 0 && result < current)
            {
                var pn = await _db.Product.Where(p => p.Id == product).Select(p => p.Name).FirstAsync();
                var sn = await _db.Store.Where(s => s.Id == store).Select(s => s.Name).FirstAsync();
                return $"الكمية غير كافية للمنتج \"{pn}\" في المخزن \"{sn}\": المتاح {Math.Max(current, 0):0.###} والرصيد بعد العملية سيصبح {result:0.###}";
            }
        }
        return null;
    }

    // ===================== stock pages =====================

    private async Task<List<(long Id, string Name, string Category, string Barcode)>> StockedProductsAsync(StockFilterDto f)
    {
        var q = _db.Product.Where(p => !p.IsDeleted && p.StorageType == StorageType.مخزني);
        if (f.CategoryId.HasValue) q = q.Where(p => p.CategoryId == f.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim();
            q = q.Where(p => p.Name.Contains(s) || p.Barcode.Contains(s));
        }

        var list = await q.OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, Category = p.Category!.Name, p.Barcode }).ToListAsync();
        return list.Select(p => (p.Id, p.Name, p.Category, p.Barcode)).ToList();
    }

    private static PagedResult<T, S> With<T, S>(PagedResult<T> p, S summary) => new()
    {
        Items = p.Items,
        Total = p.Total,
        Page = p.Page,
        PageSize = p.PageSize,
        Summary = summary
    };

    public async Task<ServiceResult<PagedResult<StockRowDto, StockSummaryDto>>> GetStoreStockAsync(StockFilterDto f)
    {
        if (!f.StoreId.HasValue)
            return ServiceResult<PagedResult<StockRowDto, StockSummaryDto>>.Failed("اختر المخزن");

        var store = f.StoreId.Value;
        var balances = await BalancesAsync(new[] { store }, null);
        var products = await StockedProductsAsync(f);

        var rows = products
            .Select(p => new StockRowDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                CategoryName = p.Category,
                Barcode = p.Barcode,
                Quantity = balances.GetValueOrDefault((store, p.Id))
            })
            .Where(r => f.IncludeZero || r.Quantity != 0)
            .ToList();

        var page = ((IReadOnlyList<StockRowDto>)rows).PageOf(f);
        return ServiceResult<PagedResult<StockRowDto, StockSummaryDto>>.Ok(
            With(page, new StockSummaryDto { TotalQuantity = rows.Sum(r => r.Quantity) }));
    }

    public async Task<ServiceResult<PagedResult<StockMatrixRowDto, StockMatrixHeaderDto>>> GetMatrixAsync(StockFilterDto f)
    {
        var stores = await _db.Store.Where(s => !s.IsDeleted && s.Active)
            .OrderBy(s => s.Name).Select(s => new StockMatrixStoreDto { Id = s.Id, Name = s.Name }).ToListAsync();

        var balances = await BalancesAsync(stores.Select(s => s.Id).ToArray(), null);
        var products = await StockedProductsAsync(f);

        var rows = products.Select(p =>
        {
            var counts = stores.Select(s => balances.GetValueOrDefault((s.Id, p.Id))).ToList();
            return new StockMatrixRowDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                CategoryName = p.Category,
                Barcode = p.Barcode,
                Counts = counts,
                Total = counts.Sum()
            };
        })
        .Where(r => f.IncludeZero || r.Counts.Any(c => c != 0))
        .ToList();

        var header = new StockMatrixHeaderDto
        {
            Stores = stores,
            Totals = stores.Select((_, i) => rows.Sum(r => r.Counts[i])).ToList(),
            GrandTotal = rows.Sum(r => r.Total)
        };

        return ServiceResult<PagedResult<StockMatrixRowDto, StockMatrixHeaderDto>>.Ok(
            With(((IReadOnlyList<StockMatrixRowDto>)rows).PageOf(f), header));
    }

    // ===================== moves pages =====================

    private async Task<Dictionary<(string, long), decimal>> RunningAsync(List<StockMove> rows, long? store)
    {
        if (rows.Count == 0) return new();

        // every earlier move of the products on this page, so the running stock is exact
        var scope = new MoveScope
        {
            ProductIds = rows.Select(r => r.ProductId).Distinct().ToArray(),
            ToExclusive = rows.Max(r => r.Date).Date.AddDays(1),
            StoreId = store
        };

        var hist = await Moves(scope)
            .Select(m => new { m.ProductId, m.Date, m.Rank, m.LineId, m.Operation, m.Qty, m.InStoreId, m.OutStoreId })
            .ToListAsync();

        decimal Delta(long? inS, long? outS, decimal q) =>
            (inS.HasValue && (store == null || inS == store) ? q : 0)
          - (outS.HasValue && (store == null || outS == store) ? q : 0);

        var result = new Dictionary<(string, long), decimal>();
        foreach (var g in hist.GroupBy(h => h.ProductId))
        {
            decimal run = 0;
            foreach (var h in g.OrderBy(h => h.Date).ThenBy(h => h.Rank).ThenBy(h => h.LineId))
            {
                run += Delta(h.InStoreId, h.OutStoreId, h.Qty);
                result[(h.Operation, h.LineId)] = run;
            }
        }
        return result;
    }

    private async Task<StockMovesSummaryDto> MovesSummaryAsync(IQueryable<StockMove> q, long? store)
    {
        var t = await q.Select(m => new { m.InStoreId, m.OutStoreId, m.Qty }).ToListAsync();
        return store.HasValue
            ? new StockMovesSummaryDto
            {
                TotalIn = t.Where(m => m.InStoreId == store).Sum(m => m.Qty),
                TotalOut = t.Where(m => m.OutStoreId == store).Sum(m => m.Qty)
            }
            // all stores: a transfer is neither in nor out, only purchases and sales change the total
            : new StockMovesSummaryDto
            {
                TotalIn = t.Where(m => m.InStoreId != null && m.OutStoreId == null).Sum(m => m.Qty),
                TotalOut = t.Where(m => m.OutStoreId != null && m.InStoreId == null).Sum(m => m.Qty)
            };
    }

    private static StockMoveDto ToMoveDto(StockMove m, long? store, decimal after) => new()
    {
        Date = m.Date,
        OperationType = m.Operation,
        DocId = m.DocId,
        DocNumber = m.DocNumber,
        IsReturn = m.IsReturn,
        OrderId = m.OrderId,
        ProductId = m.ProductId,
        ProductName = m.ProductName,
        CategoryName = m.CategoryName ?? "",
        ProductCount = m.Qty,
        OutStoreName = m.OutStoreName ?? "",
        InStoreName = m.InStoreName ?? "",
        Direction = store.HasValue ? (m.InStoreId == store ? "in" : "out") : "",
        StockAfter = after,
        Commission = 0
    };

    private string? InvalidOperation(MovesFilterDto f) =>
        !string.IsNullOrWhiteSpace(f.OperationType) && !StockOps.All.Contains(f.OperationType)
            ? "نوع العملية غير صحيح" : null;

    private async Task<(IQueryable<StockMove> Query, PagedResult<StockMove> Page)> PageMovesAsync(MovesFilterDto f)
    {
        var q = Moves(ScopeOf(f));
        var page = await q.OrderBy(m => m.Date).ThenBy(m => m.Rank).ThenBy(m => m.LineId)
        //var page = await q.OrderByDescending(m => m.Date).ThenByDescending(m => m.Rank).ThenByDescending(m => m.LineId)
            .ToPagedAsync(f);
        return (q, page);
    }

    public async Task<ServiceResult<PagedResult<StockMoveDto, StockMovesSummaryDto>>> GetStoreMovesAsync(MovesFilterDto f)
    {
        if (!f.StoreId.HasValue)
            return ServiceResult<PagedResult<StockMoveDto, StockMovesSummaryDto>>.Failed("اختر المخزن");
        if (InvalidOperation(f) is { } bad)
            return ServiceResult<PagedResult<StockMoveDto, StockMovesSummaryDto>>.Failed(bad);

        var (q, page) = await PageMovesAsync(f);
        var running = await RunningAsync(page.Items, f.StoreId);

        var items = page.Items.Select(m => ToMoveDto(m, f.StoreId, running.GetValueOrDefault((m.Operation, m.LineId)))).ToList();
        return ServiceResult<PagedResult<StockMoveDto, StockMovesSummaryDto>>.Ok(
            new PagedResult<StockMoveDto, StockMovesSummaryDto>
            {
                Items = items,
                Total = page.Total,
                Page = page.Page,
                PageSize = page.PageSize,
                Summary = await MovesSummaryAsync(q, f.StoreId)
            });
    }

    public async Task<ServiceResult<PagedResult<StockMoveDto, StockMovesSummaryDto>>> GetProductMovesAsync(MovesFilterDto f)
    {
        if (InvalidOperation(f) is { } bad)
            return ServiceResult<PagedResult<StockMoveDto, StockMovesSummaryDto>>.Failed(bad);

        var (q, page) = await PageMovesAsync(f);
        var running = await RunningAsync(page.Items, f.StoreId);   // all stores unless a store filter is set

        // the invoice figures of the rows on this page
        var saleIds = page.Items.Where(m => m.Operation == StockOps.SaleInvoice).Select(m => m.DocId).Distinct().ToList();
        var buyIds = page.Items.Where(m => m.Operation == StockOps.BuyInvoice).Select(m => m.DocId).Distinct().ToList();

        var sales = await _db.SaleInvoice.Where(i => saleIds.Contains(i.Id))
            .Include(i => i.Payments).Include(i => i.Returns).AsNoTracking().ToDictionaryAsync(i => i.Id);
        var buys = await _db.BuyInvoice.Where(i => buyIds.Contains(i.Id))
            .Include(i => i.Payments).Include(i => i.Returns).AsNoTracking().ToDictionaryAsync(i => i.Id);

        var items = page.Items.Select(m =>
        {
            var d = ToMoveDto(m, f.StoreId, running.GetValueOrDefault((m.Operation, m.LineId)));

            if (m.Operation == StockOps.BuyInvoice && buys.TryGetValue(m.DocId, out var bi))
            {
                d.SupplierName = m.PartyName;
                d.ProductPrice = m.Price;
                d.InvoiceTotal = bi.NetTotal;
                if (!m.IsReturn)
                {
                    var paid = bi.Payments.Where(p => !p.IsDeleted).Sum(p => p.Amount);
                    var returned = bi.Returns.Where(r => !r.IsDeleted).Sum(r => r.NetTotal);
                    d.InvoicePaid = paid;
                    d.InvoiceRemaining = Math.Max(0, bi.NetTotal - returned - paid);
                }
            }
            else if (m.Operation == StockOps.SaleInvoice && sales.TryGetValue(m.DocId, out var si))
            {
                d.CustomerName = m.PartyName;
                d.SellerName = m.SellerName;
                d.ProductPrice = m.Price;
                d.InvoiceTotal = si.NetTotal;
                if (!m.IsReturn)
                {
                    d.InvoicePaid = SaleBalances.Paid(si.Payments).Egp;
                    d.InvoiceRemaining = SaleBalances.Remaining(si).Egp;
                }
            }
            return d;
        }).ToList();

        return ServiceResult<PagedResult<StockMoveDto, StockMovesSummaryDto>>.Ok(
            new PagedResult<StockMoveDto, StockMovesSummaryDto>
            {
                Items = items,
                Total = page.Total,
                Page = page.Page,
                PageSize = page.PageSize,
                Summary = await MovesSummaryAsync(q, f.StoreId)
            });
    }
}