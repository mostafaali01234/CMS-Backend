using CMS.Domain.Enums;
using CMS.Domain.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CMS.Application.DTOs
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

    public class EmployeeSaveDto
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("job_id")] public long JobId { get; set; }
        [JsonPropertyName("department_id")] public long? DepartmentId { get; set; }
        [JsonPropertyName("city_id")] public long CityId { get; set; }
        [JsonPropertyName("salary")] public decimal Salary { get; set; }
        [JsonPropertyName("opening_balance")] public decimal OpeningBalance { get; set; }
        [JsonPropertyName("hire_date")] public DateTime HireDate { get; set; }
        [JsonPropertyName("birth_date")] public DateTime? BirthDate { get; set; }
        [JsonPropertyName("active")] public bool Active { get; set; }
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
        [JsonPropertyName("identification_number")] public string? IdentificationNumber { get; set; }
        [JsonPropertyName("personal_phone")] public string? PersonalPhone { get; set; }
        [JsonPropertyName("work_phone")] public string? WorkPhone { get; set; }
        [JsonPropertyName("military_status")] public MilitaryStatus MilitaryStatus { get; set; }
        [JsonPropertyName("martial_status")] public MartialStatus MartialStatus { get; set; }
        [JsonPropertyName("kids_count")] public int? KidsCount { get; set; }
        [JsonPropertyName("education")] public string? Education { get; set; }
        [JsonPropertyName("education_spec")] public string? EducationSpec { get; set; }
        [JsonPropertyName("additional_address")] public string? AdditionalAddress { get; set; }
        [JsonPropertyName("notes")] public string? Notes { get; set; }

        public Employee ToEntity() => new()
        {
            Id = Id,
            Name = Name,
            JobId = JobId,
            DepartmentId = DepartmentId is > 0 ? DepartmentId : null,   // 0 means "no department"
            CityId = CityId,
            Salary = Salary,
            OpeningBalance = OpeningBalance,
            HireDate = HireDate,
            BirthDate = BirthDate,
            Active = Active,
            UserId = string.IsNullOrWhiteSpace(UserId) ? null! : UserId,
            IdentificationNumber = IdentificationNumber ?? "",
            PersonalPhone = PersonalPhone ?? "",
            WorkPhone = WorkPhone ?? "",
            MilitaryStatus = MilitaryStatus,
            MartialStatus = MartialStatus,
            KidsCount = KidsCount,
            Education = Education ?? "",
            EducationSpec = EducationSpec ?? "",
            AdditionalAddress = AdditionalAddress ?? "",
            Notes = Notes ?? ""
        };
    }

    public class EmployeeFilterDto : PagedFilterDto
    {
        public string? Search { get; set; }        // name or phone
        public long? DepartmentId { get; set; }
        public long? JobId { get; set; }
        public bool? Active { get; set; }          // null = all
    }

    public class EmployeeListItemDto
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("phone")] public string Phone { get; set; } = "";
        [JsonPropertyName("department_name")] public string DepartmentName { get; set; } = "";
        [JsonPropertyName("manager_name")] public string ManagerName { get; set; } = "";
        [JsonPropertyName("salary")] public decimal Salary { get; set; }
        [JsonPropertyName("hire_date")] public DateTime HireDate { get; set; }
        [JsonPropertyName("job_name")] public string JobName { get; set; } = "";
        [JsonPropertyName("active")] public bool Active { get; set; }
    }
}
