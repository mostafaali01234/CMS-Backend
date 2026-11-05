// Services/Interfaces/ICommissionService.cs
public interface ICommissionService
{
    /// <summary>null when allowed; otherwise the message (a commission of this shift/invoice was already withdrawn in a payroll)</summary>
    Task<string?> CheckUnlockedAsync(long? shiftId, IEnumerable<long> invoiceIds);

    /// <summary>recomputes the commissions of the shift's sale invoices plus the given invoices (also deleted ones), and saves</summary>
    Task RecalculateAsync(long? shiftId, IEnumerable<long> invoiceIds);

    /// <summary>sets Pending_Payment / Invoice_Paid and paid_at from the invoice's payments, and saves</summary>
    Task RefreshPaymentStatusAsync(long invoiceId);
}