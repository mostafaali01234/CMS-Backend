using CMS.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS.Domain.Models
{
    [Table("sales_manager_group_product")]
    public class SalesManagerGroupProduct : IAuditable, ISoftDeletable
    {
        [Key][Column(name: "id")] public long Id { get; set; }

        [Column(name: "group_id")] public long GroupId { get; set; }
        [ForeignKey(nameof(GroupId))] public virtual SalesManagerGroup? Group { get; set; }

        [Column(name: "product_id")] public long ProductId { get; set; }
        [ForeignKey(nameof(ProductId))] public virtual Product? Product { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
