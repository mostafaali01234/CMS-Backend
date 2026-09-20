using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CMS_Backend.Models.DTOs.Requests
{
    public class UserRegisterationDto
    {
        [Required]
        [EmailAddress]
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [Required]
        [JsonPropertyName("user_name")]
        public string UserName { get; set; }
        [Required]
        [JsonPropertyName("password")]
        public string Password { get; set; }
    }
}
