using CMS.Domain.Common;
using CMS.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Domain.Models
{
    [Table("employee_payroll")]
    public class EmployeePayroll : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "employee_id")]
        public long EmployeeId { get; set; }
        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }

        [Column(name: "month")]
        public int Month { get; set; }

        [Column(name: "year")]
        public int Year { get; set; }

        [Column(name: "base_salary")]
        public decimal BaseSalary { get; set; }  

        [Column(name: "sales_commission_total")]
        public decimal SalesCommissionTotal { get; set; }  

        [Column(name: "tech_commission_total")]
        public decimal TechCommissionTotal { get; set; }  

        [Column(name: "loans_total")]
        public decimal LoansTotal { get; set; }  

        [Column(name: "bonus_total")]
        public decimal BonusTotal { get; set; }  

        [Column(name: "deductions_total")]
        public decimal DeductionsTotal { get; set; }  

        [Column(name: "lunch_total")]
        public decimal LunchTotal { get; set; }  

        [Column(name: "net_total")]
        public decimal NetTotal { get; set; }  

        [Column(name: "status")]
        public PayrollStatus Status { get; set; }

        [Column(name: "revised_by_id")]
        public long? RevisedById { get; set; }
        [ForeignKey(nameof(RevisedById))]
        public virtual Employee? RevisedBy { get; set; }

        [Column(name: "money_safe_id")]
        public long? MoneySafeId { get; set; }
        [ForeignKey(nameof(MoneySafeId))]
        public virtual MoneySafe? MoneySafe { get; set; }

        [Column(name: "withdrawn_at")]
        public DateTime? WithdrawnAt { get; set; }

        [Column(name: "withdrawn_by_id")]
        public long? WithdrawnById { get; set; }
        [ForeignKey(nameof(WithdrawnById))]
        public virtual Employee? WithdrawnBy { get; set; }



        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
