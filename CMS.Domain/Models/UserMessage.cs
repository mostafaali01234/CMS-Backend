using CMS.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Domain.Models
{
    /// <summary>A direct message between two users, optionally about an order. Not audited (high volume).</summary>
    [Table("user_message")]
    public class UserMessage : ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "sender_user_id")]
        public string SenderUserId { get; set; } = string.Empty;

        [Column(name: "recipient_user_id")]
        public string RecipientUserId { get; set; } = string.Empty;

        [Column(name: "order_id")]
        public long? OrderId { get; set; }
        [ForeignKey(nameof(OrderId))]
        public virtual Order? Order { get; set; }

        [Column(name: "body")]
        public string Body { get; set; } = string.Empty;

        [Column(name: "created_at_utc")]
        public DateTime CreatedAtUtc { get; set; }

        [Column(name: "read_at_utc")]
        public DateTime? ReadAtUtc { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
