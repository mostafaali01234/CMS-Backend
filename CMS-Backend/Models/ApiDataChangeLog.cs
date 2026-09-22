using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS_Backend.Models
{
    [Table("api_data_change_log")]
    public class ApiDataChangeLog
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("log_id")]
        public long? LogId { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("table_name")]
        public string TableName { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("primary_key_value")]
        public string PrimaryKeyValue { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("field_name")]
        public string FieldName { get; set; }

        [Column("old_value")]
        public string? OldValue { get; set; }   // NVARCHAR(MAX)

        [Column("new_value")]
        public string NewValue { get; set; }   // NVARCHAR(MAX)

        [Column("changed_date")]
        public DateTime ChangedDate { get; set; }

        // Navigation property
        [ForeignKey("LogId")]
        public virtual ApiActivityLog ApiActivityLog { get; set; }
    }
}
