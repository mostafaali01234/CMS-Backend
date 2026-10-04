using CMS.Domain.Common;
using CMS.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Domain.Models
{
    [Table("employee_payroll_adjustment")]
    public class EmployeePayrollAdjustment : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "employee_id")]
        public long EmployeeId { get; set; }
        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }

        [Column(name: "operation_date")]
        public DateTime OperationDate { get; set; }

        [Column(name: "emp_kind")]
        public PayrollAdjustmentKind Kind { get; set; }

        [Column(name: "type")]
        public PayrollAdjustmentType Type { get; set; }

        [Column(name: "amount")]
        public decimal Amount { get; set; }  

        [Column(name: "reason")]
        public string Reason { get; set; } = string.Empty;

        [Column(name: "approved_by_id")]
        public long? ApprovedById { get; set; }
        [ForeignKey(nameof(ApprovedById))]
        public virtual Employee? ApprovedBy { get; set; }



        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
