using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using ByteStoreAPI.Data;
using ByteStoreAPI.Models;

namespace ByteStoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ProductsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _db.Products.ToListAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _db.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        [HttpGet("{id}/full")]
        public async Task<IActionResult> GetFull(int id)
        {
            var product = await _db.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            string categoryName = "";
            var category = await _db.Categories.FindAsync(product.CategoryId);
            if (category != null)
            {
                categoryName = category.Name;
            }

            string manufacturerName = "";
            if (product.ManufacturerId.HasValue)
            {
                var m = await _db.Manufacturers.FindAsync(product.ManufacturerId.Value);
                if (m != null)
                {
                    manufacturerName = m.CompanyName;
                }
            }

            string supplierName = "";
            if (product.SupplierId.HasValue)
            {
                var s = await _db.Suppliers.FindAsync(product.SupplierId.Value);
                if (s != null)
                {
                    supplierName = s.CompanyName;
                }
            }

            var orderItems = await _db.OrderItems
                .Where(oi => oi.ProductId == id)
                .ToListAsync();

            int totalSold = orderItems.Sum(oi => oi.Quantity);
            decimal totalRevenue = orderItems.Sum(oi => oi.Quantity * oi.Price);

            var orderIds = orderItems.Select(oi => oi.OrderId).Distinct().ToList();

            var orders = await _db.Orders
                .Where(o => orderIds.Contains(o.Id))
                .Select(o => new
                {
                    o.Id,
                    o.OrderNumber,
                    o.OrderDate,
                    o.Status,
                    o.TotalAmount
                })
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(new
            {
                product.Id,
                product.Article,
                product.Name,
                product.Description,
                product.Color,
                product.PurchasePrice,
                product.SalePrice,
                product.Quantity,
                product.WarrantyMonths,
                product.CreatedAt,
                product.PhotoPath,

                categoryId = product.CategoryId,
                categoryName = categoryName,
                manufacturerId = product.ManufacturerId,
                manufacturerName = manufacturerName,
                supplierId = product.SupplierId,
                supplierName = supplierName,

                totalSold = totalSold,
                totalRevenue = totalRevenue,
                orderCount = orders.Count,

                orders = orders
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Product product)
        {
            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        // принимает multipart/form-data, кладёт файл в wwwroot/uploads
        [HttpPost("{id}/photo")]
        public async Task<IActionResult> UploadPhoto(int id, IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
            {
                return BadRequest("Файл не выбран.");
            }

            string ext = Path.GetExtension(photo.FileName).ToLower();

            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
            {
                return BadRequest("Разрешены только JPG и PNG.");
            }

            var product = await _db.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            string folder = Path.Combine("wwwroot", "uploads");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string fileName = $"product_{id}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
            string filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            product.PhotoPath = $"/uploads/{fileName}";
            await _db.SaveChangesAsync();

            return Ok(new { photoPath = product.PhotoPath });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Product updated)
        {
            var product = await _db.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            product.Article = updated.Article;
            product.Name = updated.Name;
            product.CategoryId = updated.CategoryId;
            product.ManufacturerId = updated.ManufacturerId;
            product.SupplierId = updated.SupplierId;
            product.PurchasePrice = updated.PurchasePrice;
            product.SalePrice = updated.SalePrice;
            product.Quantity = updated.Quantity;
            product.Description = updated.Description;
            product.Color = updated.Color;
            product.WarrantyMonths = updated.WarrantyMonths;

            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            bool usedInOrders = await _db.OrderItems.AnyAsync(oi => oi.ProductId == id);

            if (usedInOrders)
            {
                return Conflict("Нельзя удалить товар, с ним есть заказы.");
            }

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}