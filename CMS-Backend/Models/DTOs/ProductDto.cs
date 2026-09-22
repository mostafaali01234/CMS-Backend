using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS_Backend.Models.DTOs
{
    public class ProductDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("barcode")]
        public string Barcode { get; set; } = string.Empty;

        [JsonPropertyName("category_id")]
        public long CategoryId { get; set; }

        [JsonPropertyName("category_name")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("buy_price")]
        public decimal BuyPrice { get; set; }

        [JsonPropertyName("sale_price")]
        public decimal SalePrice { get; set; }

        [JsonPropertyName("storage_type")]
        public StorageType StorageType { get; set; }

        [JsonPropertyName("product_type")]
        public ProductType ProductType { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("has_serial")]
        public bool HasSerial { get; set; }

        [JsonPropertyName("enable_counter")]
        public bool EnableCounter { get; set; }

        [JsonPropertyName("enable_add_order")]
        public bool EnableAddOrder { get; set; }

        [JsonPropertyName("arrange_order")]
        public int? ArrangeOrder { get; set; }



        [JsonPropertyName("created_at_utc")]
        public DateTime? CreatedAtUtc { get; set; }
        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;
        [JsonPropertyName("updated_at_utc")]
        public DateTime? UpdatedAtUtc { get; set; }
        [JsonPropertyName("updated_by")]
        public string UpdatedBy { get; set; } = string.Empty;

    }
}
