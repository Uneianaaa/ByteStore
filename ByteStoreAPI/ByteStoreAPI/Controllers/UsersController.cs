using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ByteStoreAPI.Data;
using ByteStoreAPI.Models;

namespace ByteStoreAPI.Controllers
{
    public class RoleRequest
    {
        public string Role { get; set; } = null!;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public UsersController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _db.Users
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.LastName,
                    u.FirstName,
                    u.MiddleName,
                    u.Phone,
                    u.Role,
                    u.IsVerified,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _db.Users
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.LastName,
                    u.FirstName,
                    u.MiddleName,
                    u.Phone,
                    u.Address,
                    u.Role,
                    u.IsVerified,
                    u.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] RoleRequest request)
        {
            var user = await _db.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            string[] allowed = { "Администратор", "Сотрудник", "Продавец", "Кладовщик" };

            if (Array.IndexOf(allowed, request.Role) == -1)
            {
                return BadRequest($"Недопустимая роль: {request.Role}. " +
                                  $"Разрешены: {string.Join(", ", allowed)}");
            }

            user.Role = request.Role;
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _db.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            bool hasOrders = await _db.Orders.AnyAsync(o => o.SellerId == id);

            if (hasOrders)
            {
                return Conflict(
                    "Нельзя удалить сотрудника, он оформлял заказы.");
            }

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}