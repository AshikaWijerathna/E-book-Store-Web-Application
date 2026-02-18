using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace BookShoppingWeb.Services
{
    public class EmailService
    {
        public readonly EmailSettings _settings;
        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }
        public async Task SendEmailAsync(string toEmail, string subject,string body)
        {
            var smtp = new SmtpClient(_settings.SmtpServer, _settings.Port)
            {
                Credentials = new NetworkCredential(
                    _settings.Username,
                    _settings.Password),
                EnableSsl = true
            };
            var message = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };
            message.To.Add(toEmail);
            await smtp.SendMailAsync(message);
        }
    }
}
