// CMS.Application/DTOs/EmployeeBalanceDto.cs
using System.Text.Json.Serialization;
namespace CMS.Application.DTOs;

public class EmployeeBalanceDto
{
    [JsonPropertyName("employee_id")] public long EmployeeId { get; set; }
    [JsonPropertyName("employee_name")] public string EmployeeName { get; set; } = "";
    [JsonPropertyName("date_from")] public DateTime? DateFrom { get; set; }
    [JsonPropertyName("date_to")] public DateTime? DateTo { get; set; }

    [JsonPropertyName("opening_balance")] public decimal OpeningBalance { get; set; }
    [JsonPropertyName("salaries_total")] public decimal SalariesTotal { get; set; }           // approved (+)
    [JsonPropertyName("installments_total")] public decimal InstallmentsTotal { get; set; }   // (+)
    [JsonPropertyName("withdrawn_total")] public decimal WithdrawnTotal { get; set; }         // shown as (-)
    [JsonPropertyName("loans_total")] public decimal LoansTotal { get; set; }                 // shown as (-)
    [JsonPropertyName("closing_balance")] public decimal ClosingBalance { get; set; }

    [JsonPropertyName("movements")] public List<BalanceMovementDto> Movements { get; set; } = new();
}

public class BalanceMovementDto
{
    [JsonPropertyName("date")] public DateTime Date { get; set; }
    [JsonPropertyName("kind")] public string Kind { get; set; } = "";       // salary | installments | withdraw | loan
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("amount")] public decimal Amount { get; set; }        // signed
    [JsonPropertyName("balance")] public decimal Balance { get; set; }      // running balance after this movement
}