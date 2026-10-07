using CMS.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Domain.Models
{
    [Table("shift")]
    public class Shift : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }
        
        [Column(name: "date")]
        public DateTime Date { get; set; }

        [Column(name: "country_state_id")]
        public long CountryStateId { get; set; }
        [ForeignKey(nameof(CountryStateId))]
        public virtual CountryState? CountryState { get; set; }

        [Column(name: "car_id")]
        public long CarId { get; set; }
        [ForeignKey(nameof(CarId))]
        public virtual Car? Car { get; set; }

        [Column(name: "store_id")]
        public long StoreId { get; set; }
        [ForeignKey(nameof(StoreId))]
        public virtual Store? Store { get; set; }

        [Column(name: "km_start")]
        public decimal KmStart { get; set; } = 0;

        [Column(name: "km_end")]
        public decimal KmEnd { get; set; } = 0;

        [Column(name: "notes")]
        public string Notes { get; set; } = string.Empty;

        [Column(name: "project_id")]
        public long? ProjectId { get; set; }
        [ForeignKey(nameof(ProjectId))]
        public virtual Project? Project { get; set; }

        public virtual ICollection<ShiftTech> Techs { get; set; } = new List<ShiftTech>();
        public virtual ICollection<SaleInvoice> Invoices { get; set; } = new List<SaleInvoice>();
        public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
        public virtual ICollection<EmployeeLoan> Loans { get; set; } = new List<EmployeeLoan>();
        public virtual ICollection<MoneySafeTransaction> Transactions { get; set; } = new List<MoneySafeTransaction>();

        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
