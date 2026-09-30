using CMS.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Domain.Models
{
    [Table("product_unit")]
    public class ProductUnit 
    {
        [Key]
        [Column(name: "id")]
        public long Id { get; set; }
        [Column(name: "name")]
        public string Name { get; set; } = string.Empty;
    }
}
