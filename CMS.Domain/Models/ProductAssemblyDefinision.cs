using CMS.Domain.Enums;
using CMS.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS.Domain.Models
{
    [Table("product_assembly_definition")]
    public class ProductAssemblyDefinition : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "out_product_id")]
        [JsonPropertyName("out_product_id")]
        public long OutProductId { get; set; }
        [ForeignKey(nameof(OutProductId))]
        public virtual Product? OutProduct { get; set; }

        [Column(name: "in_product_id")]
        [JsonPropertyName("in_product_id")]
        public long InProductId { get; set; }
        [ForeignKey(nameof(InProductId))]
        public virtual Product? InProduct { get; set; }

        [Column(name: "in_quantity")]
        [JsonPropertyName("in_quantity")]
        public decimal InQuantity { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
