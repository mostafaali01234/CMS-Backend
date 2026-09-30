using System.Text.Json.Serialization;

namespace CMS.Application.DTOs
{
    public class ProductAssemblyDefinitionDto
    {
        [JsonPropertyName("out_product_id")]
        public long OutProductId { get; set; }

        [JsonPropertyName("out_product_name")]
        public string OutProductName { get; set; } = string.Empty;
        
        [JsonPropertyName("in_product_list")]
        public List<AssemblyDefinitionInProduct> InProductList { get; set; }


        [JsonPropertyName("created_at_utc")]
        public DateTime? CreatedAtUtc { get; set; }
        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;
        [JsonPropertyName("updated_at_utc")]
        public DateTime? UpdatedAtUtc { get; set; }
        [JsonPropertyName("updated_by")]
        public string UpdatedBy { get; set; } = string.Empty;
    }
    
    public class AssemblyDefinitionInProduct
    {
        [JsonPropertyName("in_product_id")]
        public long InProductId { get; set; }

        [JsonPropertyName("in_product_name")]
        public string InProductName { get; set; } = string.Empty;
        
        [JsonPropertyName("in_product_quantity")]
        public decimal InProductQuantity { get; set; }
    }

}
