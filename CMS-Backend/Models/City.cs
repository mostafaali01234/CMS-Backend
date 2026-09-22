using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS_Backend.Models
{
    [Table("city")]
    public class City
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }
        [Column(name: "name")]
        public string Name { get; set; } = string.Empty;
        [Column(name: "state_id")]
        [JsonPropertyName("state_id")]
        public long StateId { get; set; }

        [ForeignKey(nameof(StateId))]
        public virtual CountryState? State { get; set; }
    }
}
