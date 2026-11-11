using CMS.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS.Domain.Models
{
    // One component an assembly operation consumed.
    // A snapshot of the product's assembly definition taken when the operation is saved,
    // so later edits (or deletion) of the definition never change the stock of old operations.
    [Table("product_assembly_operation_item")]
    public class ProductAssemblyOperationItem : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "operation_id")]
        [JsonPropertyName("operation_id")]
        public long OperationId { get; set; }
        [ForeignKey(nameof(OperationId))]
        public virtual ProductAssemblyOperation? Operation { get; set; }

        [Column(name: "product_id")]
        [JsonPropertyName("product_id")]
        public long ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        // consumed for ONE unit of the assembled product (the definition's in_quantity when the operation was saved)
        [Column(name: "unit_quantity")]
        [JsonPropertyName("unit_quantity")]
        public decimal UnitQuantity { get; set; }

        // consumed by the whole operation = UnitQuantity x operation.Quantity
        [Column(name: "quantity")]
        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
