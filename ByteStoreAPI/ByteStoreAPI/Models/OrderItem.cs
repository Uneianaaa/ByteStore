using ByteStoreAPI.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ByteStoreAPI.Models
{
    [Table("A_OrderItems")]
    public class OrderItem
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("OrderId")]
        public int OrderId { get; set; }

        [Column("ProductId")]
        public int ProductId { get; set; }

        [Column("Quantity")]
        public int Quantity { get; set; }

        [Column("Price")]
        public decimal Price { get; set; }

        [JsonIgnore]
        public Order? Order { get; set; }
    }
}