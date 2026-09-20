using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CMS_Backend.Models.DTOs.Requests
{
    public class AddUserClaimRequest
    {
        [Required, EmailAddress]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(256)]
        [JsonPropertyName("claim_type")]
        public string ClaimType { get; set; } = string.Empty;

        [Required, StringLength(1024)]
        [JsonPropertyName("claim_value")]
        public string ClaimValue { get; set; } = string.Empty;
    }
}
