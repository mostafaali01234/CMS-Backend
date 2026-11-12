using CMS.Domain.Enums;
using System.Text.Json.Serialization;

namespace CMS.Application.DTOs
{
    public class NotificationDto
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("type")] public NotificationType Type { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
        [JsonPropertyName("body")] public string Body { get; set; } = string.Empty;
        [JsonPropertyName("entity_type")] public string? EntityType { get; set; }   // "order" | "message"
        [JsonPropertyName("entity_id")] public long? EntityId { get; set; }
        [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }    // UTC (ends with Z)
        [JsonPropertyName("read_at")] public DateTime? ReadAt { get; set; }
        [JsonPropertyName("is_read")] public bool IsRead { get; set; }
    }

    public class NotificationFilterDto : PagedFilterDto
    {
        public bool UnreadOnly { get; set; }
    }

    // ---- messages ----
    public class SendMessageDto
    {
        [JsonPropertyName("recipient_user_id")] public string RecipientUserId { get; set; } = string.Empty;
        [JsonPropertyName("body")] public string Body { get; set; } = string.Empty;
        [JsonPropertyName("order_id")] public long? OrderId { get; set; }
    }

    public class UserMessageDto
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("sender_user_id")] public string SenderUserId { get; set; } = string.Empty;
        [JsonPropertyName("sender_name")] public string SenderName { get; set; } = string.Empty;
        [JsonPropertyName("recipient_user_id")] public string RecipientUserId { get; set; } = string.Empty;
        [JsonPropertyName("order_id")] public long? OrderId { get; set; }
        [JsonPropertyName("body")] public string Body { get; set; } = string.Empty;
        [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
        [JsonPropertyName("read_at")] public DateTime? ReadAt { get; set; }
        [JsonPropertyName("is_mine")] public bool IsMine { get; set; }              // filled for the caller
    }

    public class MessageThreadFilterDto : PagedFilterDto
    {
        public string WithUserId { get; set; } = string.Empty;
        public long? OrderId { get; set; }
    }

    // one row of the "chats" list: the other person + the last message + how many you have not read
    public class ConversationDto
    {
        [JsonPropertyName("user_id")] public string UserId { get; set; } = string.Empty;
        [JsonPropertyName("user_name")] public string UserName { get; set; } = string.Empty;
        [JsonPropertyName("last_message")] public UserMessageDto? LastMessage { get; set; }
        [JsonPropertyName("unread_count")] public int UnreadCount { get; set; }
    }

    // someone you can start a chat with
    public class ContactDto
    {
        [JsonPropertyName("user_id")] public string UserId { get; set; } = string.Empty;
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    }
}
