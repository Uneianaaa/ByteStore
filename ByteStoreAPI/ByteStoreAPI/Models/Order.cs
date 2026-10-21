using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ByteStoreAPI.Models
{
    [Table("A_Orders")]
    public class Order
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("OrderNumber")]
        public string OrderNumber { get; set; } = null!;

        [Column("CustomerId")]
        public int CustomerId { get; set; }

        [Column("SellerId")]
        public int? SellerId { get; set; }

        [Column("OrderDate")]
        public DateTime OrderDate { get; set; }

        [Column("Status")]
        public string Status { get; set; } = null!;

        [Column("DeliveryAddress")]
        public string? DeliveryAddress { get; set; }

        [Column("TotalAmount")]
        public decimal TotalAmount { get; set; }

        [Column("Comment")]
        public string? Comment { get; set; }

        public List<OrderItem> OrderItems { get; set; } = new();
    }
}