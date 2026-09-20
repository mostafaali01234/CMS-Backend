using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS_Backend.Models
{
    [Table("ApiActivityLog")]
    public class ApiActivityLog
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }

        [Column(name: "user_id")]
        public string UserId { get; set; }

        [Required]
        [MaxLength(500)]
        [Column(name: "endpoint")]
        public string Endpoint { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(name: "http_method")]
        public string HttpMethod { get; set; }

        [Column(name: "request_body")]
        public string RequestBody { get; set; }   // NVARCHAR(MAX)

        [Column(name: "response_body")]
        public string ResponseBody { get; set; }  // NVARCHAR(MAX)

        [Column(name: "status_code")]
        public int StatusCode { get; set; }

        [Column(name: "execution_time_ms")]
        public int ExecutionTimeMs { get; set; }

        [MaxLength(100)]
        [Column(name: "ip_address")]
        public string IPAddress { get; set; }

        [MaxLength(500)]
        [Column(name: "user_agent")]
        public string UserAgent { get; set; }

        [Column(name: "created_date")]
        public DateTime CreatedDate { get; set; }
    }
}
