using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CMS_Backend.Models.DTOs.Requests
{
    public class UserRoleRequest
    {
        [Required, EmailAddress]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(256)]
        [JsonPropertyName("role_name")]
        public string RoleName { get; set; } = string.Empty;
    }
}
