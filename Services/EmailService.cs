using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Voia.Api.Services
{
    public interface IEmailService
    {
        Task SendAdminNotificationAsync(string adminEmail, string conversationId, string messagePreview, int unreadCount);
        Task SendBatchNotificationAsync(string adminEmail, Dictionary<string, int> unreadConversations);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendAdminNotificationAsync(
            string adminEmail, 
            string conversationId, 
            string messagePreview, 
            int unreadCount)
        {
            if (string.IsNullOrEmpty(adminEmail))
            {
                _logger.LogWarning("Intento de enviar email a dirección vacía");
                return;
            }

            if (!_settings.EnableNotifications)
            {
                _logger.LogInformation("Notificaciones por email deshabilitadas en configuración");
                return;
            }

            var subject = $"Nuevo mensaje en Sesión {conversationId}";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: 'Segoe UI', Arial, sans-serif; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f5f5f5; }}
                        .content {{ background-color: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
                        .header {{ color: #17a2b8; margin-bottom: 20px; font-size: 24px; }}
                        .message-box {{ background-color: #f8f9fa; padding: 15px; border-left: 4px solid #17a2b8; margin: 20px 0; }}
                        .button {{ display: inline-block; background-color: #17a2b8; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin-top: 20px; }}
                        .footer {{ color: #999; font-size: 12px; text-align: center; margin-top: 30px; border-top: 1px solid #eee; padding-top: 20px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='content'>
                            <h2 class='header'>💬 Nuevo mensaje en tu ausencia</h2>
                            <p style='color: #333; font-size: 16px;'>
                                Has recibido <strong>{unreadCount}</strong> mensaje(s) nuevo(s) en la <strong>Sesión {conversationId}</strong>:
                            </p>
                            <div class='message-box'>
                                <p style='color: #666; font-style: italic; margin: 0;'>
                                    ""{TruncateMessage(messagePreview, 150)}""
                                </p>
                            </div>
                            <a href='{_settings.DashboardUrl}/data/conversations' class='button'>
                                Ver conversación
                            </a>
                            <div class='footer'>
                                Este es un mensaje automático de VOIA. No respondas a este correo.
                            </div>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(adminEmail, subject, body);
        }

        public async Task SendBatchNotificationAsync(
            string adminEmail, 
            Dictionary<string, int> unreadConversations)
        {
            if (string.IsNullOrEmpty(adminEmail))
            {
                _logger.LogWarning("Intento de enviar email a dirección vacía");
                return;
            }

            if (!_settings.EnableNotifications)
            {
                _logger.LogInformation("Notificaciones por email deshabilitadas en configuración");
                return;
            }

            var totalUnread = unreadConversations.Values.Sum();
            var subject = $"Tienes {totalUnread} mensajes sin leer en {unreadConversations.Count} conversaciones";
            
            var conversationsList = string.Join("", unreadConversations.Select(kvp => 
                $"<li style='padding: 8px 0;'><strong>Sesión {kvp.Key}</strong>: {kvp.Value} mensaje(s)</li>"
            ));

            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: 'Segoe UI', Arial, sans-serif; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f5f5f5; }}
                        .content {{ background-color: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
                        .header {{ color: #17a2b8; margin-bottom: 20px; font-size: 24px; }}
                        .button {{ display: inline-block; background-color: #17a2b8; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin-top: 20px; }}
                        .footer {{ color: #999; font-size: 12px; text-align: center; margin-top: 30px; border-top: 1px solid #eee; padding-top: 20px; }}
                        ul {{ list-style: none; padding: 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='content'>
                            <h2 class='header'>📬 Resumen de mensajes sin leer</h2>
                            <p style='color: #333; font-size: 16px;'>
                                Tienes <strong>{totalUnread}</strong> mensajes sin leer en <strong>{unreadConversations.Count}</strong> conversaciones:
                            </p>
                            <ul style='color: #666; line-height: 1.8; background-color: #f8f9fa; padding: 20px; border-radius: 5px;'>
                                {conversationsList}
                            </ul>
                            <a href='{_settings.DashboardUrl}/data/conversations' class='button'>
                                Ver todas las conversaciones
                            </a>
                            <div class='footer'>
                                Este es un mensaje automático de VOIA. No respondas a este correo.
                            </div>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(adminEmail, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword),
                    EnableSsl = _settings.EnableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_settings.FromEmail, _settings.FromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"✅ Email enviado exitosamente a {toEmail}: {subject}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error al enviar email a {toEmail}");
                // No relanzar la excepción para evitar que falle el flujo principal
            }
        }

        private static string TruncateMessage(string message, int maxLength)
        {
            if (string.IsNullOrEmpty(message)) return "Sin contenido";
            if (message.Length <= maxLength) return message;
            return message.Substring(0, maxLength) + "...";
        }
    }

    public class EmailSettings
    {
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; } = "";
        public string SmtpPassword { get; set; } = "";
        public string FromEmail { get; set; } = "";
        public string FromName { get; set; } = "VOIA Notifications";
        public bool EnableSsl { get; set; } = true;
        public bool EnableNotifications { get; set; } = false; // Deshabilitado por defecto
        public string DashboardUrl { get; set; } = "http://localhost:3000";
    }
}
