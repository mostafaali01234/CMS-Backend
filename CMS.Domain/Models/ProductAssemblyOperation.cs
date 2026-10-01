using CMS.Domain.Enums;
using CMS.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS.Domain.Models
{
    [Table("product_assembly_operation")]
    public class ProductAssemblyOperation : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "out_product_id")]
        [JsonPropertyName("out_product_id")]
        public long OutProductId { get; set; }
        [ForeignKey(nameof(OutProductId))]
        public virtual Product? OutProduct { get; set; }

        [Column(name: "in_store_id")]
        [JsonPropertyName("in_store_id")]
        public long InStoreId { get; set; }
        [ForeignKey(nameof(InStoreId))]
        public virtual Store? InStore { get; set; }

        [Column(name: "out_store_id")]
        [JsonPropertyName("out_store_id")]
        public long OutStoreId { get; set; }
        [ForeignKey(nameof(OutStoreId))]
        public virtual Store? OutStore { get; set; }

        [Column(name: "quantity")]
        public decimal Quantity { get; set; }

        [Column(name: "operation_date")]
        public DateTime OperationDate { get; set; }

        [Column(name: "notes")]
        public string Notes { get; set; } = string.Empty;


        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
