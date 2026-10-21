using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ByteStoreAPI.Data;
using ByteStoreAPI.Models;
using ByteStoreAPI.Services;

namespace ByteStoreAPI.Controllers
{
    public class UpdateProfileRequest
    {
        public string LastName { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }

    public class ChangeEmailRequest
    {
        public string NewEmail { get; set; } = null!;
    }

    public class ConfirmEmailRequest
    {
        public string NewEmail { get; set; } = null!;
        public string Code { get; set; } = null!;
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly EmailService _emailService;

        public ProfileController(AppDbContext db, EmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        private int GetCurrentUserId()
        {
            string? idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(idStr))
            {
                return 0;
            }

            return int.Parse(idStr);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            int userId = GetCurrentUserId();

            var user = await _db.Users
                .Where(u => u.Id == userId)
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
                    u.CreatedAt,
                    u.PhotoPath
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
        {
            int userId = GetCurrentUserId();

            var user = await _db.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            user.LastName = request.LastName;
            user.FirstName = request.FirstName;
            user.MiddleName = request.MiddleName;
            user.Phone = request.Phone;
            user.Address = request.Address;

            await _db.SaveChangesAsync();

            return NoContent();
        }

        // Принимает файл, сохраняет в wwwroot/uploads
        [HttpPost("me/photo")]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
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

            int userId = GetCurrentUserId();

            var user = await _db.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            string folder = Path.Combine("wwwroot", "uploads");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string fileName = $"user_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
            string filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            user.PhotoPath = $"/uploads/{fileName}";

            await _db.SaveChangesAsync();

            return Ok(new { photoPath = user.PhotoPath });
        }

        [HttpPost("me/change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.OldPassword) ||
                string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest("Старый и новый пароль обязательны.");
            }

            int userId = GetCurrentUserId();

            var user = await _db.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            if (user.Password != request.OldPassword)
            {
                return BadRequest("Неверный старый пароль.");
            }

            user.Password = request.NewPassword;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Пароль изменён." });
        }

        // Отправляет код на НОВУЮ почту, сохраняет с Purpose = EmailChange
        [HttpPost("me/request-email-change")]
        public async Task<IActionResult> RequestEmailChange([FromBody] ChangeEmailRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewEmail))
            {
                return BadRequest("Новый email обязателен.");
            }

            int userId = GetCurrentUserId();

            var user = await _db.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            bool emailTaken = await _db.Users
                .AnyAsync(u => u.Email == request.NewEmail && u.Id != userId);

            if (emailTaken)
            {
                return Conflict("Этот email уже занят.");
            }

            string code = CodeGenerator.Generate();

            var verification = new VerificationCode
            {
                UserId = userId,
                Code = code,
                Purpose = "EmailChange",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false
            };

            _db.VerificationCodes.Add(verification);
            await _db.SaveChangesAsync();

            try
            {
                await _emailService.SendCodeAsync(request.NewEmail, code);
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    message = "Код создан, но письмо не отправилось.",
                    error = ex.Message
                });
            }

            return Ok(new { message = "Код отправлен на новый email." });
        }

        // Подтверждение смены email по коду
        [HttpPost("me/confirm-email-change")]
        public async Task<IActionResult> ConfirmEmailChange([FromBody] ConfirmEmailRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewEmail) ||
                string.IsNullOrWhiteSpace(request.Code))
            {
                return BadRequest("Email и код обязательны.");
            }

            int userId = GetCurrentUserId();

            var user = await _db.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var verification = await _db.VerificationCodes
                .Where(v => v.UserId == userId
                         && v.Purpose == "EmailChange"
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

            user.Email = request.NewEmail;
            verification.IsUsed = true;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Email изменён." });
        }
    }
}