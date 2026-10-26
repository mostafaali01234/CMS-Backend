// CMS.Application/Services/SaleBalances.cs
using CMS.Domain.Enums;
using CMS.Domain.Models;
namespace CMS.Application.Helpers;

public static class SaleBalances
{
    // regular + advance payments (refunds are not payments towards the invoice)
    public static (decimal Egp, decimal Cur) Paid(IEnumerable<CustomerPayment>? p)
    {
        var l = p?.Where(x => !x.IsDeleted && x.Type != PaymentType.Refund_Payment).ToList() ?? new();
        return (l.Sum(x => x.Amount), l.Sum(x => x.AmountCurrency));
    }

    public static (decimal Egp, decimal Cur) Refunded(IEnumerable<CustomerPayment>? p)
    {
        var l = p?.Where(x => !x.IsDeleted && x.Type == PaymentType.Refund_Payment).ToList() ?? new();
        return (l.Sum(x => x.Amount), l.Sum(x => x.AmountCurrency));
    }

    public static (decimal Egp, decimal Cur) Returned(IEnumerable<SaleInvoice>? r)
    {
        var l = r?.Where(x => !x.IsDeleted).ToList() ?? new();
        return (l.Sum(x => x.NetTotal), l.Sum(x => x.NetTotalCurrency));
    }

    // needs Payments and Returns loaded
    public static (decimal Egp, decimal Cur) Remaining(SaleInvoice inv)
    {
        var paid = Paid(inv.Payments);
        var ret = Returned(inv.Returns);
        return (Math.Max(0, inv.NetTotal - ret.Egp - paid.Egp),
                Math.Max(0, inv.NetTotalCurrency - ret.Cur - paid.Cur));
    }
}