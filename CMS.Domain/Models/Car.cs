using CMS.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS.Domain.Models
{
    [Table("car")]
    public class Car : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }
        [Column(name: "name")]
        public string Name { get; set; } = string.Empty;
        [Column(name: "notes")]
        public string Notes { get; set; } = string.Empty;
        [Column(name: "driver_id")]
        [JsonPropertyName("driver_id")]
        public long DriverId { get; set; }

        [ForeignKey(nameof(DriverId))]
        public virtual Employee? Driver { get; set; }
        [MaxLength(50)]
        [Column(name: "plat_number")]
        [JsonPropertyName("plat_number")]
        public string PlatNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column(name: "model")]
        public string Model { get; set; } = string.Empty;

        [Column(name: "oil_change_rate")]
        [JsonPropertyName("oil_change_rate")]
        public decimal OilChangeRate { get; set; }

        [Column(name: "filter_change_rate")]
        [JsonPropertyName("filter_change_rate")]
        public decimal FilterChangeRate { get; set; }

        [Column(name: "sk_change_rate")]
        [JsonPropertyName("sk_change_rate")]
        public decimal SkChangeRate { get; set; }

        [Column(name: "tire_change_rate")]
        [JsonPropertyName("tire_change_rate")]
        public decimal TireChangeRate { get; set; }

        [Column(name: "license_start_date")]
        [JsonPropertyName("license_start_date")]
        public DateTime LicenseStartDate { get; set; }

        [Column(name: "license_end_date")]
        [JsonPropertyName("license_end_date")]
        public DateTime LicenseEndDate { get; set; }

        [Column(name: "insurance_start_date")]
        [JsonPropertyName("insurance_start_date")]
        public DateTime InsuranceStartDate { get; set; }

        [Column(name: "insurance_end_date")]
        [JsonPropertyName("insurance_end_date")]
        public DateTime InsuranceEndDate { get; set; }

        [MaxLength(200)]

        [Column(name: "insurance_company_name")]
        [JsonPropertyName("insurance_company_name")]
        public string InsuranceCompanyName { get; set; } = string.Empty;

        [MaxLength(200)]

        [Column(name: "owner_name")]
        [JsonPropertyName("owner_name")]
        public string OwnerName { get; set; } = string.Empty;

        [MaxLength(100)]

        [Column(name: "motor_number")]
        [JsonPropertyName("motor_number")]
        public string MotorNumber { get; set; } = string.Empty;

        [MaxLength(100)]

        [Column(name: "chassis_number")]
        [JsonPropertyName("chassis_number")]
        public string ChassisNumber { get; set; } = string.Empty;


        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
