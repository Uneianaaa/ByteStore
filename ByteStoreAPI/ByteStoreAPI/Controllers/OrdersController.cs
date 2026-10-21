using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ByteStoreAPI.Data;
using ByteStoreAPI.Models;

namespace ByteStoreAPI.Controllers
{
    public class OrderItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class CreateOrderRequest
    {
        public int CustomerId { get; set; }
        public int? SellerId { get; set; }
        public string DeliveryAddress { get; set; } = null!;
        public string? Comment { get; set; }
        public List<OrderItemRequest> Items { get; set; } = new();
    }

    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public OrdersController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _db.Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Собираем Id покупателей и продавцов
            var customerIds = orders.Select(o => o.CustomerId).Distinct().ToList();
            var sellerIds = orders.Where(o => o.SellerId.HasValue)
                                  .Select(o => o.SellerId!.Value)
                                  .Distinct()
                                  .ToList();

            // Загружаем ФИО одним запросом в словари
            var customers = await _db.Users
                .Where(u => customerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => $"{u.LastName} {u.FirstName}");

            var sellers = await _db.Users
                .Where(u => sellerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => $"{u.LastName} {u.FirstName}");

            var result = orders.Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.CustomerId,
                CustomerName = customers.ContainsKey(o.CustomerId)
                    ? customers[o.CustomerId]
                    : "—",
                o.SellerId,
                SellerName = o.SellerId.HasValue && sellers.ContainsKey(o.SellerId.Value)
                    ? sellers[o.SellerId.Value]
                    : "—",
                o.OrderDate,
                o.Status,
                o.DeliveryAddress,
                o.TotalAmount,
                o.Comment,
                o.OrderItems
            }).ToList();

            return Ok(result);
        }

        // Один заказ с полными деталями по позициям
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // ФИО покупателя
            string customerName = "—";
            var customer = await _db.Users.FindAsync(order.CustomerId);
            if (customer != null)
            {
                customerName = $"{customer.LastName} {customer.FirstName}";
            }

            // ФИО продавца
            string sellerName = "—";
            if (order.SellerId.HasValue)
            {
                var seller = await _db.Users.FindAsync(order.SellerId.Value);
                if (seller != null)
                {
                    sellerName = $"{seller.LastName} {seller.FirstName}";
                }
            }

            // Дополняем позиции названием товара
            var items = new List<object>();

            foreach (var item in order.OrderItems)
            {
                var product = await _db.Products.FindAsync(item.ProductId);
                string productName = product != null ? product.Name : "—";

                items.Add(new
                {
                    item.Id,
                    item.ProductId,
                    ProductName = productName,
                    item.Quantity,
                    item.Price,
                    Sum = item.Quantity * item.Price
                });
            }

            return Ok(new
            {
                order.Id,
                order.OrderNumber,
                order.CustomerId,
                CustomerName = customerName,
                order.SellerId,
                SellerName = sellerName,
                order.OrderDate,
                order.Status,
                order.DeliveryAddress,
                order.TotalAmount,
                order.Comment,
                Items = items
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
        {
            if (request.Items == null || request.Items.Count == 0)
            {
                return BadRequest("Заказ должен содержать хотя бы один товар.");
            }

            var customerExists = await _db.Users.AnyAsync(u => u.Id == request.CustomerId);
            if (!customerExists)
            {
                return BadRequest($"Покупатель с Id={request.CustomerId} не найден.");
            }

            var order = new Order
            {
                OrderNumber = await GenerateOrderNumberAsync(),
                CustomerId = request.CustomerId,
                SellerId = request.SellerId,
                OrderDate = DateTime.UtcNow,
                Status = "New",
                DeliveryAddress = request.DeliveryAddress,
                Comment = request.Comment,
                TotalAmount = 0
            };

            decimal total = 0;

            foreach (var item in request.Items)
            {
                var product = await _db.Products.FindAsync(item.ProductId);

                if (product == null)
                {
                    return BadRequest($"Товар с Id={item.ProductId} не найден.");
                }

                if (product.Quantity < item.Quantity)
                {
                    return BadRequest(
                        $"Товара \"{product.Name}\" недостаточно на складе. " +
                        $"Доступно: {product.Quantity}, запрошено: {item.Quantity}.");
                }

                var orderItem = new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    Price = product.SalePrice
                };

                order.OrderItems.Add(orderItem);

                total += product.SalePrice * item.Quantity;

                product.Quantity -= item.Quantity;
            }

            order.TotalAmount = total;

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string newStatus)
        {
            var order = await _db.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            string[] allowed = { "Новый", "Оплачен", "Отправлен", "Завершён", "Отменён" };

            if (Array.IndexOf(allowed, newStatus) == -1)
            {
                return BadRequest($"Недопустимый статус: {newStatus}. " +
                                  $"Разрешены: {string.Join(", ", allowed)}");
            }

            order.Status = newStatus;
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            _db.OrderItems.RemoveRange(order.OrderItems);
            _db.Orders.Remove(order);

            await _db.SaveChangesAsync();

            return NoContent();
        }

        // Генерация номера заказа в формате ORD-2026-0001
        private async Task<string> GenerateOrderNumberAsync()
        {
            int year = DateTime.UtcNow.Year;
            string prefix = $"ORD-{year}-";
            var lastOrder = await _db.Orders
                .Where(o => o.OrderNumber.StartsWith(prefix))
                .OrderByDescending(o => o.OrderNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastOrder != null)
            {
                string numberPart = lastOrder.OrderNumber.Substring(prefix.Length);

                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }
    }
}