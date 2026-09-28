using CMS_Backend.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS_Backend.Models
{
    [Table("money_safe")]
    public class MoneySafe : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "cod")]
        public long Cod { get; set; }

        [Column(name: "name")]
        public string Name { get; set; } = string.Empty;

        [Column(name: "notes")]
        public string Notes { get; set; } = string.Empty;

        [Column(name: "active")]
        public bool Active { get; set; } = true;

        [Column(name: "opening_balance")]
        public decimal OpeningBalance { get; set; } = 0;

        [Column(name: "opening_balance_currency")]
        public decimal OpeningBalanceCurrency { get; set; } = 0;

        [Column(name: "manager_id")]
        [JsonPropertyName("manager_id")]
        public long? ManagerId { get; set; }
        [ForeignKey(nameof(ManagerId))]
        public virtual Employee? Manager { get; set; }

        [Column(name: "category_id")]
        [JsonPropertyName("category_id")]
        public long? CategoryId { get; set; }
        [ForeignKey(nameof(CategoryId))]
        public virtual MoneySafeCategory? Category { get; set; }

        [Column(name: "type_id")]
        [JsonPropertyName("type_id")]
        public long? TypeId { get; set; }
        [ForeignKey(nameof(TypeId))]
        public virtual MoneySafeType? Type { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
