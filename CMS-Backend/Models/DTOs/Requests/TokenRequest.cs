using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CMS_Backend.Models.DTOs.Requests
{
    public class TokenRequest
    {
        [Required]
        [JsonPropertyName("token")]
        public string Token { get; set; }
        [Required]
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }
    }

}
