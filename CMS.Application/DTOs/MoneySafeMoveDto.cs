using System.Text.Json.Serialization;

namespace CMS.Application.DTOs
{
    // one movement in/out of a money safe, whatever table it came from
    public class MoneySafeMoveDto
    {
        // customer_payment | customer_advance | customer_refund | supplier_payment | supplier_refund |
        // expense | payroll | loan | transfer_in | transfer_out
        [JsonPropertyName("kind")] public string Kind { get; set; } = string.Empty;
        [JsonPropertyName("id")] public long Id { get; set; }                 // id in the source table (see kind)
        [JsonPropertyName("date")] public DateTime Date { get; set; }
        [JsonPropertyName("party")] public string Party { get; set; } = string.Empty;   // customer / supplier / expense type / employee / other safe
        [JsonPropertyName("notes")] public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("in")] public decimal In { get; set; }
        [JsonPropertyName("out")] public decimal Out { get; set; }
        [JsonPropertyName("in_currency")] public decimal InCurrency { get; set; }
        [JsonPropertyName("out_currency")] public decimal OutCurrency { get; set; }

        // safe balance right after this movement (newest first, so it counts down the list)
        [JsonPropertyName("balance")] public decimal Balance { get; set; }
        [JsonPropertyName("balance_currency")] public decimal BalanceCurrency { get; set; }
    }

    public class MoneySafeMoveFilterDto : PagedFilterDto
    {
        public long? MoneySafeId { get; set; }     // omitted => the first safe the user can see
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }

    public class MoneySafeMovesSummaryDto
    {
        [JsonPropertyName("money_safe_id")] public long MoneySafeId { get; set; }
        [JsonPropertyName("money_safe_name")] public string MoneySafeName { get; set; } = string.Empty;
        [JsonPropertyName("is_multi_currency")] public bool IsMultiCurrency { get; set; }

        // balance just before DateFrom
        [JsonPropertyName("opening_balance")] public decimal OpeningBalance { get; set; }
        [JsonPropertyName("opening_balance_currency")] public decimal OpeningBalanceCurrency { get; set; }

        [JsonPropertyName("total_in")] public decimal TotalIn { get; set; }
        [JsonPropertyName("total_out")] public decimal TotalOut { get; set; }
        [JsonPropertyName("total_in_currency")] public decimal TotalInCurrency { get; set; }
        [JsonPropertyName("total_out_currency")] public decimal TotalOutCurrency { get; set; }

        // balance at the end of DateTo
        [JsonPropertyName("closing_balance")] public decimal ClosingBalance { get; set; }
        [JsonPropertyName("closing_balance_currency")] public decimal ClosingBalanceCurrency { get; set; }

        // balance right now, ignoring the date filter
        [JsonPropertyName("current_balance")] public decimal CurrentBalance { get; set; }
        [JsonPropertyName("current_balance_currency")] public decimal CurrentBalanceCurrency { get; set; }
    }
}
