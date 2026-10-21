using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ByteStoreAPI.Models
{
    [Table("A_Products")]
    public class Product
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Article")]
        public string Article { get; set; } = null!;

        [Column("Name")]
        public string Name { get; set; } = null!;

        [Column("CategoryId")]
        public int CategoryId { get; set; }

        [Column("ManufacturerId")]
        public int? ManufacturerId { get; set; }

        [Column("SupplierId")]
        public int? SupplierId { get; set; }

        [Column("PurchasePrice")]
        public decimal PurchasePrice { get; set; }

        [Column("SalePrice")]
        public decimal SalePrice { get; set; }

        [Column("Quantity")]
        public int Quantity { get; set; }

        [Column("Description")]
        public string? Description { get; set; }

        [Column("Color")]
        public string? Color { get; set; }

        [Column("WarrantyMonths")]
        public int? WarrantyMonths { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        [Column("PhotoPath")]
        public string? PhotoPath { get; set; }
    }
}