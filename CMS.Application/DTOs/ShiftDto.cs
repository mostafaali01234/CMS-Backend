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

        [JsonPropertyName("money_safe_id")] 
        public long? MoneySafeId { get; set; }   // primary tech's safe

        [JsonPropertyName("custody")] 
        public decimal Custody { get; set; }           // into the tech's safe

        [JsonPropertyName("delivered")] 
        public decimal Delivered { get; set; }       // out of the tech's safe


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

    public class ShiftBoardDto
    {
        [JsonPropertyName("shift")] public ShiftDto? Shift { get; set; }          // null: nothing found, the page offers "add"
        [JsonPropertyName("items")] public List<ShiftItemRowDto> Items { get; set; } = new();
        [JsonPropertyName("primary_tech_name")] public string? PrimaryTechName { get; set; }
        [JsonPropertyName("money_safe_id")] public long? MoneySafeId { get; set; }       // the primary tech's safe
        [JsonPropertyName("money_safe_name")] public string? MoneySafeName { get; set; }
        [JsonPropertyName("open_orders")] public List<OpenOrderDto> OpenOrders { get; set; } = new();
    }

    public class OpenOrderDto
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("customer_name")] public string CustomerName { get; set; } = "";
        [JsonPropertyName("net_total")] public decimal NetTotal { get; set; }
    }

    public class ShiftItemRowDto
    {
        [JsonPropertyName("product_id")] public long ProductId { get; set; }
        [JsonPropertyName("product_name")] public string ProductName { get; set; } = "";
        [JsonPropertyName("opening")] public decimal Opening { get; set; }          // رصيد اول المدة
        [JsonPropertyName("transfer_in")] public decimal TransferIn { get; set; }   // تحويل (وارد)
        [JsonPropertyName("total")] public decimal Total { get; set; }              // اجمالي = opening + transfer_in
        [JsonPropertyName("ordered_by_order")] public Dictionary<long, decimal> OrderedByOrder { get; set; } = new();
        [JsonPropertyName("sold_by_invoice")] public Dictionary<long, decimal> SoldByInvoice { get; set; } = new();
        [JsonPropertyName("consumed")] public decimal Consumed { get; set; }        // اجمالي المستهلك = the shift invoices
        [JsonPropertyName("transfer_out")] public decimal TransferOut { get; set; } // تحويل (صادر)
        [JsonPropertyName("other_change")] public decimal OtherChange { get; set; } // anything else that day in this store
        [JsonPropertyName("remaining")] public decimal Remaining { get; set; }      // المتبقي
    }

    public class ShiftTransferRequestDto
    {
        [JsonPropertyName("money_safe_id")] public long MoneySafeId { get; set; } 
        [JsonPropertyName("amount")] public decimal Amount { get; set; }
        [JsonPropertyName("notes")] public string Notes { get; set; } = "";
        [JsonPropertyName("auditor_id")] public long AuditorId { get; set; }
    }
}