using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ByteStoreAPI.Models
{
    [Table("A_Users")]
    public class User
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Email")]
        public string Email { get; set; } = null!;

        [Column("Password")]
        public string Password { get; set; } = null!;

        [Column("LastName")]
        public string LastName { get; set; } = null!;

        [Column("FirstName")]

        public string FirstName { get; set; } = null!;

        [Column("MiddleName")]
        public string? MiddleName { get; set; }

        [Column("Phone")]
        public string? Phone { get; set; }

        [Column("Address")]
        public string? Address { get; set; }

        [Column("Role")]
        public string Role { get; set; } = null!;

        [Column("IsVerified")]
        public bool IsVerified { get; set; }

        [Column("CreatedAt")]
        public  DateTime CreatedAt { get; set; }

        [Column("PhotoPath")]
        public string? PhotoPath { get; set; }

    }

}