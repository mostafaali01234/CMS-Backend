using CMS.Domain.Common;
using CMS.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Domain.Models
{
    [Table("employee_commission")]
    public class EmployeeCommission : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "order_id")]
        public long OrderId { get; set; }
        [ForeignKey(nameof(OrderId))]
        public virtual Order? Order { get; set; }

        [Column(name: "invoice_id")]
        public long InvoiceId { get; set; }
        [ForeignKey(nameof(InvoiceId))]
        public virtual SaleInvoice? Invoice { get; set; }

        [Column(name: "employee_id")]
        public long EmployeeId { get; set; }
        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }

        [Column(name: "employee_role")]
        public CommissionRole EmployeeRole { get; set; }

        [Column(name: "product_id")]
        public long ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        [Column(name: "product_total")]
        public decimal ProductTotal { get; set; }

        [Column(name: "amount")]
        public decimal Amount { get; set; }

        [Column(name: "extra_amount")]
        public decimal Extra_Amount { get; set; } = 0;

        [Column(name: "status")]
        public CommissionStatus Status { get; set; }

        [Column(name: "paid_at")]
        public DateTime? PaidAt { get; set; }

        [Column(name: "withdrawn_at")]
        public DateTime? WithdrawnAt { get; set; }

        //[Column(name: "payroll_id")]
        //public long? PayrollId { get; set; }
        //[ForeignKey(nameof(PayrollId))]
        //public virtual EmployeePayroll? Payroll { get; set; }




        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
