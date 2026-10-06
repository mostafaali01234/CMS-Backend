using CMS.Domain.Enums;
using CMS.Domain.Common;
//using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Domain.Models
{
    [Table("order")]
    public class Order : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "installation_date")]
        public DateTime InstallationDate { get; set; }

        [Column(name: "status")]
        public OrderStatus Status { get; set; }

        [Column(name: "source")]
        public OrderSource Source { get; set; }

        [Column(name: "type")]
        public OrderType Type { get; set; }

        [Column(name: "category")]
        public OrderCategory Category { get; set; }

        //[Precision(18, 2)]
        [Column(name: "total")]
        public decimal Total { get; set; }

        //[Precision(18, 2)]
        [Column(name: "discount")]
        public decimal Discount { get; set; }

        //[Precision(18, 2)]
        [Column(name: "net_total")]
        public decimal NetTotal { get; set; }

        [Column(name: "shipping")]
        public bool Shipping { get; set; } = false;

        [Column(name: "customer_id")]
        public long CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public virtual Customer? Customer { get; set; }

        [Column(name: "city_id")]
        public long CityId { get; set; }
        [ForeignKey(nameof(CityId))]
        public virtual City? City { get; set; }

        [Column(name: "line_id")]
        public long LineId { get; set; }
        [ForeignKey(nameof(LineId))]
        public virtual OrderLine? line { get; set; }

        [Column(name: "location_notes")]
        public string LocationNotes { get; set; } = string.Empty;

        [Column(name: "seller_id")]
        public long SellerId { get; set; }
        [ForeignKey(nameof(SellerId))]
        public virtual Employee? Seller { get; set; }

        [Column(name: "auditor_id")]
        public long AuditorId { get; set; }
        [ForeignKey(nameof(AuditorId))]
        public virtual Employee? Auditor { get; set; }

        [Column(name: "attachment_image")]
        public string AttachmentImage { get; set; } = string.Empty;

        [Column(name: "old_order_id")]
        public long? OldOrderId { get; set; }
        [ForeignKey(nameof(OldOrderId))]
        public virtual Order? OldOrder { get; set; }

        public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public virtual ICollection<OrderNoteHistory> Notes { get; set; } = new List<OrderNoteHistory>();
        public virtual ICollection<OrderTechHistory> TechHistory { get; set; } = new List<OrderTechHistory>();

        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
