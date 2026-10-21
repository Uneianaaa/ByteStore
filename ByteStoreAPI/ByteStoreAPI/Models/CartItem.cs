using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ByteStoreAPI.Models
{
    [Table("A_CartItems")]
    public class CartItem
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("CartId")]
        public int CartId { get; set; }

        [Column("ProductId")]
        public int ProductId { get; set; }

        [Column("Quantity")]
        public int Quantity { get; set; }
    }
}
