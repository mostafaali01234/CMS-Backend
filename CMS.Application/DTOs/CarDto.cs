// Models/DTOs/CarDto.cs
using System.Text.Json.Serialization;

namespace CMS.Application.DTOs
{
    public class CarDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("driver_id")]
        public long DriverId { get; set; }

        [JsonPropertyName("driver_name")]
        public string DriverName { get; set; } = string.Empty;

        [JsonPropertyName("plat_number")]
        public string PlatNumber { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("oil_change_rate")]
        public decimal OilChangeRate { get; set; }

        [JsonPropertyName("filter_change_rate")]
        public decimal FilterChangeRate { get; set; }

        [JsonPropertyName("sk_change_rate")]
        public decimal SkChangeRate { get; set; }

        [JsonPropertyName("tire_change_rate")]
        public decimal TireChangeRate { get; set; }

        [JsonPropertyName("license_start_date")]
        public DateTime LicenseStartDate { get; set; }

        [JsonPropertyName("license_end_date")]
        public DateTime LicenseEndDate { get; set; }

        [JsonPropertyName("insurance_start_date")]
        public DateTime InsuranceStartDate { get; set; }

        [JsonPropertyName("insurance_end_date")]
        public DateTime InsuranceEndDate { get; set; }

        [JsonPropertyName("insurance_company_name")]
        public string InsuranceCompanyName { get; set; } = string.Empty;

        [JsonPropertyName("owner_name")]
        public string OwnerName { get; set; } = string.Empty;

        [JsonPropertyName("motor_number")]
        public string MotorNumber { get; set; } = string.Empty;

        [JsonPropertyName("chassis_number")]
        public string ChassisNumber { get; set; } = string.Empty;

        [JsonPropertyName("created_at_utc")]
        public DateTime? CreatedAtUtc { get; set; }

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updated_at_utc")]
        public DateTime? UpdatedAtUtc { get; set; }

        [JsonPropertyName("updated_by")]
        public string UpdatedBy { get; set; } = string.Empty;
    }
}