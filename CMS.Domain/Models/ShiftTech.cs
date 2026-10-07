using CMS.Domain.Common;
using CMS.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Domain.Models
{
    [Table("shift_tech")]
    public class ShiftTech : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }
        
        [Column(name: "date")]
        public DateTime Date { get; set; }

        [Column(name: "shift_id")]
        public long ShiftId { get; set; }
        [ForeignKey(nameof(ShiftId))]
        public virtual Shift? Shift { get; set; }

        [Column(name: "tech_id")]
        public long TechId { get; set; }
        [ForeignKey(nameof(TechId))]
        public virtual Employee? Tech { get; set; }

        [Column(name: "tech_type")]
        public TechType TechType { get; set; } = TechType.Tech;



        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
