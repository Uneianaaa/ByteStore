using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ByteStoreAPI.Data;
using ByteStoreAPI.Models;

namespace ByteStoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ManufacturersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ManufacturersController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var manufacturers = await _db.Manufacturers.ToListAsync();
            return Ok(manufacturers);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var manufacturer = await _db.Manufacturers.FindAsync(id);

            if (manufacturer == null)
            {
                return NotFound();
            }

            return Ok(manufacturer);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Manufacturer manufacturer)
        {
            _db.Manufacturers.Add(manufacturer);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = manufacturer.Id }, manufacturer);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Manufacturer updated)
        {
            var manufacturer = await _db.Manufacturers.FindAsync(id);

            if (manufacturer == null)
            {
                return NotFound();
            }

            manufacturer.CompanyName = updated.CompanyName;
            manufacturer.Country = updated.Country;

            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var manufacturer = await _db.Manufacturers.FindAsync(id);

            if (manufacturer == null)
            {
                return NotFound();
            }

            bool hasProducts = await _db.Products.AnyAsync(p => p.ManufacturerId == id);

            if (hasProducts)
            {
                return Conflict("Нельзя удалить производителя, так как есть товары этого бренда.");
            }

            _db.Manufacturers.Remove(manufacturer);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}