using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CMS.Application.DTOs.Requests
{
    public class CreateRoleRequest
    {
        [Required, StringLength(256)]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

}
