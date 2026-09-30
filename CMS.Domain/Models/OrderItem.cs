using CMS.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Domain.Models
{
    [Table("order_item")]
    public class OrderItem : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "order_id")]
        public long OrderId { get; set; }
        [ForeignKey(nameof(OrderId))]
        public virtual Order? Order { get; set; }

        [Column(name: "product_id")]
        public long ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

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
