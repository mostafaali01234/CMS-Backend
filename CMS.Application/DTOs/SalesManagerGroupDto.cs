// CMS.Application/DTOs/SalesManagerGroupDto.cs
using System.Text.Json.Serialization;
namespace CMS.Application.DTOs;

public class SalesManagerGroupDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("manager_id")] public long ManagerId { get; set; }
    [JsonPropertyName("manager_name")] public string ManagerName { get; set; } = "";
    [JsonPropertyName("percent")] public decimal Percent { get; set; }
    [JsonPropertyName("active")] public bool Active { get; set; } = true;
    [JsonPropertyName("notes")] public string Notes { get; set; } = "";
    [JsonPropertyName("sellers")] public List<GroupMemberDto> Sellers { get; set; } = new();
    [JsonPropertyName("products")] public List<GroupMemberDto> Products { get; set; } = new();
}

public class GroupMemberDto
{
    [JsonPropertyName("id")] public long Id { get; set; }          // employee id or product id
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}