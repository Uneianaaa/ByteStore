using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ByteStoreAPI.Models
{
    [Table("A_Categories")]
    public class Category
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Name")]
        public string Name { get; set; } = null!;

        [Column("Description")]
        public string? Description { get; set; }
    }
}
