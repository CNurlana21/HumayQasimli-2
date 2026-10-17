using System.Net;
using System.Net.Mail;
using System.Net.Http.Json;

namespace HumayQasimli.Services
{
    /// <summary>
    /// Brevo HTTP API ile mail gonderir (Render pulsuz planinda SMTP baglidir).
    /// Brevo:ApiKey yoxdursa kohne SMTP yoluna kecir (lokal inkisaf ucun).
    /// </summary>
    public class EmailSender : IEmailSender
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var brevoKey = _configuration["Brevo:ApiKey"];

            if (!string.IsNullOrWhiteSpace(brevoKey))
            {
                await SendViaBrevoAsync(brevoKey, toEmail, subject, htmlMessage);
                return;
            }

            await SendViaSmtpAsync(toEmail, subject, htmlMessage);
        }

        private async Task SendViaBrevoAsync(string apiKey, string toEmail, string subject, string htmlMessage)
        {
            var senderName = _configuration["EmailSettings:SenderName"] ?? "Humay Qasimli";
            var senderEmail = _configuration["Brevo:SenderEmail"] ?? _configuration["EmailSettings:SenderEmail"];

            if (string.IsNullOrWhiteSpace(senderEmail))
            {
                _logger.LogWarning("Brevo sender email teyin olunmayib. To: {ToEmail}", toEmail);
                return;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", apiKey);
            request.Headers.Add("accept", "application/json");
            request.Content = JsonContent.Create(new
            {
                sender = new { name = senderName, email = senderEmail },
                to = new[] { new { email = toEmail } },
                subject,
                htmlContent = htmlMessage
            });

            try
            {
                using var response = await Http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Brevo mail gondermedi. Status: {Status}, Cavab: {Body}", (int)response.StatusCode, body);
                    throw new InvalidOperationException($"Brevo xetasi ({(int)response.StatusCode}): {body}");
                }
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                _logger.LogError(ex, "Brevo ile mail gondermek mumkun olmadi: {ToEmail}", toEmail);
                throw;
            }
        }

        private async Task SendViaSmtpAsync(string toEmail, string subject, string htmlMessage)
        {
            var host = _configuration["EmailSettings:SmtpHost"];
            var port = int.TryParse(_configuration["EmailSettings:SmtpPort"], out var p) ? p : 587;
            var senderName = _configuration["EmailSettings:SenderName"] ?? "Humay Qasimli";
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var username = _configuration["EmailSettings:Username"];
            var password = _configuration["EmailSettings:Password"];
            var enableSsl = bool.TryParse(_configuration["EmailSettings:EnableSsl"], out var ssl) ? ssl : true;

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(senderEmail) ||
                string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Email settings are not fully configured. Skipping real send and logging the message instead. To: {ToEmail}, Subject: {Subject}", toEmail, subject);
                _logger.LogInformation("Email body (not sent - missing SMTP config): {Body}", htmlMessage);
                return;
            }

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mail.To.Add(toEmail);

            try
            {
                await client.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
                throw;
            }
        }
    }
}