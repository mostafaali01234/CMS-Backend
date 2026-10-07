using CMS.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Domain.Models
{
    [Table("employee_loan")]
    public class EmployeeLoan : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "employee_id")]
        public long EmployeeId { get; set; }
        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }

        [Column(name: "approved_by_id")]
        public long? ApprovedById { get; set; }
        [ForeignKey(nameof(ApprovedById))]
        public virtual Employee? ApprovedBy { get; set; }

        [Column(name: "amount")]
        public decimal Amount { get; set; }

        [Column(name: "money_safe_id")]
        public long? MoneySafeId { get; set; }
        [ForeignKey(nameof(MoneySafeId))]
        public virtual MoneySafe? MoneySafe { get; set; }

        [Column(name: "installment_count")]
        public int InstallmentCount { get; set; }

        [Column(name: "start_date")]
        public DateTime StartDate { get; set; }          // first deduction month

        [Column(name: "notes")]
        public string Notes { get; set; } = string.Empty;

        [Column(name: "project_id")]
        public long? ProjectId { get; set; }
        [ForeignKey(nameof(ProjectId))]
        public virtual Project? Project { get; set; }

        [Column(name: "shift_id")]
        public long? ShiftId { get; set; }
        [ForeignKey(nameof(ShiftId))]
        public virtual Shift? Shift { get; set; }

        public virtual ICollection<EmployeeLoanInstallment> Installments { get; set; } = new List<EmployeeLoanInstallment>();

        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
