using System.Text.Json.Serialization;

namespace CMS.Application.DTOs;

public class PartyBalanceDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("phone")] public string Phone { get; set; } = string.Empty;
    [JsonPropertyName("opening_balance")] public decimal OpeningBalance { get; set; }
    [JsonPropertyName("invoices_total")] public decimal InvoicesTotal { get; set; }
    [JsonPropertyName("returns_total")] public decimal ReturnsTotal { get; set; }
    [JsonPropertyName("payments_total")] public decimal PaymentsTotal { get; set; }
    [JsonPropertyName("refunds_total")] public decimal RefundsTotal { get; set; }
    [JsonPropertyName("balance")] public decimal Balance { get; set; }
    [JsonPropertyName("invoices_total_currency")] public decimal InvoicesTotalCurrency { get; set; }
    [JsonPropertyName("returns_total_currency")] public decimal ReturnsTotalCurrency { get; set; }
    [JsonPropertyName("payments_total_currency")] public decimal PaymentsTotalCurrency { get; set; }
    [JsonPropertyName("refunds_total_currency")] public decimal RefundsTotalCurrency { get; set; }
    [JsonPropertyName("balance_currency")] public decimal BalanceCurrency { get; set; }
}

public class StatementLineDto
{
    [JsonPropertyName("date")] public DateTime Date { get; set; }
    [JsonPropertyName("kind")] public string Kind { get; set; } = string.Empty;   // invoice | return | payment | advance | refund
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("number")] public long? Number { get; set; }                // invoice number, for invoice and return lines
    [JsonPropertyName("ref_id")] public long? RefId { get; set; }                 // the invoice a payment is tied to
    [JsonPropertyName("ref_number")] public long? RefNumber { get; set; }
    [JsonPropertyName("order_id")] public long? OrderId { get; set; }
    [JsonPropertyName("notes")] public string Notes { get; set; } = string.Empty;
    [JsonPropertyName("increase")] public decimal Increase { get; set; }
    [JsonPropertyName("decrease")] public decimal Decrease { get; set; }
    [JsonPropertyName("increase_currency")] public decimal IncreaseCurrency { get; set; }
    [JsonPropertyName("decrease_currency")] public decimal DecreaseCurrency { get; set; }
    [JsonPropertyName("balance")] public decimal Balance { get; set; }
    [JsonPropertyName("balance_currency")] public decimal BalanceCurrency { get; set; }
}

public class StatementDto
{
    [JsonPropertyName("party_id")] public long PartyId { get; set; }
    [JsonPropertyName("party_name")] public string PartyName { get; set; } = string.Empty;
    [JsonPropertyName("party_phone")] public string PartyPhone { get; set; } = string.Empty;
    [JsonPropertyName("opening_balance")] public decimal OpeningBalance { get; set; }            // balance before the first line shown
    [JsonPropertyName("opening_balance_currency")] public decimal OpeningBalanceCurrency { get; set; }
    [JsonPropertyName("closing_balance")] public decimal ClosingBalance { get; set; }
    [JsonPropertyName("closing_balance_currency")] public decimal ClosingBalanceCurrency { get; set; }
    [JsonPropertyName("lines")] public List<StatementLineDto> Lines { get; set; } = new();
}
