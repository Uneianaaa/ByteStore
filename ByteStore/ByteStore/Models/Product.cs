using System;
// ↑ DateTime.

namespace ByteStore.Models
{
    // ─────────────────────────────────────────────────────────
    // КЛАСС Product — модель товара для WPF
    // ─────────────────────────────────────────────────────────
    // Повторяет структуру JSON, который возвращает API.
    // Используется для десериализации ответа GET /api/products.

    public class Product
    {
        public int Id { get; set; }
        public string Article { get; set; }
        public string Name { get; set; }
        public int CategoryId { get; set; }
        public int? ManufacturerId { get; set; }
        public int? SupplierId { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public int Quantity { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public int? WarrantyMonths { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}