using CMS.Domain.Common;
using CMS.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Domain.Models
{
    [Table("supplier_payment")]
    public class SupplierPayment : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "date")]
        public DateTime Date { get; set; }

        [Column(name: "supplier_id")]
        public long SupplierId { get; set; }
        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier? Supplier { get; set; }

        [Column(name: "type")]
        public PaymentType Type { get; set; } = PaymentType.Regular_Payment;

        [Column(name: "buy_invoice_id")]
        public long? InvoiceId { get; set; }
        [ForeignKey(nameof(InvoiceId))]
        public virtual BuyInvoice? Invoice { get; set; }

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


        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
