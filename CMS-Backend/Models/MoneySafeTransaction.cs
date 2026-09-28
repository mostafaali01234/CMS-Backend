using CMS_Backend.Models.DTOs;
using CMS_Backend.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS_Backend.Models
{
    [Table("money_safe_transaction")]
    public class MoneySafeTransaction : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "transaction_date")]
        public DateTime TransactionDate { get; set; }

        [Column(name: "transaction_notes")]
        public string TransactionNotes { get; set; } = string.Empty;

        [Column(name: "transaction_amount")]
        public decimal TransactionAmount { get; set; }

        [Column(name: "transaction_amount_currency")]
        public decimal TransactionAmountCurrency { get; set; } = decimal.Zero;

        [Column(name: "transaction_type")]
        public MoneySafeTransactionType TransactionType { get; set; } = MoneySafeTransactionType.تحويل;

        [Column(name: "auditor_id")]
        public long AuditorId { get; set; }
        [ForeignKey(nameof(AuditorId))]
        public virtual Employee? Auditor { get; set; }

        [Column(name: "out_money_safe_id")]
        public long OutMoneySafeId { get; set; }
        [ForeignKey(nameof(OutMoneySafeId))]
        public virtual MoneySafe? OutMoneySafe { get; set; }

        [Column(name: "in_money_safe_id")]
        public long InMoneySafeId { get; set; }
        [ForeignKey(nameof(InMoneySafeId))]
        public virtual MoneySafe? InMoneySafe { get; set; }

        [Column(name: "project_id")]
        public long? ProjectId { get; set; }
        [ForeignKey(nameof(ProjectId))]
        public virtual Project? Project { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
