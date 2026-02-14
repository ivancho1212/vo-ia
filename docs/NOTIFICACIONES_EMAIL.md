# Sistema de Notificaciones por Email para Administradores

## 📧 Objetivo
Notificar al usuario administrativo cuando recibe mensajes mientras NO está logueado, similar a Google Hangouts.

---

## 🔧 Implementación Backend (.NET)

### 1. Servicio de Email (`Services/EmailService.cs`)

```csharp
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
            var subject = $"Nuevo mensaje en Sesión {conversationId}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f5f5f5;'>
                        <div style='background-color: white; padding: 30px; border-radius: 8px;'>
                            <h2 style='color: #17a2b8; margin-bottom: 20px;'>
                                💬 Nuevo mensaje en tu ausencia
                            </h2>
                            <p style='color: #333; font-size: 16px;'>
                                Has recibido <strong>{unreadCount}</strong> mensaje(s) nuevo(s) en la <strong>Sesión {conversationId}</strong>:
                            </p>
                            <div style='background-color: #f8f9fa; padding: 15px; border-left: 4px solid #17a2b8; margin: 20px 0;'>
                                <p style='color: #666; font-style: italic;'>
                                    ""{messagePreview}""
                                </p>
                            </div>
                            <a href='http://localhost:3000/data/conversations' 
                               style='display: inline-block; background-color: #17a2b8; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin-top: 20px;'>
                                Ver conversación
                            </a>
                            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                            <p style='color: #999; font-size: 12px; text-align: center;'>
                                Este es un mensaje automático de VOIA. No respondas a este correo.
                            </p>
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
            var totalUnread = unreadConversations.Values.Sum();
            var subject = $"Tienes {totalUnread} mensajes sin leer en {unreadConversations.Count} conversaciones";
            
            var conversationsList = string.Join("", unreadConversations.Select(kvp => 
                $"<li><strong>Sesión {kvp.Key}</strong>: {kvp.Value} mensaje(s)</li>"
            ));

            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f5f5f5;'>
                        <div style='background-color: white; padding: 30px; border-radius: 8px;'>
                            <h2 style='color: #17a2b8; margin-bottom: 20px;'>
                                📬 Resumen de mensajes sin leer
                            </h2>
                            <p style='color: #333; font-size: 16px;'>
                                Tienes <strong>{totalUnread}</strong> mensajes sin leer en <strong>{unreadConversations.Count}</strong> conversaciones:
                            </p>
                            <ul style='color: #666; line-height: 1.8;'>
                                {conversationsList}
                            </ul>
                            <a href='http://localhost:3000/data/conversations' 
                               style='display: inline-block; background-color: #17a2b8; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin-top: 20px;'>
                                Ver todas las conversaciones
                            </a>
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

                await client.SendAsync(mailMessage);
                _logger.LogInformation($"Email enviado exitosamente a {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al enviar email a {toEmail}");
                throw;
            }
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
    }
}
```

---

### 2. Configuración en `appsettings.json`

```json
{
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "tu-email@gmail.com",
    "SmtpPassword": "tu-app-password",
    "FromEmail": "noreply@voia.com",
    "FromName": "VOIA Notifications",
    "EnableSsl": true
  },
  "NotificationSettings": {
    "SendImmediateNotification": true,
    "BatchNotificationIntervalMinutes": 15,
    "MaxMessagesBeforeBatch": 3
  }
}
```

**Nota para Gmail:** 
- Usa "App Passwords" en lugar de la contraseña normal
- Configuración → Seguridad → Verificación en 2 pasos → Contraseñas de aplicaciones

---

### 3. Worker para Notificaciones por Lote (`Workers/EmailNotificationWorker.cs`)

```csharp
using Microsoft.EntityFrameworkCore;

namespace Voia.Api.Workers
{
    public class EmailNotificationWorker : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<EmailNotificationWorker> _logger;
        private readonly int _intervalMinutes = 15;

        public EmailNotificationWorker(IServiceProvider services, ILogger<EmailNotificationWorker> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendBatchNotificationsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en EmailNotificationWorker");
                }

                await Task.Delay(TimeSpan.FromMinutes(_intervalMinutes), stoppingToken);
            }
        }

        private async Task SendBatchNotificationsAsync()
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<VoiaDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            // Obtener admins activos
            var admins = await userManager.GetUsersInRoleAsync("Admin");

            foreach (var admin in admins)
            {
                // Verificar si el admin está desconectado (último heartbeat > 30 min)
                var lastActivity = await context.ActivityLogs
                    .Where(a => a.UserId == admin.Id)
                    .OrderByDescending(a => a.Timestamp)
                    .Select(a => a.Timestamp)
                    .FirstOrDefaultAsync();

                var isOffline = lastActivity == null || 
                                (DateTime.UtcNow - lastActivity) > TimeSpan.FromMinutes(30);

                if (!isOffline) continue;

                // Obtener conversaciones con mensajes no leídos
                var unreadConversations = await context.Conversations
                    .Where(c => c.UnreadAdminMessages > 0)
                    .Select(c => new { c.Id, c.UnreadAdminMessages })
                    .ToDictionaryAsync(c => c.Id.ToString(), c => c.UnreadAdminMessages);

                if (unreadConversations.Any())
                {
                    await emailService.SendBatchNotificationAsync(
                        admin.Email, 
                        unreadConversations
                    );

                    _logger.LogInformation($"Notificación por lote enviada a {admin.Email}");
                }
            }
        }
    }
}
```

---

### 4. Modificar SignalR Hub (`Hubs/ConversationsHub.cs`)

```csharp
public class ConversationsHub : Hub
{
    private readonly VoiaDbContext _context;
    private readonly IEmailService _emailService;
    private readonly UserManager<User> _userManager;

    public ConversationsHub(
        VoiaDbContext context, 
        IEmailService emailService, 
        UserManager<User> userManager)
    {
        _context = context;
        _emailService = emailService;
        _userManager = userManager;
    }

    // Llamar este método cuando llega un mensaje nuevo
    private async Task NotifyAdminIfOfflineAsync(int conversationId, string messageText)
    {
        // Verificar si hay admins conectados en este momento
        var connectedAdmins = await _context.ActivityLogs
            .Where(a => a.UserId != null && 
                        a.Activity == "Connected" && 
                        a.Timestamp > DateTime.UtcNow.AddMinutes(-5))
            .Select(a => a.UserId)
            .Distinct()
            .ToListAsync();

        // Si no hay admins conectados, enviar email
        if (!connectedAdmins.Any())
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            
            foreach (var admin in admins)
            {
                var unreadCount = await _context.Messages
                    .Where(m => m.ConversationId == conversationId && !m.IsReadByAdmin)
                    .CountAsync();

                await _emailService.SendAdminNotificationAsync(
                    admin.Email,
                    conversationId.ToString(),
                    messageText,
                    unreadCount
                );
            }
        }
    }

    // Agregar en el método que recibe mensajes
    public async Task UserMessage(int conversationId, string text)
    {
        // ... código existente para guardar mensaje ...

        // Notificar a admins offline
        await NotifyAdminIfOfflineAsync(conversationId, text);
    }
}
```

---

### 5. Registrar Servicios en `Program.cs`

```csharp
// Configuración de Email
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();

// Worker para notificaciones por lote
builder.Services.AddHostedService<EmailNotificationWorker>();
```

---

## 📊 Estrategia de Notificaciones

### Opción 1: Notificación Inmediata (Como Hangouts)
- ✅ Envía email instantáneamente cuando llega un mensaje
- ✅ Mejor experiencia de usuario
- ⚠️ Puede generar muchos emails si hay mucha actividad

### Opción 2: Notificación por Lote
- ✅ Agrupa mensajes cada 15 minutos
- ✅ Menos saturación de inbox
- ⚠️ Menos inmediata

### Opción 3: Híbrida (Recomendada)
- 1er mensaje → Notificación inmediata
- Mensajes subsecuentes → Agrupa por 15 min
- Si pasan 3+ mensajes → Envía resumen

---

## 🔔 Agregar Campo `UnreadAdminMessages` a Conversaciones

```sql
ALTER TABLE Conversations 
ADD UnreadAdminMessages INT NOT NULL DEFAULT 0;
```

```csharp
// En ConversationController
[HttpPost("{id}/mark-read")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> MarkAsRead(int id)
{
    var conversation = await _context.Conversations.FindAsync(id);
    if (conversation == null) return NotFound();

    conversation.UnreadAdminMessages = 0;
    await _context.SaveChangesAsync();

    return Ok();
}
```

---

## ✅ Checklist de Implementación

- [ ] Crear `EmailService.cs`
- [ ] Agregar configuración en `appsettings.json`
- [ ] Crear worker `EmailNotificationWorker.cs`
- [ ] Modificar SignalR Hub para detectar admins offline
- [ ] Agregar campo `UnreadAdminMessages` a BD
- [ ] Registrar servicios en `Program.cs`
- [ ] Configurar credenciales SMTP (Gmail App Password)
- [ ] Probar envío de emails en desarrollo
- [ ] Implementar lógica de marcado como leído en frontend

---

## 🎨 Mejoras Visuales Frontend (Ya Implementadas)

✅ **Badge azul (info) con contador** de mensajes no leídos
✅ **Animación de pulso** en el indicador
✅ **Sombra sutil** para mayor visibilidad
✅ **Muestra hasta 99+ mensajes**

---

## 📧 Proveedores SMTP Recomendados

1. **Gmail** (desarrollo): smtp.gmail.com:587
2. **SendGrid** (producción): smtp.sendgrid.net:587
3. **Mailgun** (producción): smtp.mailgun.org:587
4. **AWS SES**: email-smtp.region.amazonaws.com:587

---

## 🚀 Próximos Pasos

1. Implementar `EmailService` básico
2. Probar con Gmail en local
3. Agregar worker para notificaciones por lote
4. Configurar SendGrid/Mailgun para producción
5. Agregar panel de configuración de notificaciones en dashboard
