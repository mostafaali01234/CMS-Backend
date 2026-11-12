using CMS.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Domain.Models
{
    /// <summary>
    /// One row per person who must be told about something (so 3 recipients = 3 rows).
    /// "Read" is just ReadAtUtc being filled in. Intentionally NOT IAuditable / not audited:
    /// it is high-volume, system-generated data.
    /// </summary>
    [Table("notification")]
    public class Notification
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        // Identity user id (the "UserId" claim in the JWT), not an Employee id
        [Column(name: "recipient_user_id")]
        public string RecipientUserId { get; set; } = string.Empty;

        [Column(name: "type")]
        public NotificationType Type { get; set; }

        [Column(name: "title")]
        public string Title { get; set; } = string.Empty;

        [Column(name: "body")]
        public string Body { get; set; } = string.Empty;

        // what it is about, so the UI can open it: "order" + order id, "message" + message id
        [Column(name: "entity_type")]
        public string? EntityType { get; set; }

        [Column(name: "entity_id")]
        public long? EntityId { get; set; }

        [Column(name: "created_at_utc")]
        public DateTime CreatedAtUtc { get; set; }

        // null = unread
        [Column(name: "read_at_utc")]
        public DateTime? ReadAtUtc { get; set; }
    }
}
