using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ByteStoreAPI.Data;
using ByteStoreAPI.Models;

namespace ByteStoreAPI.Controllers
{
    // Класс для отправки сообщения
    public class SendMessageRequest
    {
        public int ReceiverId { get; set; }
        public string Text { get; set; } = null!;
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public MessagesController(AppDbContext db)
        {
            _db = db;
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
        //кому можно писать
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            int myId = GetCurrentUserId();

            var users = await _db.Users
                .Where(u => u.Id != myId)
                .OrderBy(u => u.LastName)
                .Select(u => new
                {
                    u.Id,
                    FullName = u.LastName + " " + u.FirstName,
                    u.Role
                })
                .ToListAsync();

            return Ok(users);
        }
        // Переписка с конкретным пользователем
        [HttpGet("with/{userId}")]
        public async Task<IActionResult> GetDialog(int userId)
        {
            int myId = GetCurrentUserId();

            var messages = await _db.Messages
                .Where(m => (m.SenderId == myId && m.ReceiverId == userId)
                         || (m.SenderId == userId && m.ReceiverId == myId))
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    m.ReceiverId,
                    m.Text,
                    m.SentAt,
                    m.IsRead,
                    IsMine = m.SenderId == myId
                })
                .ToListAsync();

            // Помечаем сообщения от собеседника как прочитанные
            var unread = await _db.Messages
                .Where(m => m.SenderId == userId
                         && m.ReceiverId == myId
                         && !m.IsRead)
                .ToListAsync();

            foreach (var msg in unread)
            {
                msg.IsRead = true;
            }

            if (unread.Count > 0)
            {
                await _db.SaveChangesAsync();
            }

            return Ok(messages);
        }
        // Отправить сообщение
        [HttpPost]
        public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
        {
            if (request.ReceiverId <= 0 || string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest("Получатель и текст обязательны.");
            }

            int myId = GetCurrentUserId();

            bool receiverExists = await _db.Users.AnyAsync(u => u.Id == request.ReceiverId);

            if (!receiverExists)
            {
                return BadRequest("Получатель не найден.");
            }

            var message = new Message
            {
                SenderId = myId,
                ReceiverId = request.ReceiverId,
                Text = request.Text.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _db.Messages.Add(message);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Отправлено.", id = message.Id });
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            int myId = GetCurrentUserId();

            int count = await _db.Messages
                .CountAsync(m => m.ReceiverId == myId && !m.IsRead);

            return Ok(new { count = count });
        }

        // Список существующих диалогов с последним сообщением
        [HttpGet("dialogs")]
        public async Task<IActionResult> GetDialogs()
        {
            int myId = GetCurrentUserId();

            var allMessages = await _db.Messages
                .Where(m => m.SenderId == myId || m.ReceiverId == myId)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            var dialogs = new List<object>();
            var seenUserIds = new HashSet<int>();

            foreach (var msg in allMessages)
            {
                int otherId = msg.SenderId == myId ? msg.ReceiverId : msg.SenderId;

                if (seenUserIds.Contains(otherId))
                {
                    continue;
                }

                seenUserIds.Add(otherId);

                var otherUser = await _db.Users.FindAsync(otherId);
                string otherName = otherUser != null
                    ? otherUser.LastName + " " + otherUser.FirstName
                    : "Неизвестный";

                int unreadCount = await _db.Messages
                    .CountAsync(m => m.SenderId == otherId && m.ReceiverId == myId && !m.IsRead);

                dialogs.Add(new
                {
                    UserId = otherId,
                    FullName = otherName,
                    LastMessage = msg.Text,
                    LastSentAt = msg.SentAt,
                    UnreadCount = unreadCount
                });
            }

            return Ok(dialogs);
        }
    }
}