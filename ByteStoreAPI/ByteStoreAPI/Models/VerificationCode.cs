using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ByteStoreAPI.Models
{
    [Table("A_VerificationCodes")]
    public class VerificationCode
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("UserId")]
        public int UserId { get; set; }

        [Column("Code")]
        public string Code { get; set; } = null!;

        [Column("Purpose")]
        public string Purpose { get; set; } = null!;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        [Column("ExpiresAt")]
        public DateTime ExpiresAt { get; set; }

        [Column("IsUsed")]
        public bool IsUsed { get; set; }
    }
}
