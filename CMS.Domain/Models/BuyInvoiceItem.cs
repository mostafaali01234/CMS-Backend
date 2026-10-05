using CMS.Domain.Common;
using CMS.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Domain.Models
{
    [Table("buy_invoice_item")]
    public class BuyInvoiceItem : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "invoice_id")]
        public long InvoiceId { get; set; }
        [ForeignKey(nameof(InvoiceId))]
        public virtual BuyInvoice? Invoice { get; set; }

        [Column(name: "product_id")]
        public long ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        [Column(name: "store_id")]
        public long StoreId { get; set; }
        [ForeignKey(nameof(StoreId))]
        public virtual Store? Store { get; set; }

        [Column(name: "project_id")]
        public long? ProjectId { get; set; }
        [ForeignKey(nameof(ProjectId))]
        public virtual Project? Project { get; set; }

        [Column(name: "product_notes")]
        public string ProductNotes { get; set; } = string.Empty;

        [Column(name: "price_type")]
        public PriceType PriceType { get; set; }
        
        [Column(name: "product_price")]
        public decimal ProductPrice { get; set; }

        [Column(name: "product_quantity")]
        public decimal ProductQuantity { get; set; }

        [Column(name: "product_total")]
        public decimal ProductTotal { get; set; }

        [Column(name: "product_discount")]
        public decimal ProductDiscount { get; set; }

        [Column(name: "product_net_total")]
        public decimal ProductNetTotal { get; set; }



        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
