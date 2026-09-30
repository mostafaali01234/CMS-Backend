// Models/DTOs/StoreDto.cs
using System.Text.Json.Serialization;

namespace CMS.Application.DTOs
{
    public record TokenUser(string Id, string UserName, string? Email, IReadOnlyList<string> Roles);

}