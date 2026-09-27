using CMS_Backend.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS_Backend.Models
{
    [Table("store")]
    public class Store : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "name")]
        public string Name { get; set; } = string.Empty;

        [Column(name: "manager_id")]
        [JsonPropertyName("manager_id")]
        public long? ManagerId { get; set; }
        [ForeignKey(nameof(ManagerId))]
        public virtual Employee? Manager { get; set; }

        [Column(name: "phone")]
        public string Phone { get; set; } = string.Empty;

        [Column(name: "notes")]
        public string Notes { get; set; } = string.Empty;

        [Column(name: "city_id")]
        [JsonPropertyName("city_id")]
        public long CityId { get; set; }
        [ForeignKey(nameof(CityId))]
        public virtual City? City { get; set; }

        [Column(name: "address")]
        public string Address { get; set; } = string.Empty;

        [Column(name: "active")]
        public bool Active { get; set; } = true;


        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
