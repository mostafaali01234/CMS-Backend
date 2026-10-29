// Services/BalanceService.cs
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using CMS.Application.Interfaces.Configuration;
using CMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Services;

public class BalanceService : IBalanceService
{
    private readonly IAppDbContext _db;
    public BalanceService(IAppDbContext db) { _db = db; }

    private record Line(DateTime Date, int Order, string Kind, long Id, long? Number, long? RefId, long? RefNumber,
                        long? OrderId, string Notes, decimal Inc, decimal Dec, decimal IncC, decimal DecC);

    private static PartyBalanceDto Finish(PartyBalanceDto r)
    {
        r.Balance = r.OpeningBalance + r.InvoicesTotal - r.ReturnsTotal - r.PaymentsTotal + r.RefundsTotal;
        r.BalanceCurrency = r.InvoicesTotalCurrency - r.ReturnsTotalCurrency - r.PaymentsTotalCurrency + r.RefundsTotalCurrency;
        return r;
    }

    // ---------------- suppliers ----------------
    public async Task<ServiceResult<List<PartyBalanceDto>>> GetSupplierBalancesAsync()
    {
        var parties = await _db.Supplier.Where(s => !s.IsDeleted).AsNoTracking()
            .Select(s => new { s.Id, s.Name, s.Phone, s.OpeningBalance }).ToListAsync();

        var inv = await _db.BuyInvoice.Where(i => !i.IsDeleted)
            .GroupBy(i => new { i.SupplierId, i.Type })
            .Select(g => new { g.Key.SupplierId, g.Key.Type, Egp = g.Sum(x => x.NetTotal), Cur = g.Sum(x => x.NetTotalCurrency) })
            .ToListAsync();

        var pay = await _db.SupplierPayment.Where(p => !p.IsDeleted)
            .GroupBy(p => p.SupplierId)
            .Select(g => new { Id = g.Key, Egp = g.Sum(x => x.Amount), Cur = g.Sum(x => x.AmountCurrency) })
            .ToListAsync();

        var rows = parties.Select(p =>
        {
            var buy = inv.Where(x => x.SupplierId == p.Id && x.Type == InvoiceType.Buy).ToList();
            var ret = inv.Where(x => x.SupplierId == p.Id && x.Type == InvoiceType.Buy_Return).ToList();
            var pa = pay.FirstOrDefault(x => x.Id == p.Id);
            return Finish(new PartyBalanceDto
            {
                Id = p.Id,
                Name = p.Name,
                Phone = p.Phone,
                OpeningBalance = p.OpeningBalance,
                InvoicesTotal = buy.Sum(x => x.Egp),
                InvoicesTotalCurrency = buy.Sum(x => x.Cur),
                ReturnsTotal = ret.Sum(x => x.Egp),
                ReturnsTotalCurrency = ret.Sum(x => x.Cur),
                PaymentsTotal = pa?.Egp ?? 0,
                PaymentsTotalCurrency = pa?.Cur ?? 0
            });
        }).ToList();

        return ServiceResult<List<PartyBalanceDto>>.Ok(rows);
    }

    public async Task<ServiceResult<StatementDto>> GetSupplierStatementAsync(long id, DateTime? from, DateTime? to)
    {
        var s = await _db.Supplier.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (s is null) return ServiceResult<StatementDto>.NotFound("Supplier not found");

        var inv = await _db.BuyInvoice.Where(i => !i.IsDeleted && i.SupplierId == id).AsNoTracking().ToListAsync();
        var pays = await _db.SupplierPayment.Where(p => !p.IsDeleted && p.SupplierId == id)
            .Include(p => p.Invoice).AsNoTracking().ToListAsync();

        var lines = new List<Line>();
        lines.AddRange(inv.Select(i => i.Type == InvoiceType.Buy
            ? new Line(i.Date, 0, "invoice", i.Id, i.Number, null, null, i.OrderId, i.Notes, i.NetTotal, 0, i.NetTotalCurrency, 0)
            : new Line(i.Date, 1, "return", i.Id, i.Number, null, null, i.OrderId, i.Notes, 0, i.NetTotal, 0, i.NetTotalCurrency)));
        lines.AddRange(pays.Select(p =>
            new Line(p.Date, 2, "payment", p.Id, null, p.InvoiceId, p.Invoice?.Number, null, p.Notes, 0, p.Amount, 0, p.AmountCurrency)));

        return ServiceResult<StatementDto>.Ok(Build(s.Id, s.Name, s.Phone, s.OpeningBalance, lines, from, to));
    }

    // ---------------- customers ----------------
    public async Task<ServiceResult<List<PartyBalanceDto>>> GetCustomerBalancesAsync()
    {
        var parties = await _db.Customer.Where(c => !c.IsDeleted).AsNoTracking()
            .Select(c => new { c.Id, c.Name, c.Phone, c.OpeningBalance }).ToListAsync();   // see "things to check"

        var inv = await _db.SaleInvoice.Where(i => !i.IsDeleted)
            .GroupBy(i => new { i.CustomerId, i.Type })
            .Select(g => new { g.Key.CustomerId, g.Key.Type, Egp = g.Sum(x => x.NetTotal), Cur = g.Sum(x => x.NetTotalCurrency) })
            .ToListAsync();

        var pay = await _db.CustomerPayment.Where(p => !p.IsDeleted)
            .GroupBy(p => new { p.CustomerId, p.Type })
            .Select(g => new { g.Key.CustomerId, g.Key.Type, Egp = g.Sum(x => x.Amount), Cur = g.Sum(x => x.AmountCurrency) })
            .ToListAsync();

        var rows = parties.Select(p =>
        {
            var sale = inv.Where(x => x.CustomerId == p.Id && x.Type == InvoiceType.Sale).ToList();
            var ret = inv.Where(x => x.CustomerId == p.Id && x.Type == InvoiceType.Sale_Return).ToList();
            var paid = pay.Where(x => x.CustomerId == p.Id && x.Type != PaymentType.Refund_Payment).ToList();
            var refunded = pay.Where(x => x.CustomerId == p.Id && x.Type == PaymentType.Refund_Payment).ToList();
            return Finish(new PartyBalanceDto
            {
                Id = p.Id,
                Name = p.Name,
                Phone = p.Phone,
                OpeningBalance = p.OpeningBalance,
                InvoicesTotal = sale.Sum(x => x.Egp),
                InvoicesTotalCurrency = sale.Sum(x => x.Cur),
                ReturnsTotal = ret.Sum(x => x.Egp),
                ReturnsTotalCurrency = ret.Sum(x => x.Cur),
                PaymentsTotal = paid.Sum(x => x.Egp),
                PaymentsTotalCurrency = paid.Sum(x => x.Cur),
                RefundsTotal = refunded.Sum(x => x.Egp),
                RefundsTotalCurrency = refunded.Sum(x => x.Cur)
            });
        }).ToList();

        return ServiceResult<List<PartyBalanceDto>>.Ok(rows);
    }

    public async Task<ServiceResult<StatementDto>> GetCustomerStatementAsync(long id, DateTime? from, DateTime? to)
    {
        var c = await _db.Customer.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (c is null) return ServiceResult<StatementDto>.NotFound("Customer not found");

        var inv = await _db.SaleInvoice.Where(i => !i.IsDeleted && i.CustomerId == id).AsNoTracking().ToListAsync();
        var pays = await _db.CustomerPayment.Where(p => !p.IsDeleted && p.CustomerId == id)
            .Include(p => p.Invoice).AsNoTracking().ToListAsync();

        var lines = new List<Line>();
        lines.AddRange(inv.Select(i => i.Type == InvoiceType.Sale
            ? new Line(i.Date, 0, "invoice", i.Id, i.Number, null, null, i.OrderId, i.Notes, i.NetTotal, 0, i.NetTotalCurrency, 0)
            : new Line(i.Date, 1, "return", i.Id, i.Number, null, null, i.OrderId, i.Notes, 0, i.NetTotal, 0, i.NetTotalCurrency)));
        lines.AddRange(pays.Select(p => p.Type == PaymentType.Refund_Payment
            // money we paid back: the customer owes us more
            ? new Line(p.Date, 3, "refund", p.Id, null, p.InvoiceId, p.Invoice?.Number, p.Invoice?.OrderId, p.Notes, p.Amount, 0, p.AmountCurrency, 0)
            : new Line(p.Date, 2, p.Type == PaymentType.Advance_Payment ? "advance" : "payment", p.Id, null, p.InvoiceId,
                       p.Invoice?.Number, p.AdvancePaymentOrderId ?? p.Invoice?.OrderId, p.Notes, 0, p.Amount, 0, p.AmountCurrency)));

        return ServiceResult<StatementDto>.Ok(Build(c.Id, c.Name, c.Phone, c.OpeningBalance, lines, from, to));
    }

    // ---------------- shared ----------------
    private static StatementDto Build(long id, string name, string phone, decimal opening,
                                      List<Line> all, DateTime? from, DateTime? to)
    {
        var sorted = all.OrderBy(l => l.Date.Date).ThenBy(l => l.Order).ThenBy(l => l.Id).ToList();

        decimal bal = opening, balC = 0;
        decimal openBal = opening, openBalC = 0;
        var rows = new List<StatementLineDto>();

        foreach (var l in sorted)
        {
            bal += l.Inc - l.Dec;
            balC += l.IncC - l.DecC;

            if (from.HasValue && l.Date.Date < from.Value.Date) { openBal = bal; openBalC = balC; continue; }
            if (to.HasValue && l.Date.Date > to.Value.Date) continue;

            rows.Add(new StatementLineDto
            {
                Date = l.Date,
                Kind = l.Kind,
                Id = l.Id,
                Number = l.Number,
                RefId = l.RefId,
                RefNumber = l.RefNumber,
                OrderId = l.OrderId,
                Notes = l.Notes ?? "",
                Increase = l.Inc,
                Decrease = l.Dec,
                IncreaseCurrency = l.IncC,
                DecreaseCurrency = l.DecC,
                Balance = bal,
                BalanceCurrency = balC
            });
        }

        return new StatementDto
        {
            PartyId = id,
            PartyName = name,
            PartyPhone = phone,
            OpeningBalance = openBal,
            OpeningBalanceCurrency = openBalC,
            ClosingBalance = rows.Count > 0 ? rows[^1].Balance : openBal,
            ClosingBalanceCurrency = rows.Count > 0 ? rows[^1].BalanceCurrency : openBalC,
            Lines = rows
        };
    }
}