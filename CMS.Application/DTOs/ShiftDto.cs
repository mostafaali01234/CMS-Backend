// Models/DTOs/ShiftDto.cs
using System.Text.Json.Serialization;
using CMS.Application.DTOs;
using CMS.Domain.Enums;

namespace CMS.Domain.DTOs
{
    public class ShiftDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("country_state_id")]
        public long CountryStateId { get; set; }

        [JsonPropertyName("country_state_name")]
        public string CountryStateName { get; set; } = string.Empty;

        [JsonPropertyName("car_id")]
        public long CarId { get; set; }

        [JsonPropertyName("car_name")]
        public string CarName { get; set; } = string.Empty;

        [JsonPropertyName("store_id")]
        public long StoreId { get; set; }

        [JsonPropertyName("store_name")]
        public string StoreName { get; set; } = string.Empty;

        [JsonPropertyName("km_start")]
        public decimal KmStart { get; set; }

        [JsonPropertyName("km_end")]
        public decimal KmEnd { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("project_id")]
        public long? ProjectId { get; set; }

        [JsonPropertyName("project_name")]
        public string ProjectName { get; set; } = string.Empty;

        [JsonPropertyName("techs")]
        public List<ShiftTechDto> Techs { get; set; } = new();

        [JsonPropertyName("invoices")]
        public List<SaleInvoiceDto> Invoices { get; set; } = new();

        [JsonPropertyName("expenses")]
        public List<ExpenseDto> Expenses { get; set; } = new();

        [JsonPropertyName("loans")]
        public List<EmployeeLoanDto> Loans { get; set; } = new();

        [JsonPropertyName("transactions")]
        public List<MoneySafeTransactionDto> Transactions { get; set; } = new();

        [JsonPropertyName("created_at_utc")]
        public DateTime? CreatedAtUtc { get; set; }

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updated_at_utc")]
        public DateTime? UpdatedAtUtc { get; set; }

        [JsonPropertyName("updated_by")]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class ShiftTechDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("tech_id")]
        public long TechId { get; set; }

        [JsonPropertyName("tech_name")]
        public string TechName { get; set; } = string.Empty;

        [JsonPropertyName("tech_type")]
        public TechType TechType { get; set; }
    }
}