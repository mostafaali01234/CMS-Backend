using CMS.Domain.Common;
using CMS.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Domain.Models
{
    [Table("employee_loan_installment")]
    public class EmployeeLoanInstallment : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "loan_id")]
        public long LoanId { get; set; }
        [ForeignKey(nameof(LoanId))]
        public virtual EmployeeLoan? Loan { get; set; }

        [Column(name: "installment_number")]
        public int InstallmentNumber { get; set; }

        [Column(name: "due_date")]
        public DateTime DueDate { get; set; }

        [Column(name: "amount")]
        public decimal Amount { get; set; }

        [Column(name: "status")]
        public InstallmentStatus Status { get; set; }    // Pending, Deducted, Skipped

        [Column(name: "deduction_id")]
        public long? DeductionId { get; set; }           // set once payroll takes it
        [ForeignKey(nameof(DeductionId))]
        public virtual EmployeePayrollAdjustment? Deduction { get; set; }


        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }

}
