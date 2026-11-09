using CMS.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS.Domain.Models
{
    [Table("sales_manager_group")]
    public class SalesManagerGroup : IAuditable, ISoftDeletable
    {
        [Key][Column(name: "id")] public long Id { get; set; }

        [Column(name: "manager_id")] public long ManagerId { get; set; }
        [ForeignKey(nameof(ManagerId))] public virtual Employee? Manager { get; set; }

        [Column(name: "percent")] public decimal Percent { get; set; }
        [Column(name: "active")] public bool Active { get; set; } = true;
        [Column(name: "notes")] public string Notes { get; set; } = string.Empty;

        public virtual ICollection<SalesManagerGroupSeller> Sellers { get; set; } = new List<SalesManagerGroupSeller>();
        public virtual ICollection<SalesManagerGroupProduct> Products { get; set; } = new List<SalesManagerGroupProduct>();

        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
