using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Voia.Api.Hubs;
using Voia.Api.Services.Interfaces;
using Voia.Api.Services;
using Voia.Api.Services.Chat;
using Voia.Api.Data;
using System;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using StackExchange.Redis;

public class MessageProcessingWorker : BackgroundService
{
    private readonly ILogger<MessageProcessingWorker> _logger;
    private readonly IServiceProvider _services;
    private readonly InMemoryMessageQueue _inMemoryQueue; // optional
    private readonly RedisService? _redis;

    private const string StreamKey = "voia:message_jobs";
    private const string ConsumerGroup = "voia_workers";

    public MessageProcessingWorker(ILogger<MessageProcessingWorker> logger, IServiceProvider services, InMemoryMessageQueue queue)
    {
        _logger = logger;
        _services = services;
        _inMemoryQueue = queue;
        // try to resolve RedisService if available
        _redis = services.GetService(typeof(RedisService)) as RedisService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MessageProcessingWorker started. Redis enabled: {hasRedis}", _redis != null);

        if (_redis != null)
        {
            await RunRedisConsumerLoop(stoppingToken);
            return;
        }

        // Fallback to in-memory processing loop
        await RunInMemoryLoop(stoppingToken);
    }

    private async Task RunInMemoryLoop(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Using InMemoryMessageQueue consumer.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_inMemoryQueue.TryDequeue(out var job) && job != null)
                {
                    await ProcessJob(job, stoppingToken);
                }
                else
                {
                    await Task.Delay(500, stoppingToken);
                }
            }
            catch (OperationCanceledException) { /* shutting down */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing in-memory message job");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task RunRedisConsumerLoop(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Redis Stream consumer loop on stream {stream} group {group}", StreamKey, ConsumerGroup);
    var db = _redis!.Db;
    var mux = _redis; // to keep connection alive

        // Ensure consumer group exists
        try
        {
            // XGROUP CREATE key group $ MKSTREAM
            await db.StreamCreateConsumerGroupAsync(StreamKey, ConsumerGroup, "$");
            _logger.LogInformation("Consumer group {group} created for stream {stream}", ConsumerGroup, StreamKey);
        }
        catch (RedisServerException ex) when (ex.Message?.Contains("BUSYGROUP") == true)
        {
            _logger.LogInformation("Consumer group {group} already exists", ConsumerGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating consumer group for Redis stream");
            // If we cannot create group, fallback to in-memory
            await RunInMemoryLoop(stoppingToken);
            return;
        }

        var consumerName = Environment.MachineName + "-" + Guid.NewGuid().ToString("N");
        _logger.LogInformation("Redis consumer name: {consumer}", consumerName);

        // Consumer loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // XREADGROUP GROUP group consumer COUNT 10 BLOCK 5000 STREAMS key >
                var entries = await db.StreamReadGroupAsync(StreamKey, ConsumerGroup, consumerName, "=", count: 10, noAck: false);
                if (entries != null && entries.Length > 0)
                {
                    foreach (var entry in entries)
                    {
                        try
                        {
                            if (entry.Values.Length == 0) continue;
                            var val = entry.Values[0].Value;
                            var payload = val.HasValue ? val.ToString() : null;
                            if (string.IsNullOrEmpty(payload))
                            {
                                _logger.LogWarning("Empty payload in stream entry {id}", entry.Id);
                                await db.StreamAcknowledgeAsync(StreamKey, ConsumerGroup, new[] { entry.Id });
                                continue;
                            }

                            var job = JsonSerializer.Deserialize<MessageJob>(payload);
                            if (job == null)
                            {
                                _logger.LogWarning("Failed to deserialize job from entry {id}", entry.Id);
                                await db.StreamAcknowledgeAsync(StreamKey, ConsumerGroup, new[] { entry.Id });
                                continue;
                            }

                            _logger.LogDebug("Processing stream entry {id} conv={conv} msg={msg}", entry.Id, job.ConversationId, job.MessageId);
                            await ProcessJob(job, stoppingToken);

                            // ACK
                            await db.StreamAcknowledgeAsync(StreamKey, ConsumerGroup, new[] { entry.Id });
                        }
                        catch (Exception exEntry)
                        {
                            _logger.LogError(exEntry, "Error processing stream entry {id}", entry.Id);
                            // do not ack so it can be retried or moved to a dead-letter later
                        }
                    }
                }
                else
                {
                    // No entries, short wait
                    await Task.Delay(500, stoppingToken);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis consumer loop error, retrying with backoff");
                await Task.Delay(2000, stoppingToken);
            }
        }
    }

    private async Task ProcessJob(MessageJob job, CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var ai = scope.ServiceProvider.GetRequiredService<IAiProviderService>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();
        var vectorSearch = scope.ServiceProvider.GetService<VectorSearchService>();
        var promptBuilder = scope.ServiceProvider.GetService<PromptBuilderService>();
        var dataCaptureService = scope.ServiceProvider.GetService<BotDataCaptureService>(); // 🆕

        // Re-fetch conversation for safety
        var conversation = await db.Conversations.FindAsync(job.ConversationId);
        if (conversation == null)
        {
            _logger.LogWarning("Conversation {id} not found for job", job.ConversationId);
            return;
        }

        // Defense-in-depth: if the conversation has AI paused, skip processing the job.
        if (!conversation.IsWithAI)
        {
            _logger.LogInformation("Skipping job {job} because AI is paused for conversation {conv}", job.MessageId, job.ConversationId);
            return;
        }

        // 🆕 PASO 1: PROCESAR CAPTURA DE DATOS PRIMERO
        // Si el usuario escribió datos capturables, extraerlos y guardarlos antes de enviar a IA
        var currentCapturedFields = job.CapturedFields ?? new System.Collections.Generic.List<Voia.Api.Services.DataField>();
        
        _logger.LogInformation("🔹 [Worker] job.CapturedFields: {count} campos", currentCapturedFields.Count);
        foreach (var field in currentCapturedFields)
        {
            _logger.LogInformation("  - {fieldName}: {value}", field.FieldName, field.Value ?? "NULL");
        }
        
        if (dataCaptureService != null && currentCapturedFields.Any())
        {
            _logger.LogInformation("🔍 [MessageProcessingWorker] Procesando captura de datos - Mensaje: {msg}, Campos pendientes: {count}", 
                job.Question, currentCapturedFields.Where(f => string.IsNullOrEmpty(f.Value)).Count());
            
            try
            {
                var captureResult = await dataCaptureService.ProcessMessageAsync(
                    job.BotId,
                    job.UserId,
                    job.ConversationId.ToString(), // Usar conversationId como sessionId
                    job.Question,
                    currentCapturedFields
                );

                _logger.LogInformation("📊 [Worker] Resultado de captura: {newSubmissions} nuevos, RequiresAiClarification: {requiresClarification}", 
                    captureResult.NewSubmissions.Count, captureResult.RequiresAiClarification);

                // 🆕 Actualizar campos con los nuevos datos capturados
                if (captureResult.NewSubmissions.Any())
                {
                    _logger.LogInformation("✅ [MessageProcessingWorker] Se capturaron {count} nuevos datos", captureResult.NewSubmissions.Count);
                    
                    // Aplicar los nuevos datos a la lista de campos para el próximo prompt
                    foreach (var submission in captureResult.NewSubmissions)
                    {
                        _logger.LogInformation("  📝 Submission: CaptureFieldId={id}, Value='{value}', CaptureField.FieldName='{fieldName}'", 
                            submission.CaptureFieldId, submission.SubmissionValue, submission.CaptureField?.FieldName);
                        
                        var field = currentCapturedFields.FirstOrDefault(f => f.FieldName == submission.CaptureField?.FieldName);
                        if (field != null)
                        {
                            field.Value = submission.SubmissionValue;
                            _logger.LogInformation("✅ Campo actualizado: {fieldName} = {value}", field.FieldName, submission.SubmissionValue);
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ No se encontró campo en memoria para: {fieldName}", submission.CaptureField?.FieldName);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("ℹ️ No se capturaron nuevos datos");
                }

                // Si requiere aclaración de la IA, procesar eso
                if (captureResult.RequiresAiClarification)
                {
                    _logger.LogInformation("🤔 [MessageProcessingWorker] La captura requiere aclaración de IA");
                    job.Question = captureResult.AiClarificationPrompt; // Usar la pregunta de aclaración
                }
            }
            catch (Exception capEx)
            {
                _logger.LogWarning(capEx, "Error en servicio de captura de datos para job {job}", job.MessageId);
                // Continuar sin fallo - la captura es adicional
            }
        }
        else
        {
            if (dataCaptureService == null)
                _logger.LogWarning("⚠️ dataCaptureService es NULL");
            else
                _logger.LogInformation("ℹ️ No hay campos para procesar (currentCapturedFields vacío o nulo)");
        }

        // 0) Optional: perform a vector search to decide whether we have relevant context
        string prompt = job.Question;  // 🆕 Declarar prompt aquí para que sea accesible más abajo
        
        try
        {
            _logger.LogInformation("📍 [MessageProcessingWorker] Processing job {jobId} - Location: {city}, {country} | Context: {context}", job.MessageId, job.UserCity, job.UserCountry, job.ContextMessage);
            
            var vectors = new System.Collections.Generic.List<object>();
            if (vectorSearch != null)
            {
                try
                {
                    vectors = await vectorSearch.SearchVectorsAsync(job.BotId, job.Question);
                }
                catch (Exception vex)
                {
                    _logger.LogWarning(vex, "Vector search failed for bot {bot} conv {conv}", job.BotId, job.ConversationId);
                    vectors = new System.Collections.Generic.List<object>();
                }
            }

            // 🆕 PASO 2: CONSTRUIR PROMPT CON CAMPOS CAPTURADOS ACTUALIZADOS
            try
            {
                if (promptBuilder != null)
                {
                    try
                    {
                        // 🆕 Pasar los campos capturados actualizados en lugar de lista vacía
                        prompt = await promptBuilder.BuildPromptFromBotContextAsync(
                            job.BotId, 
                            job.UserId ?? 0, 
                            job.Question, 
                            currentCapturedFields,  // 🆕 Usar campos actuales (con datos ya capturados)
                            job.UserCountry, 
                            job.UserCity, 
                            job.ContextMessage
                        );
                        _logger.LogInformation("✅ [MessageProcessingWorker] Prompt built successfully - Location data + Capture fields ({fieldCount} campos)", 
                            currentCapturedFields.Count);
                    }
                    catch (Exception pex)
                    {
                        _logger.LogWarning(pex, "PromptBuilder failed for job {job}, falling back to raw question", job.MessageId);
                        prompt = job.Question;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while building prompt for job {job}", job.MessageId);
                prompt = job.Question;
            }

            // Ahora validar vectores para decidir si responder con IA o con "no access"
            if (vectors == null || vectors.Count == 0)
            {
                // No relevant vectors -> avoid hallucination, reply with a safe message
                _logger.LogInformation("No vectors found for bot {bot} conv {conv}. Sending 'no access' reply.", job.BotId, job.ConversationId);

                var noAccessText = "Lo siento, no tengo acceso a información relevante para responder a eso en este momento.";
                var noAccessMessage = new Voia.Api.Models.Messages.Message {
                    BotId = job.BotId,
                    UserId = job.UserId,
                    PublicUserId = conversation.PublicUserId,
                    ConversationId = job.ConversationId,
                    Sender = "bot",
                    MessageText = noAccessText,
                    Source = "widget",
                    CreatedAt = DateTime.UtcNow
                };
                db.Messages.Add(noAccessMessage);
                await db.SaveChangesAsync(cancellationToken);

                // 🆕 Actualizar campos de la conversación cuando no hay acceso
                conversation.BotResponse = noAccessText;
                conversation.LastMessage = noAccessText;
                conversation.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation($"✅ [Worker] Conversación actualizada con mensaje de 'no access'");

                await hubContext.Clients.Group(job.ConversationId.ToString()).SendAsync("ReceiveMessage", new
                {
                    conversationId = job.ConversationId,
                    from = "bot",
                    text = noAccessText,
                    timestamp = noAccessMessage.CreatedAt,
                    id = noAccessMessage.Id
                }, cancellationToken: cancellationToken);

                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during vector-check for job {job}", job.MessageId);
            // Continue to attempt AI call as a best-effort fallback
        }

        // prompt ya fue construido arriba con la ubicación y campos capturados
        
        string botAnswer;
        try
        {
            _logger.LogInformation("📤 [MessageProcessingWorker] PROMPT BEING SENT TO AI:\n{prompt}", prompt);
            botAnswer = await ai.GetBotResponseAsync(job.BotId, job.UserId ?? 0, prompt, currentCapturedFields); // 🆕 Pasar campos capturados
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI provider error for conversation {id}", job.ConversationId);
            botAnswer = "⚠️ Error al procesar la respuesta.";
        }

        var botMessage = new Voia.Api.Models.Messages.Message {
            BotId = job.BotId,
            UserId = job.UserId,
            PublicUserId = conversation.PublicUserId,
            ConversationId = job.ConversationId,
            Sender = "bot",
            MessageText = botAnswer,
            Source = "widget",
            CreatedAt = DateTime.UtcNow
        };
        db.Messages.Add(botMessage);
        await db.SaveChangesAsync(cancellationToken);

        // 🆕 Actualizar campos de la conversación con la respuesta del bot
        conversation.BotResponse = botAnswer;
        conversation.LastMessage = botAnswer;
        conversation.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation($"✅ [Worker] Conversación actualizada - bot_response y last_message");

        await hubContext.Clients.Group(job.ConversationId.ToString()).SendAsync("ReceiveMessage", new
        {
            conversationId = job.ConversationId,
            from = "bot",
            text = botAnswer,
            timestamp = botMessage.CreatedAt,
            id = botMessage.Id
        }, cancellationToken: cancellationToken);
        // Notify any connected admins in real-time about this bot response so admin panels update immediately
        try
        {
            await hubContext.Clients.Group("admin").SendAsync("NewConversationOrMessage", new
            {
                id = botMessage.Id,
                conversationId = botMessage.ConversationId,
                text = botMessage.MessageText,
                from = "bot",
                fromRole = "bot",
                timestamp = botMessage.CreatedAt,
                userId = botMessage.UserId,
                publicUserId = botMessage.PublicUserId
            }, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify admins about bot message {MessageId} for conv {ConversationId}", botMessage.Id, botMessage.ConversationId);
        }
    }
}
