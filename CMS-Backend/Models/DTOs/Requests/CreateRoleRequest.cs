using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CMS_Backend.Models.DTOs.Requests
{
    public class CreateRoleRequest
    {
        [Required, StringLength(256)]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

}
