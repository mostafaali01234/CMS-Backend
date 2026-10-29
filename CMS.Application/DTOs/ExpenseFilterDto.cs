// Models/DTOs/ExpenseDto.cs
using CMS.Application.DTOs;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS.Domain.DTOs
{
    public class ExpenseFilterDto : PagedFilterDto
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public long? CategoryId { get; set; }
        public long? ExpenseTypeId { get; set; }
        public string? Search { get; set; }
    }

    public class ExpenseListSummaryDto
    {
        [JsonPropertyName("amount")] public decimal Amount { get; set; }
        [JsonPropertyName("amount_currency")] public decimal AmountCurrency { get; set; }
    }
}