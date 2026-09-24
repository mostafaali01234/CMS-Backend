using CMS_Backend.Models.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS_Backend.Models
{
    [Table("customer")]
    public class Customer : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }
        [Column(name: "name")]
        public string Name { get; set; } = string.Empty;
        [Column(name: "notes")]
        public string Notes { get; set; } = string.Empty;
        [Column(name: "phone")]
        public string Phone { get; set; } = string.Empty;
        [Column(name: "phone_2")]
        public string? Phone2 { get; set; } = string.Empty;
        [Column(name: "phone_3")]
        public string? Phone3 { get; set; } = string.Empty;

        [Column(name: "city_id")]
        [JsonPropertyName("city_id")]
        public long CityId { get; set; }

        [ForeignKey(nameof(CityId))]
        public virtual City? City { get; set; }
        [Column(name: "Address")]
        public string? Address { get; set; } = string.Empty;
        [Column(name: "opening_balance")]
        public decimal OpeningBalance { get; set; } = 0;

        [Column(name: "seller_id")]
        [JsonPropertyName("seller_id")]
        public string SellerId { get; set; }

        [ForeignKey(nameof(SellerId))]
        public virtual IdentityUser? Seller { get; set; }
        [Column(name: "regular_customer")]
        public bool? RegularCustomer { get; set; } = false;


        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
