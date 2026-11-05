// Services/CommissionService.cs
using CMS.Application.Helpers;
using CMS.Application.Interfaces;
using CMS.Application.Interfaces.Configuration;
using CMS.Domain.Enums;
using CMS.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Services;

public class CommissionService : ICommissionService
{
    private readonly IAppDbContext _db;
    public CommissionService(IAppDbContext db) { _db = db; }

    private static bool IsLead(TechType t) => t is TechType.Tech or TechType.Tech_And_Helper;

    public async Task<string?> CheckUnlockedAsync(long? shiftId, IEnumerable<long> invoiceIds)
    {
        var ids = invoiceIds.ToList();
        var locked = await _db.EmployeeCommission.AnyAsync(c =>
            !c.IsDeleted && c.Status == CommissionStatus.Withdrawn
            && (ids.Contains(c.InvoiceId) || (shiftId != null && c.Invoice!.ShiftId == shiftId)));
        return locked ? "تم سحب عمولة هذه الوردية في كشف الرواتب، لا يمكن تعديل فواتيرها" : null;
    }

    public async Task RecalculateAsync(long? shiftId, IEnumerable<long> invoiceIds)
    {
        var extra = invoiceIds.ToList();

        // live sale invoices: the whole shift, plus the invoices that were just saved (a non-shift invoice)
        var invoices = await _db.SaleInvoice
            .Where(i => !i.IsDeleted && i.Type == InvoiceType.Sale
                     && ((shiftId != null && i.ShiftId == shiftId) || extra.Contains(i.Id)))
            .Include(i => i.Items.Where(x => !x.IsDeleted))
            .Include(i => i.Order)
            .OrderBy(i => i.Date).ThenBy(i => i.Id)
            .AsNoTracking().ToListAsync();

        // techs of each shift: the primary first, then the others in the order they were added
        var shiftIds = invoices.Where(i => i.ShiftId != null).Select(i => i.ShiftId!.Value).Distinct().ToList();
        var techRows = await _db.ShiftTech.Where(t => !t.IsDeleted && shiftIds.Contains(t.ShiftId))
            .OrderBy(t => t.Id).Select(t => new { t.ShiftId, t.TechId, t.TechType }).ToListAsync();
        var techsByShift = techRows.GroupBy(t => t.ShiftId)
            .ToDictionary(g => g.Key, g => g.OrderBy(t => IsLead(t.TechType) ? 0 : 1).Select(t => t.TechId).ToList());

        // the five fixed units (الاول .. الخامس), in their id order
        var unitIds = await _db.ProductUnit.OrderBy(u => u.Id).Select(u => u.Id).Take(5).ToListAsync();
        var productIds = invoices.SelectMany(i => i.Items).Select(x => x.ProductId).Distinct().ToList();
        var rates = await _db.ProductUnitCommission.Where(r => productIds.Contains(r.ProductId))
            .AsNoTracking().ToDictionaryAsync(r => (r.ProductId, r.UnitId));

        // ---- what the commissions should be ----
        var desired = new Dictionary<(long Invoice, long Product, long Employee, CommissionRole Role), (long Order, decimal Total, decimal Amount)>();
        var seen = new Dictionary<(long Shift, long Product), int>();     // times the product appeared in the shift so far

        foreach (var inv in invoices)
        {
            var techs = inv.ShiftId.HasValue ? techsByShift.GetValueOrDefault(inv.ShiftId.Value) ?? new List<long>() : new List<long>();

            foreach (var g in inv.Items.Where(x => !x.IsDeleted).GroupBy(x => x.ProductId))
            {
                int n = 1;                                             // no shift: always the first unit
                if (inv.ShiftId.HasValue)
                {
                    var k = (inv.ShiftId.Value, g.Key);
                    n = seen.GetValueOrDefault(k) + 1;
                    seen[k] = n;
                }
                if (inv.OrderId == null || unitIds.Count == 0) continue;

                var unitId = unitIds[Math.Min(n, unitIds.Count) - 1];  // sixth and later use الخامس
                if (!rates.TryGetValue((g.Key, unitId), out var rate)) continue;

                var total = g.Sum(x => x.ProductNetTotal);
                var orderId = inv.OrderId.Value;

                void Put(long employee, CommissionRole role, decimal amount)
                {
                    if (amount > 0) desired[(inv.Id, g.Key, employee, role)] = (orderId, total, amount);
                }

                // techs: primary = tech_comm, then assistant 1..3, the rest keep assistant 3
                for (var p = 0; p < techs.Count; p++)
                    Put(techs[p], CommissionRole.Tech,
                        p == 0 ? rate.TechComm : p == 1 ? rate.Assistant1Comm : p == 2 ? rate.Assistant2Comm : rate.Assistant3Comm);

                // seller: a percent of the product total, by the order's type
                var percent = inv.Order?.Type switch
                {
                    OrderType.جملة => rate.SalesWholesaleCommPercent,
                    OrderType.نص_جملة => rate.SalesHalfWholesaleCommPercent,
                    OrderType.تركيب => rate.SalesRetailCommPercent,
                    _ => 0m
                };
                if (inv.Order?.SellerId is long seller)
                    Put(seller, CommissionRole.Sales, Math.Round(total * percent / 100m, 2));
            }
        }

        // ---- apply to employee_commission: update in place so extra_amount, status and paid_at survive ----
        var liveIds = invoices.Select(i => i.Id).ToList();
        var existing = await _db.EmployeeCommission
            .Where(c => !c.IsDeleted && (liveIds.Contains(c.InvoiceId) || extra.Contains(c.InvoiceId)))
            .ToListAsync();

        foreach (var c in existing)
        {
            if (desired.Remove((c.InvoiceId, c.ProductId, c.EmployeeId, c.EmployeeRole), out var d))
            {
                c.OrderId = d.Order; c.ProductTotal = d.Total; c.Amount = d.Amount;
            }
            else if (c.Extra_Amount != 0) c.Amount = 0;                // keep the admin's extra amount
            else { c.IsDeleted = true; c.DeletedAtUtc = DateTime.UtcNow; }
        }

        foreach (var (k, d) in desired)
            _db.EmployeeCommission.Add(new EmployeeCommission
            {
                InvoiceId = k.Invoice,
                ProductId = k.Product,
                EmployeeId = k.Employee,
                EmployeeRole = k.Role,
                OrderId = d.Order,
                ProductTotal = d.Total,
                Amount = d.Amount,
                Status = CommissionStatus.Pending_Payment
            });

        await _db.SaveChangesAsync();
        await ApplyPaymentStatusAsync(liveIds);
    }

    public Task RefreshPaymentStatusAsync(long invoiceId) => ApplyPaymentStatusAsync(new List<long> { invoiceId });

    private async Task ApplyPaymentStatusAsync(List<long> ids)
    {
        if (ids.Count == 0) return;

        var invoices = await _db.SaleInvoice.Where(i => !i.IsDeleted && i.Type == InvoiceType.Sale && ids.Contains(i.Id))
            .Include(i => i.Payments).Include(i => i.Returns).AsNoTracking().ToListAsync();
        var rows = await _db.EmployeeCommission
            .Where(c => !c.IsDeleted && ids.Contains(c.InvoiceId) && c.Status != CommissionStatus.Withdrawn).ToListAsync();

        foreach (var inv in invoices)
        {
            var (rem, remCur) = SaleBalances.Remaining(inv);
            var paid = rem <= 0 && remCur <= 0;

            // the date the invoice got fully paid: its latest live payment (the invoice date if it has none, e.g. a zero invoice)
            var paidAt = inv.Payments.Where(p => !p.IsDeleted).Select(p => (DateTime?)p.Date).Max() ?? inv.Date;

            foreach (var c in rows.Where(r => r.InvoiceId == inv.Id))
            {
                c.Status = paid ? CommissionStatus.Invoice_Paid : CommissionStatus.Pending_Payment;
                c.PaidAt = paid ? paidAt : null;
            }
        }
        await _db.SaveChangesAsync();
    }
}