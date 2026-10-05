using CMS.Domain.Common;
using CMS.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Domain.Models
{
    [Table("customer_payment")]
    public class CustomerPayment : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "date")]
        public DateTime Date { get; set; }

        [Column(name: "customer_id")]
        public long CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public virtual Customer? Customer { get; set; }

        [Column(name: "type")]
        public PaymentType Type { get; set; } = PaymentType.Regular_Payment;

        [Column(name: "sale_invoice_id")]
        public long? InvoiceId { get; set; }
        [ForeignKey(nameof(InvoiceId))]
        public virtual SaleInvoice? Invoice { get; set; }

        [Column(name: "money_safe_id")]
        public long MoneySafeId { get; set; }
        [ForeignKey(nameof(MoneySafeId))]
        public virtual MoneySafe? MoneySafe { get; set; }

        [Column(name: "amount")]
        public decimal Amount { get; set; }

        [Column(name: "amount_currency")]
        public decimal AmountCurrency { get; set; }

        [Column(name: "notes")]
        public string Notes { get; set; } = string.Empty;

        [Column(name: "auditor_id")]
        public long AuditorId { get; set; }

        [ForeignKey(nameof(AuditorId))]
        public virtual Employee? Auditor { get; set; }

        [Column(name: "advance_payment_order_id")]
        public long? AdvancePaymentOrderId { get; set; }


        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
