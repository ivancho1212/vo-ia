using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.BotConversation;
using Voia.Api.Models.Conversations;
using Voia.Api.Models.Messages;
using Voia.Api.Services.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class BotConversationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAiProviderService _aiProviderService;

    public BotConversationsController(
        ApplicationDbContext context,
        IAiProviderService aiProviderService
    )
    {
        _context = context;
        _aiProviderService = aiProviderService;
    }

    /// <summary>
    /// Envía una pregunta al bot, guarda la conversación/mensajes y devuelve la respuesta.
    /// </summary>
    [HttpPost("ask")]
    public async Task<IActionResult> AskQuestion([FromBody] AskBotRequestDto dto)
    {
        bool isPhantomMessage = dto.Meta?.InternalOnly == true;

        // Validar usuario solo si no es mensaje interno
        if (!isPhantomMessage)
        {
            if (dto.UserId <= 0)
                return BadRequest(new { success = false, error = "UserId inválido" });

            var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
            if (!userExists)
                return BadRequest(new { success = false, error = "Usuario no encontrado" });
        }

        // Validar bot
        var bot = await _context.Bots.FirstOrDefaultAsync(b => b.Id == dto.BotId);
        if (bot == null)
            return NotFound(new { success = false, error = "Bot no encontrado" });

        string response = null;

        // Solo intentar usar IA si no es phantom y hay texto
        if (!isPhantomMessage && !string.IsNullOrWhiteSpace(dto.Question))
        {
            try
            {
                response = await _aiProviderService.GetBotResponseAsync(bot.Id, dto.UserId, dto.Question);
                if (string.IsNullOrWhiteSpace(response))
                    response = "Lo siento, no pude generar una respuesta en este momento.";
            }
            catch (Exception ex)
            {
                response = "⚠️ Error al procesar el mensaje. Inténtalo más tarde.";
                Console.WriteLine("❌ Error en IA: " + ex.Message);
            }
        }

        int conversationId;

        if (dto.ConversationId.HasValue)
        {
            var existingConv = await _context.Conversations.FindAsync(dto.ConversationId.Value);
            if (existingConv == null)
                return NotFound(new { success = false, error = "Conversación no encontrada." });

            existingConv.UpdatedAt = DateTime.UtcNow;
            existingConv.LastMessage = string.IsNullOrWhiteSpace(response) ? dto.Question : response;
            conversationId = existingConv.Id;
        }
        else
        {
            var newConv = new Conversation
            {
                BotId = bot.Id,
                UserId = dto.UserId,
                Title = dto.Question?.Length > 30 ? dto.Question.Substring(0, 30) : dto.Question,
                UserMessage = dto.Question,
                BotResponse = response,
                Status = "activa",
                Blocked = false,
                LastMessage = string.IsNullOrWhiteSpace(response) ? dto.Question : response,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Conversations.Add(newConv);
            await _context.SaveChangesAsync();

            conversationId = newConv.Id;
        }

        // Solo guardar mensajes reales
        if (!isPhantomMessage && !string.IsNullOrWhiteSpace(dto.Question))
        {
            _context.Messages.Add(new Message
            {
                BotId = bot.Id,
                UserId = dto.UserId,
                ConversationId = conversationId,
                Sender = "user",
                MessageText = dto.Question,
                CreatedAt = DateTime.UtcNow,
                Source = "widget"
            });

            _context.Messages.Add(new Message
            {
                BotId = bot.Id,
                UserId = dto.UserId,
                ConversationId = conversationId,
                Sender = "bot",
                MessageText = response,
                CreatedAt = DateTime.UtcNow,
                Source = "widget"
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            conversationId = conversationId,
            question = dto.Question,
            answer = response
        });
    }
}
