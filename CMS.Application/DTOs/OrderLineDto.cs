// Models/DTOs/OrderNoteHistoryDto.cs
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CMS.Application.DTOs
{
    public class OrderLineRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("manager_id")]
        [Range(1, long.MaxValue)]
        public long ManagerId { get; set; }

        [JsonPropertyName("city_ids")]
        public List<long> CityIds { get; set; } = new();

        [JsonPropertyName("category_ids")]
        public List<long> CategoryIds { get; set; } = new();
    }
}