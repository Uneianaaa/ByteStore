using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ByteStoreAPI.Models
{
    [Table("A_Messages")]
    public class Message
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("SenderId")]
        public int SenderId { get; set; }

        [Column("ReceiverId")]
        public int ReceiverId { get; set; }

        [Column("Text")]
        public string Text { get; set; } = null!;

        [Column("SentAt")]
        public DateTime SentAt { get; set; }

        [Column("IsRead")]
        public bool IsRead { get; set; }
    }
}