using CMS_Backend.Models.DTOs;
using CMS_Backend.Models.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS_Backend.Models
{
    [Table("employee")]
    public class Employee : IAuditable, ISoftDeletable
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }
        [Column(name: "name")]
        public string Name { get; set; } = string.Empty;

        [Column(name: "job_id")]
        public long JobId { get; set; }

        [ForeignKey(nameof(JobId))]
        public virtual Job? Job { get; set; }

        [Column(name: "department_id")]
        public long? DepartmentId { get; set; }

        [ForeignKey(nameof(DepartmentId))]
        public virtual Department? Department { get; set; }

        [Column(name: "salary")]
        public decimal Salary { get; set; }

        [Column(name: "opening_balance")]
        public decimal OpeningBalance { get; set; }

        [Column(name: "hire_date")]
        public DateTime HireDate { get; set; }

        [Column(name: "active")]
        public bool Active { get; set; }

        [Column(name: "user_id")]
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual IdentityUser? UserAccount { get; set; }

        [Column(name: "notes")]
        public string Notes { get; set; } = string.Empty;

        [Column(name: "birth_date")]
        public DateTime? BirthDate { get; set; }

        [Column(name: "identification_number")]
        public string IdentificationNumber { get; set; } = string.Empty;

        //[Column(name: "state_id")]
        //public long StateId { get; set; }

        //[ForeignKey(nameof(StateId))]
        //public virtual CountryState? StateAddress { get; set; }

        [Column(name: "city_id")]
        public long CityId { get; set; }

        [ForeignKey(nameof(CityId))]
        public virtual City? CityAddress { get; set; }

        [Column(name: "additional_address")]
        public string AdditionalAddress { get; set; } = string.Empty;

        [Column(name: "military_status")]
        public MilitaryStatus MilitaryStatus { get; set; }

        [Column(name: "martial_status")]
        public MartialStatus MartialStatus { get; set; }

        [Column(name: "kids_count")]
        public int? KidsCount { get; set; }

        [Column(name: "personal_phone")]
        public string PersonalPhone { get; set; } = string.Empty;

        [Column(name: "work_phone")]
        public string WorkPhone { get; set; } = string.Empty;

        [Column(name: "education")]
        public string Education { get; set; } = string.Empty;

        [Column(name: "education_spec")]
        public string EducationSpec { get; set; } = string.Empty;



        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
