using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ByteStoreAPI.Data;
using ByteStoreAPI.Models;

namespace ByteStoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ReportsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("sales")]
        public async Task<IActionResult> SalesReport(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            DateTime dateFrom = from ?? DateTime.UtcNow.AddDays(-30);
            DateTime dateTo = to ?? DateTime.UtcNow;

            if (dateFrom > dateTo)
            {
                return BadRequest("Дата 'from' не может быть позже 'to'.");
            }

            var orders = await _db.Orders
                .Where(o => o.OrderDate >= dateFrom
                         && o.OrderDate <= dateTo
                         && o.Status != "Cancelled")
                .Include(o => o.OrderItems)
                .ToListAsync();

            int orderCount = orders.Count;
            decimal totalAmount = orders.Sum(o => o.TotalAmount);

            var grouped = orders
                .SelectMany(o => o.OrderItems)
                .GroupBy(i => i.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalQuantity = g.Sum(i => i.Quantity),
                    TotalRevenue = g.Sum(i => i.Quantity * i.Price)
                })
                .ToList();

            var productIds = grouped.Select(i => i.ProductId).ToList();
            var productNames = await _db.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name);

            var items = grouped.Select(i => new
            {
                i.ProductId,
                ProductName = productNames.ContainsKey(i.ProductId)
                    ? productNames[i.ProductId]
                    : "—",
                i.TotalQuantity,
                i.TotalRevenue
            }).ToList();

            return Ok(new
            {
                from = dateFrom,
                to = dateTo,
                orderCount = orderCount,
                totalAmount = totalAmount,
                items = items
            });
        }

        [HttpGet("low-stock")]
        public async Task<IActionResult> LowStock()
        {
            var products = await _db.Products
                .Where(p => p.Quantity <= 5)
                .OrderBy(p => p.Quantity)
                .Select(p => new
                {
                    p.Id,
                    p.Article,
                    p.Name,
                    p.Quantity
                })
                .ToListAsync();

            return Ok(products);
        }

        [HttpGet("top-products")]
        public async Task<IActionResult> TopProducts()
        {
            var grouped = await _db.OrderItems
                .GroupBy(i => i.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(i => i.Quantity),
                    TotalRevenue = g.Sum(i => i.Quantity * i.Price)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(10)
                .ToListAsync();

            var productIds = grouped.Select(x => x.ProductId).ToList();
            var productNames = await _db.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name);

            var result = grouped.Select(x => new
            {
                x.ProductId,
                ProductName = productNames.ContainsKey(x.ProductId)
                    ? productNames[x.ProductId]
                    : "—",
                x.TotalSold,
                x.TotalRevenue
            }).ToList();

            return Ok(result);
        }
    }
}