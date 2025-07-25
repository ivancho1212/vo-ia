using Microsoft.AspNetCore.SignalR; // ✅ Añadir
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Voia.Api.Data;
using Voia.Api.Hubs; // ✅ Añadir

public class InactiveConversationWorker : BackgroundService
{
    private readonly ILogger<InactiveConversationWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<ChatHub> _hubContext; // ✅ Añadir

    // ✅ Modificar constructor
    public InactiveConversationWorker(ILogger<InactiveConversationWorker> logger, IServiceProvider serviceProvider, IHubContext<ChatHub> hubContext)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🧹 Servicio de conversaciones inactivas iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var inactivityThreshold = DateTime.UtcNow.AddMinutes(-15);

                var inactiveConversations = dbContext.Conversations
                    .Where(c => c.Status == "activa" && c.LastActiveAt < inactivityThreshold)
                    .ToList();

                if (inactiveConversations.Any())
                {
                    _logger.LogInformation($"Encontradas {inactiveConversations.Count} conversaciones para marcar como inactivas.");
                    foreach (var conv in inactiveConversations)
                    {
                        conv.Status = "inactiva";
                        // ✅ ENVIAR NOTIFICACIÓN AL PANEL DE ADMIN
                        await _hubContext.Clients.Group("admin").SendAsync("ConversationStatusChanged", conv.Id, "inactiva", stoppingToken);
                    }
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }
        }
    }
}