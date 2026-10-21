using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ByteStoreAPI.Services
{
    // Сервис отправки email через SMTP
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendCodeAsync(string toEmail, string code)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(
                _config["Smtp:DisplayName"],
                _config["Smtp:Email"]
            ));

            message.To.Add(new MailboxAddress("", toEmail));

            message.Subject = "Код подтверждения регистрации";

            message.Body = new TextPart("plain")
            {
                Text = $"Здравствуйте!\n\n" +
                       $"Ваш код подтверждения: {code}\n\n" +
                       $"Никому не сообщайте его. Код действует 10 минут."
            };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(
                    _config["Smtp:Host"],
                    int.Parse(_config["Smtp:Port"]!),
                    SecureSocketOptions.StartTls
                );

                await client.AuthenticateAsync(
                    _config["Smtp:Email"],
                    _config["Smtp:Password"]
                );

                await client.SendAsync(message);

                await client.DisconnectAsync(true);
            }
        }
    }
}