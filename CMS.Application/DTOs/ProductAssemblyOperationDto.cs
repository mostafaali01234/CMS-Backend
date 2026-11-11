using CMS.Domain.Enums;
using System.Text.Json.Serialization;

namespace CMS.Application.DTOs
{
    public class ProductAssemblyOperationItemDto
    {
        [JsonPropertyName("product_id")]
        public long ProductId { get; set; }

        [JsonPropertyName("product_name")]
        public string ProductName { get; set; } = string.Empty;

        // consumed for one unit of the assembled product
        [JsonPropertyName("unit_quantity")]
        public decimal UnitQuantity { get; set; }

        // consumed by the whole operation
        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }
    }

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

        [JsonPropertyName("operation_type")]
        public ProductAssemblyOperationType OperationType { get; set; }

        [JsonPropertyName("first_tech_id")]
        public long? FirstTechId { get; set; }

        [JsonPropertyName("first_tech_name")]
        public string FirstTechName { get; set; } = string.Empty;

        [JsonPropertyName("second_tech_id")]
        public long? SecondTechId { get; set; }

        [JsonPropertyName("second_tech_name")]
        public string SecondTechName { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        // read-only: the components this operation consumed (taken from the definition when it was saved)
        [JsonPropertyName("items")]
        public List<ProductAssemblyOperationItemDto> Items { get; set; } = new();



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
