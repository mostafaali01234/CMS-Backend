using CMS_Backend.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS_Backend.Models
{
    [Table("supplier")]
    public class Supplier : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }
        [Column(name: "name")]
        public string Name { get; set; } = string.Empty;
        [Column(name: "name_en")]
        [JsonPropertyName("name_en")]
        public string NameEn { get; set; } = string.Empty;
        [Column(name: "notes")]
        public string Notes { get; set; } = string.Empty;
        [Column(name: "phone")]
        public string Phone { get; set; } = string.Empty;
        [Column(name: "email")]
        public string Email { get; set; } = string.Empty;
        [Column(name: "tax_card")]
        [JsonPropertyName("tax_card")]
        public string TaxCard { get; set; } = string.Empty;
        [Column(name: "commercial_register")]
        [JsonPropertyName("commercial_register")]
        public string CommercialRegister { get; set; } = string.Empty;
        [Column(name: "city_id")]
        [JsonPropertyName("city_id")]
        public long CityId { get; set; }

        [ForeignKey(nameof(CityId))]
        public virtual City? City { get; set; }
        [Column(name: "Address")]
        public string Address { get; set; } = string.Empty;
        [Column(name: "opening_balance")]
        public decimal OpeningBalance { get; set; } = 0;
        [Column(name: "fixed_discount")]
        public decimal FixedDiscount { get; set; } = 0;


        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
