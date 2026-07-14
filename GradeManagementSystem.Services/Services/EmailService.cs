using GradeManagementSystem.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace GradeManagementSystem.Services.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var smtpHost = _configuration["SmtpSettings:Host"];
            var smtpPortValue = _configuration["SmtpSettings:Port"];
            var smtpUser = _configuration["SmtpSettings:UserName"];
            var smtpPass = _configuration["SmtpSettings:Password"];
            var smtpFrom = _configuration["SmtpSettings:From"];
            var enableSslValue = _configuration["SmtpSettings:EnableSsl"];

            if (string.IsNullOrWhiteSpace(smtpHost) ||
                string.IsNullOrWhiteSpace(smtpUser) ||
                string.IsNullOrWhiteSpace(smtpPass) ||
                string.IsNullOrWhiteSpace(smtpFrom) ||
                !int.TryParse(smtpPortValue, out var smtpPort) ||
                !bool.TryParse(enableSslValue, out var enableSsl))
            {
                throw new InvalidOperationException("SMTP settings are incomplete or invalid.");
            }

            using (var client = new SmtpClient(smtpHost, smtpPort))
            {
                client.Credentials = new NetworkCredential(smtpUser, smtpPass);
                client.EnableSsl = enableSsl;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpFrom),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(to);

                await client.SendMailAsync(mailMessage);
            }
        }
    }
}
