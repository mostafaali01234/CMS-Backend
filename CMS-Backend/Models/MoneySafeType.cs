using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS_Backend.Models
{
    [Table("money_safe_type")]
    public class MoneySafeType
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }
        [Column(name: "cod")]
        public long Cod { get; set; }
        [Column(name: "name")]
        public string Name { get; set; } = string.Empty;
    }
}
