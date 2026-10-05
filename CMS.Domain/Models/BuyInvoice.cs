using CMS.Domain.Common;
using CMS.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Domain.Models
{
    [Table("buy_invoice")]
    public class BuyInvoice : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "number")]
        public long Number { get; set; }
        
        [Column(name: "type")]
        public InvoiceType Type { get; set; }

        [Column(name: "date")]
        public DateTime Date { get; set; }

        [Column(name: "notes")]
        public string Notes { get; set; } = string.Empty;

        [Column(name: "supplier_id")]
        public long SupplierId { get; set; }
        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier? Supplier { get; set; }

        [Column(name: "auditor_id")]
        public long AuditorId { get; set; }
        [ForeignKey(nameof(AuditorId))]
        public virtual Employee? Auditor { get; set; }

        [Column(name: "order_id")]
        public long? OrderId { get; set; }
        [ForeignKey(nameof(OrderId))]
        public virtual Order? Order { get; set; }

        [Column(name: "original_invoice_id")]
        public long? OriginalInvoiceId { get; set; }
        [ForeignKey(nameof(OriginalInvoiceId))]
        public virtual BuyInvoice? OriginalInvoice { get; set; }

        [Column(name: "total")]
        public decimal Total { get; set; }

        [Column(name: "discount")]
        public decimal Discount { get; set; }

        [Column(name: "net_total")]
        public decimal NetTotal { get; set; }

        [Column(name: "net_total_currency")]
        public decimal NetTotalCurrency { get; set; } = decimal.Zero;

        public virtual ICollection<BuyInvoiceItem> Items { get; set; } = new List<BuyInvoiceItem>();
        public virtual ICollection<SupplierPayment> Payments { get; set; } = new List<SupplierPayment>();

        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
