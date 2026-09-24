using CMS_Backend.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS_Backend.Models
{
    [Table("product_unit_commission")]
    public class ProductUnitCommission
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "product_id")]
        [JsonPropertyName("product_id")]
        public long ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        [Column(name: "unit_id")]
        [JsonPropertyName("unit_id")]
        public long UnitId { get; set; }

        [ForeignKey(nameof(UnitId))]
        public virtual ProductUnit? Unit { get; set; }

        [Column(name: "sales_wholesale_comm_percent")]
        [JsonPropertyName("sales_wholesale_comm_percent")]
        public decimal SalesWholesaleCommPercent { get; set; }

        [Column(name: "sales_half_wholesale_comm_percent")]
        [JsonPropertyName("sales_half_wholesale_comm_percent")]
        public decimal SalesHalfWholesaleCommPercent { get; set; }

        [Column(name: "sales_retail_comm_percent")]
        [JsonPropertyName("sales_retail_comm_percent")]
        public decimal SalesRetailCommPercent { get; set; }

        [Column(name: "tech_comm")]
        [JsonPropertyName("tech_comm")]
        public decimal TechComm { get; set; }

        [Column(name: "assistant_1_comm")]
        [JsonPropertyName("assistant_1_comm")]
        public decimal Assistant1Comm { get; set; }

        [Column(name: "assistant_2_comm")]
        [JsonPropertyName("assistant_2_comm")]
        public decimal Assistant2Comm { get; set; }

        [Column(name: "assistant_3_comm")]
        [JsonPropertyName("assistant_3_comm")]
        public decimal Assistant3Comm { get; set; }
    }
}
