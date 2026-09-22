using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS_Backend.Models.DTOs
{
    public class EmployeeDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("job_id")]
        public long JobId { get; set; }
        [JsonPropertyName("job_name")]
        public string JobName { get; set; } = string.Empty;

        [JsonPropertyName("department_id")]
        public long DepartmentId { get; set; }
        [JsonPropertyName("Department_name")]
        public string departmentName { get; set; } = string.Empty;

        [JsonPropertyName("salary")]
        public decimal Salary { get; set; }

        [JsonPropertyName("opening_balance")]
        public decimal OpeningBalance { get; set; }

        [JsonPropertyName("hire_date")]
        public DateTime HireDate { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("user_email")]
        public string UserEmail { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("birth_date")]
        public DateTime? BirthDate { get; set; }

        [JsonPropertyName("identification_number")]
        public string IdentificationNumber { get; set; } = string.Empty;

        [JsonPropertyName("city_id")]
        public long CityId { get; set; }
        [JsonPropertyName("city_name")]
        public string CityName { get; set; } = string.Empty;

        [JsonPropertyName("additional_address")]
        public string AdditionalAddress { get; set; } = string.Empty;

        [JsonPropertyName("military_status")]
        public MilitaryStatus MilitaryStatus { get; set; }

        [JsonPropertyName("martial_status")]
        public MartialStatus MartialStatus { get; set; }

        [JsonPropertyName("kids_count")]
        public int? KidsCount { get; set; }

        [JsonPropertyName("personal_phone")]
        public string PersonalPhone { get; set; } = string.Empty;

        [JsonPropertyName("work_phone")]
        public string WorkPhone { get; set; } = string.Empty;
            
        [JsonPropertyName("education")]
        public string Education { get; set; } = string.Empty;

        [JsonPropertyName("education_spec")]
        public string EducationSpec { get; set; } = string.Empty;




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
