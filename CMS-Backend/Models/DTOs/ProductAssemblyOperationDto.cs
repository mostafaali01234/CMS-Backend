using System.Text.Json.Serialization;

namespace CMS_Backend.Models.DTOs
{
    public class ProductAssemblyOperationDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("out_product_id")]
        public long OutProductId { get; set; }

        [JsonPropertyName("out_product_name")]
        public string OutProductName { get; set; } = string.Empty;

        [JsonPropertyName("out_store_id")]
        public long OutStoreId { get; set; }

        [JsonPropertyName("out_store_name")]
        public string OutStoreName { get; set; } = string.Empty;
        
        [JsonPropertyName("in_store_id")]
        public long InStoreId { get; set; }

        [JsonPropertyName("in_store_name")]
        public string InStoreName { get; set; } = string.Empty;
        
        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }
        
        [JsonPropertyName("operation_date")]
        public DateTime OperationDate { get; set; }
        
        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;



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
