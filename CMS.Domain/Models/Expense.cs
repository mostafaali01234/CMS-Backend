using CMS.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS.Domain.Models
{
    [Table("expense")]
    public class Expense : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "expense_type_id")]
        public long ExpenseTypeId { get; set; }

        [ForeignKey(nameof(ExpenseTypeId))]
        public virtual ExpenseType? ExpenseType { get; set; }

        [Column(name: "amount")]
        public decimal Amount { get; set; } = 0;

        [Column(name: "amount_currency")]
        public decimal AmountCurrency { get; set; } = 0;

        [Column(name: "notes")]
        public string Notes { get; set; } = string.Empty;

        [Column(name: "data")] //  {car_id, oil_start_km, oil_end_km, gas_current_km, gas_litres_count}
        public string Data { get; set; } = string.Empty;

        [Column(name: "money_safe_id")]
        public long MoneySafeId { get; set; }

        [ForeignKey(nameof(MoneySafeId))]
        public virtual MoneySafe? MoneySafe { get; set; }

        [Column(name: "auditor_id")]
        public long AuditorId { get; set; }

        [ForeignKey(nameof(AuditorId))]
        public virtual Employee? Auditor { get; set; }

        [Column(name: "project_id")]
        public long? ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        public virtual Project? Project { get; set; }

        [Column(name: "shift_id")]
        public long? ShiftId { get; set; }
        [ForeignKey(nameof(ShiftId))]
        public virtual Shift? Shift { get; set; }


        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
