using System.Net;
using System.Net.Mail;

namespace QuanLyKhachSan.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IConfiguration configuration,
            ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendAsync(
            string? toEmail,
            string subject,
            string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                return false;
            }

            string? host = _configuration["Email:SmtpHost"];
            string? portText = _configuration["Email:SmtpPort"];
            string? senderEmail = _configuration["Email:SenderEmail"];
            string? senderName = _configuration["Email:SenderName"];
            string? password = _configuration["Email:Password"];

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(senderEmail) ||
                string.IsNullOrWhiteSpace(password) ||
                !int.TryParse(portText, out int port))
            {
                _logger.LogWarning(
                    "Chưa cấu hình đầy đủ Email trong appsettings.json.");

                return false;
            }

            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(
                        senderEmail,
                        string.IsNullOrWhiteSpace(senderName)
                            ? "Luxury Hotel"
                            : senderName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                message.To.Add(toEmail.Trim());

                using var smtpClient = new SmtpClient(host, port)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(
                        senderEmail,
                        password)
                };

                await smtpClient.SendMailAsync(message);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Không thể gửi email tới {Email}.",
                    toEmail);

                return false;
            }
        }
    }
}