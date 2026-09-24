// Models/DTOs/CustomerDto.cs
using System.Text.Json.Serialization;

namespace CMS_Backend.Models.DTOs
{
    public class CustomerDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;

        [JsonPropertyName("phone_2")]
        public string? Phone2 { get; set; } = string.Empty;

        [JsonPropertyName("phone_3")]
        public string? Phone3 { get; set; } = string.Empty;

        [JsonPropertyName("city_id")]
        public long CityId { get; set; }

        [JsonPropertyName("city_name")]
        public string CityName { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string? Address { get; set; } = string.Empty;

        [JsonPropertyName("opening_balance")]
        public decimal OpeningBalance { get; set; }

        [JsonPropertyName("seller_id")]
        public string SellerId { get; set; } = string.Empty;

        [JsonPropertyName("seller_name")]
        public string SellerName { get; set; } = string.Empty;

        [JsonPropertyName("regular_customer")]
        public bool? RegularCustomer { get; set; }

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