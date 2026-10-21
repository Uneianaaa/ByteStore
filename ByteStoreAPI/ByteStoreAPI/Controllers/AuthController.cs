using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ByteStoreAPI.Data;
using ByteStoreAPI.Models;
using ByteStoreAPI.Services;

namespace ByteStoreAPI.Controllers
{
    public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
    public class VerifyRequest
    {
        public string Email { get; set; } = null!;
        public string Code { get; set; } = null!;
    }
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly TokenService _tokenService;
        private readonly EmailService _emailService;

        public AuthController(
            AppDbContext db,
            TokenService tokenService,
            EmailService emailService)
        {
            _db = db;
            _tokenService = tokenService;
            _emailService = emailService;
        }

        // Создаёт пользователя, генерирует код и отправляет его на email
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return BadRequest("Email обязателен.");
            }

            if (string.IsNullOrWhiteSpace(user.Password))
            {
                return BadRequest("Пароль обязателен.");
            }
            var emailExists = await _db.Users.AnyAsync(u => u.Email == user.Email);

            if (emailExists)
            {
                return Conflict("Пользователь с таким email уже существует.");
            }
            user.Role = "Сотрудник";
            user.IsVerified = false;
            user.CreatedAt = DateTime.UtcNow;

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            string code = CodeGenerator.Generate();

            var verification = new VerificationCode
            {
                UserId = user.Id,
                Code = code,
                Purpose = "Registration",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false
            };

            _db.VerificationCodes.Add(verification);
            await _db.SaveChangesAsync();

            // Отправляем код на почту
            try
            {
                await _emailService.SendCodeAsync(user.Email, code);
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    message = "Пользователь создан, но письмо не отправилось. " +
                              "Обратитесь в поддержку.",
                    error = ex.Message,
                    userId = user.Id
                });
            }

            return Ok(new
            {
                message = "Регистрация успешна. Код отправлен на email.",
                userId = user.Id,
                email = user.Email
            });
        }

        // Подтверждение регистрации по коду из письма
        [HttpPost("verify")]
        public async Task<IActionResult> Verify([FromBody] VerifyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Code))
            {
                return BadRequest("Email и код обязательны.");
            }
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return NotFound("Пользователь не найден.");
            }

            if (user.IsVerified)
            {
                return BadRequest("Пользователь уже подтверждён.");
            }

            // Ищем актуальный код регистрации
            var verification = await _db.VerificationCodes
                .Where(v => v.UserId == user.Id
                         && v.Purpose == "Registration"
                         && !v.IsUsed
                         && v.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();

            if (verification == null)
            {
                return BadRequest("Код не найден или истёк.");
            }

            if (verification.Code != request.Code)
            {
                return BadRequest("Неверный код.");
            }
            verification.IsUsed = true;
            user.IsVerified = true;

            await _db.SaveChangesAsync();
            string token = _tokenService.GenerateToken(user);

            return Ok(new
            {
                message = "Email подтверждён.",
                token = token,
                userId = user.Id,
                email = user.Email,
                role = user.Role
            });
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Email обязателен.");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Пароль обязателен.");
            }
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return Unauthorized("Неверный email или пароль.");
            }
            if (user.Password != request.Password)
            {
                return Unauthorized("Неверный email или пароль.");
            }
            if (!user.IsVerified)
            {
                return StatusCode(403, "Email не подтверждён. Введите код из письма.");
            }
            string token = _tokenService.GenerateToken(user);

            return Ok(new
            {
                message = "Вход выполнен.",
                token = token,
                userId = user.Id,
                email = user.Email,
                fullName = $"{user.LastName} {user.FirstName}",
                role = user.Role
            });
        }
    }
}