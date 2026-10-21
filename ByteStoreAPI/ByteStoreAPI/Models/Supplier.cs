using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ByteStoreAPI.Models
{
    [Table("A_Suppliers")]
    public class Supplier
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("CompanyName")]
        public string CompanyName { get; set; } = null!;

        [Column("Phone")]
        public string Phone { get; set; } = null!;

        [Column("Email")]
        public string? Email { get; set; }

        [Column("Address")]
        public string? Address { get; set; }

        [Column("ContactLastName")]
        public string? ContactLastName { get; set; }

        [Column("ContactFirstName")]
        public string? ContactFirstName { get; set; }

        [Column("ContactMiddleName")]
        public string? ContactMiddleName { get; set; }
    }
}
