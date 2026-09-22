using CMS_Backend.Models.DTOs;
using CMS_Backend.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS_Backend.Models
{
    [Table("product")]
    public class Product : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "name")]
        public string Name { get; set; } = string.Empty;

        [Column(name: "description")]
        public string? Description { get; set; } = string.Empty;

        [Column(name: "barcode")]
        public string Barcode { get; set; } = string.Empty;

        [Column(name: "category_id")]
        [JsonPropertyName("category_id")]
        public long CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public virtual ProductCategory? Category { get; set; }

        [Column(name: "buy_price")]
        public decimal BuyPrice { get; set; } = 0;

        [Column(name: "sale_price")]
        public decimal SalePrice { get; set; } = 0;

        [Column(name: "storage_type")]
        public StorageType StorageType { get; set; } = 0;

        [Column(name: "product_type")]
        public ProductType ProductType { get; set; } = 0;

        [Column(name: "active")]
        public bool Active { get; set; } = true;

        [Column(name: "has_serial")]
        public bool HasSerial { get; set; } = false;

        [Column(name: "enable_counter")]
        public bool EnableCounter { get; set; } = true;

        [Column(name: "enable_add_order")]
        public bool EnableAddOrder { get; set; } = true;

        [Column(name: "arrange_order")]
        public int? ArrangeOrder { get; set; }


        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
