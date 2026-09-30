using CMS.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS.Domain.Models
{
    [Table("store_transaction_item")]
    public class StoreTransactionItem : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "transaction_id")]
        [JsonPropertyName("transaction_id")]
        public long TransactionId { get; set; }
        [ForeignKey(nameof(TransactionId))]
        public virtual StoreTransaction? Transaction { get; set; }
        
        [Column(name: "product_id")]
        [JsonPropertyName("product_id")]
        public long ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        [Column(name: "quantity")]
        public decimal Quantity { get; set; }



        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
