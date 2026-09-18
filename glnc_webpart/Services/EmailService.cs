using System.Net;
using System.Net.Mail;

namespace glnc_webpart.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendDeliveryNotificationAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpHost = _configuration["Smtp:Host"] ?? "mail.ncmail.nc";
                var smtpPort = int.Parse(_configuration["Smtp:Port"] ?? "587");
                var smtpUsername = _configuration["Smtp:Username"] ?? "noreply_glnc-2ts@clinotag.com";
                var smtpPassword = _configuration["Smtp:Password"] ?? "Paralyser-Partage84,";
                var fromEmail = _configuration["Smtp:FromEmail"] ?? "noreply_glnc-2ts@clinotag.com";

                using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    smtpClient.EnableSsl = true;

                    using (var mail = new MailMessage())
                    {
                        mail.From = new MailAddress(fromEmail);
                        mail.To.Add(toEmail);
                        mail.Subject = subject;
                        mail.Body = body;

                        await smtpClient.SendMailAsync(mail);
                        _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {ToEmail}", toEmail);
                throw;
            }
        }
    }
}

