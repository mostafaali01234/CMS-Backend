// Models/DTOs/CarDto.cs
using System.Text.Json.Serialization;

namespace CMS.Api.Models.DTOs
{
    public class StoreTransactionDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("transaction_date")]
        public DateTime TransactionDate { get; set; }

        [JsonPropertyName("auditor_id")]
        public long AuditorId { get; set; }
        [JsonPropertyName("auditor_name")]
        public string AuditorName { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("out_store_id")]
        public long OutStoreId { get; set; }

        [JsonPropertyName("out_store_name")]
        public string OutStoreName { get; set; } = string.Empty;

        [JsonPropertyName("in_store_id")]
        public long InStoreId { get; set; }

        [JsonPropertyName("in_store_name")]
        public string InStoreName { get; set; } = string.Empty;

        [JsonPropertyName("transaction_items")]
        public List<TransactionItemsDto> TransactionItems { get; set; }


        [JsonPropertyName("created_at_utc")]
        public DateTime? CreatedAtUtc { get; set; }

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updated_at_utc")]
        public DateTime? UpdatedAtUtc { get; set; }

        [JsonPropertyName("updated_by")]
        public string UpdatedBy { get; set; } = string.Empty;
    }
    public class TransactionItemsDto
    {
        [JsonPropertyName("item_id")]
        public long ItemId { get; set; }

        [JsonPropertyName("item_name")]
        public string ItemName { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }
    }
}