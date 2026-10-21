using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ByteStoreAPI.Data;
using ByteStoreAPI.Models;

namespace ByteStoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SuppliersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SuppliersController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var suppliers = await _db.Suppliers.ToListAsync();
            return Ok(suppliers);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var supplier = await _db.Suppliers.FindAsync(id);

            if (supplier == null)
            {
                return NotFound();
            }

            return Ok(supplier);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Supplier supplier)
        {
            _db.Suppliers.Add(supplier);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, supplier);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Supplier updated)
        {
            var supplier = await _db.Suppliers.FindAsync(id);

            if (supplier == null)
            {
                return NotFound();
            }

            supplier.CompanyName = updated.CompanyName;
            supplier.Phone = updated.Phone;
            supplier.Email = updated.Email;
            supplier.Address = updated.Address;
            supplier.ContactLastName = updated.ContactLastName;
            supplier.ContactFirstName = updated.ContactFirstName;
            supplier.ContactMiddleName = updated.ContactMiddleName;

            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await _db.Suppliers.FindAsync(id);

            if (supplier == null)
            {
                return NotFound();
            }

            bool hasProducts = await _db.Products.AnyAsync(p => p.SupplierId == id);

            if (hasProducts)
            {
                return Conflict("Нельзя удалить поставщика — есть товары от него.");
            }

            _db.Suppliers.Remove(supplier);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}