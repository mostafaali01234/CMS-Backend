using System.Text.Json.Serialization;

namespace CMS.Application.DTOs;

public enum AuditOperation { Add, Update, Delete }

// bound from the query string; every filter is optional
public class AuditLogFilterDto : PagedFilterDto     // Page / PageSize, like the other lists
{
    public DateTime? DateFrom { get; set; }            // local days, both ends included
    public DateTime? DateTo { get; set; }
    public long? EmployeeId { get; set; }              // the employee whose user made the change
    public AuditOperation? Operation { get; set; }     // Add | Update | Delete
}

public class AuditLogChangeDto
{
    [JsonPropertyName("field")] public string Field { get; set; } = string.Empty;
    [JsonPropertyName("old_value")] public string? OldValue { get; set; }
    [JsonPropertyName("new_value")] public string? NewValue { get; set; }
}

// totals for the whole filter (not just the page)
public class AuditLogSummaryDto
{
    [JsonPropertyName("add")] public int Add { get; set; }
    [JsonPropertyName("update")] public int Update { get; set; }
    [JsonPropertyName("delete")] public int Delete { get; set; }
}

// one row = one record that was added / updated / deleted by one request
public class AuditLogRowDto
{
    [JsonPropertyName("changed_at")] public DateTime ChangedAt { get; set; }
    [JsonPropertyName("user_name")] public string UserName { get; set; } = string.Empty;
    [JsonPropertyName("operation")] public AuditOperation Operation { get; set; }
    [JsonPropertyName("table_name")] public string TableName { get; set; } = string.Empty;
    [JsonPropertyName("record_id")] public string RecordId { get; set; } = string.Empty;
    [JsonPropertyName("http_method")] public string HttpMethod { get; set; } = string.Empty;
    [JsonPropertyName("endpoint")] public string Endpoint { get; set; } = string.Empty;
    [JsonPropertyName("changes")] public List<AuditLogChangeDto> Changes { get; set; } = new();
}