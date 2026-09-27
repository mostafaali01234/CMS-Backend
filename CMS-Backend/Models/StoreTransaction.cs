using CMS_Backend.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS_Backend.Models
{
    [Table("store_transaction")]
    public class StoreTransaction : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "transaction_date")]
        public DateTime TransactionDate { get; set; }

        [Column(name: "auditor_id")]
        [JsonPropertyName("auditor_id")]
        public long AuditorId { get; set; }
        [ForeignKey(nameof(AuditorId))]
        public virtual Employee? Auditor { get; set; }

        [Column(name: "notes")]
        public string Notes { get; set; } = string.Empty;

        [Column(name: "out_store_id")]
        [JsonPropertyName("out_store_id")]
        public long OutStoreId { get; set; }
        [ForeignKey(nameof(OutStoreId))]
        public virtual Store? OutStore { get; set; }

        [Column(name: "in_store_id")]
        [JsonPropertyName("in_store_id")]
        public long InStoreId { get; set; }
        [ForeignKey(nameof(InStoreId))]
        public virtual Store? InStore { get; set; }


        public virtual ICollection<StoreTransactionItem> Items { get; set; } = new List<StoreTransactionItem>();



        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
