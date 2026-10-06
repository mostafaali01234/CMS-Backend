using CMS.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Domain.Models
{
    [Table("employee_special_commission_definition")]
    public class EmployeeSpecialCommissionDefinition : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "manager_id")]
        public long ManagerId { get; set; }

        [ForeignKey(nameof(ManagerId))]
        public virtual Employee? Manager { get; set; }

        [Column(name: "commission_rate")]
        public decimal CommissionRate { get; set; } = 0;


        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();


        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
