using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Voia.Api.Data;
using Voia.Api.Models.BotConversation;
using Voia.Api.Models.Conversations;
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
    /// Envía una pregunta al bot y devuelve la respuesta.
    /// </summary>
    [HttpPost("ask")]
    public async Task<IActionResult> AskQuestion([FromBody] AskBotRequestDto dto)
    {
        if (dto.UserId <= 0)
        {
            return BadRequest(new
            {
                success = false,
                error = "UserId inválido"
            });
        }

        var bot = await _context.Bots.FirstOrDefaultAsync(b => b.Id == dto.BotId);
        if (bot == null)
        {
            return NotFound(new
            {
                success = false,
                error = "Bot no encontrado"
            });
        }

        var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
        if (!userExists)
        {
            return BadRequest(new
            {
                success = false,
                error = "Usuario no encontrado"
            });
        }

        var response = await _aiProviderService.GetBotResponseAsync(bot.Id, dto.UserId, dto.Question);

        if (string.IsNullOrWhiteSpace(response))
            response = "Lo siento, no pude generar una respuesta en este momento.";

        var conversation = new Conversation
        {
            BotId = bot.Id,
            UserId = dto.UserId,
            Title = dto.Question,
            UserMessage = dto.Question,
            BotResponse = response,
            CreatedAt = DateTime.UtcNow,
            User = null,
        };

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            question = dto.Question,
            answer = response
        });
    }
}
