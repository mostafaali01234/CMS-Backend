using CMS.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Domain.Models
{
    [Table("product_category")]
    public class ProductCategory : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }
        [Column(name: "name")]
        public string Name { get; set; } = string.Empty;
        [Column(name: "description")]
        public string? Description { get; set; } = string.Empty;

        public virtual ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();

        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
