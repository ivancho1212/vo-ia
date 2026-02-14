using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Services;

namespace Voia.Api.Workers
{
    public class EmailNotificationWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailNotificationWorker> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15);

        public EmailNotificationWorker(
            IServiceProvider serviceProvider,
            ILogger<EmailNotificationWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("📧 EmailNotificationWorker iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendNotificationsAsync(stoppingToken);
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Apagado normal — no propagar
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error en EmailNotificationWorker");
                }
            }

            _logger.LogInformation("📧 EmailNotificationWorker detenido");
        }

        private async Task CheckAndSendNotificationsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            // Verificar qué admins están offline
            var recentlyActiveAdminIds = await dbContext.ActivityLogs
                .Where(log => log.CreatedAt >= DateTime.UtcNow.AddMinutes(-5))
                .Select(log => log.UserId)
                .Distinct()
                .ToListAsync(stoppingToken);

            // Buscar conversaciones con mensajes sin leer
            var conversationsWithUnread = await dbContext.Conversations
                .Where(c => c.UnreadAdminMessages > 0)
                .Include(c => c.AssignedUser)
                .ToListAsync(stoppingToken);

            if (!conversationsWithUnread.Any())
            {
                _logger.LogDebug("✅ No hay mensajes sin leer");
                return;
            }

            // Agrupar por admin asignado
            var conversationsByAdmin = conversationsWithUnread
                .Where(c => c.AssignedUserId != null)
                .GroupBy(c => c.AssignedUserId);

            foreach (var group in conversationsByAdmin)
            {
                var adminId = group.Key;
                var conversations = group.ToList();

                // Verificar si el admin está offline
                if (recentlyActiveAdminIds.Contains(adminId))
                {
                    _logger.LogDebug($"👤 Admin {adminId} está online, no enviar email");
                    continue;
                }

                // Obtener email del admin
                var admin = conversations.First().AssignedUser;
                if (string.IsNullOrEmpty(admin?.Email))
                {
                    _logger.LogWarning($"⚠️ Admin {adminId} no tiene email configurado");
                    continue;
                }

                // Preparar resumen de conversaciones sin leer
                var unreadByConversation = conversations
                    .ToDictionary(c => c.Id.ToString(), c => c.UnreadAdminMessages);

                // Enviar email por lote
                await emailService.SendBatchNotificationAsync(admin.Email, unreadByConversation);

                _logger.LogInformation(
                    $"📬 Email de resumen enviado a {admin.Email}: {conversations.Count} conversaciones, " +
                    $"{unreadByConversation.Values.Sum()} mensajes sin leer");
            }
        }
    }
}
