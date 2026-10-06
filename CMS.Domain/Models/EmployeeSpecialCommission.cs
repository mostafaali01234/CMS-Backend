using CMS.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Domain.Models
{
    [Table("employee_special_commission")]
    public class EmployeeSpecialCommission : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "manager_id")]
        public long ManagerId { get; set; }
        [ForeignKey(nameof(ManagerId))]
        public virtual Employee? Manager { get; set; }

        [Column(name: "employee_id")]
        public long EmployeeId { get; set; }
        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }

        [Column(name: "month")]
        public int Month { get; set; }

        [Column(name: "year")]
        public int Year { get; set; }

        [Column(name: "sales_total")]
        public decimal SalesTotal { get; set; } = 0;

        [Column(name: "commission_rate")]
        public decimal CommissionRate { get; set; } = 0;

        [Column(name: "commission_Total")]
        public decimal CommissionTotal { get; set; } = 0;

        [Column(name: "notes")]
        public string Notes { get; set; } = string.Empty;


        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
