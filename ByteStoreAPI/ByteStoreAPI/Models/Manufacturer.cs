using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ByteStoreAPI.Models
{
    [Table("A_Manufacturers")]
    public class Manufacturer
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("CompanyName")]
        public string CompanyName { get; set; } = null!;

        [Column("Country")]
        public string? Country { get; set; }
    }
}
