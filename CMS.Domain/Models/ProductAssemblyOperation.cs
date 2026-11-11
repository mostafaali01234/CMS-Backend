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
        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [Column(name: "operation_date")]
        [JsonPropertyName("operation_date")]
        public DateTime OperationDate { get; set; }

        [Column(name: "operation_type")]
        [JsonPropertyName("operation_type")]
        public ProductAssemblyOperationType OperationType { get; set; }

        [Column(name: "first_tech_id")]
        [JsonPropertyName("first_tech_id")]
        public long? FirstTechId { get; set; }
        [ForeignKey(nameof(FirstTechId))]
        public virtual Employee? FirstTech { get; set; }

        [Column(name: "second_tech_id")]
        [JsonPropertyName("second_tech_id")]
        public long? SecondTechId { get; set; }
        [ForeignKey(nameof(SecondTechId))]
        public virtual Employee? SecondTech { get; set; }

        [Column(name: "notes")]
        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        // the components consumed from OutStore (OutProduct itself enters InStore)
        public virtual ICollection<ProductAssemblyOperationItem> Items { get; set; } = new List<ProductAssemblyOperationItem>();


        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
