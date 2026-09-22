using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS_Backend.Models
{
    [Table("country_state")]
    public class CountryState
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }
        [Column(name: "name")]
        public string Name { get; set; } = string.Empty;
        [Column(name: "order_number")]
        [JsonPropertyName("order_number")]
        public int OrderNumber { get; set; }
        public virtual ICollection<City> Cities { get; set; } = new List<City>();
    }
}
