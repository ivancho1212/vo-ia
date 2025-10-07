using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Voia.Api.Models.DTOs;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TokenUsageLogsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TokenUsageLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

    [HttpGet]
    [HasPermission("CanViewTokenUsageLogs")]
    public async Task<ActionResult<IEnumerable<TokenUsageLogResponseDto>>> GetAll()
        {
            var logs = await _context.TokenUsageLogs
                .Select(x => new TokenUsageLogResponseDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    BotId = x.BotId,
                    TokensUsed = x.TokensUsed,
                    UsageDate = x.UsageDate
                })
                .ToListAsync();

            return Ok(logs);
        }

    [HttpGet("{id}")]
    [HasPermission("CanViewTokenUsageLogs")]
    public async Task<ActionResult<TokenUsageLogResponseDto>> GetById(int id)
        {
            var log = await _context.TokenUsageLogs.FindAsync(id);

            if (log == null)
                return NotFound();

            return new TokenUsageLogResponseDto
            {
                Id = log.Id,
                UserId = log.UserId,
                BotId = log.BotId,
                TokensUsed = log.TokensUsed,
                UsageDate = log.UsageDate
            };
        }

    [HttpPost]
    [HasPermission("CanEditTokenUsageLogs")]
    public async Task<ActionResult<TokenUsageLogResponseDto>> Create(TokenUsageLogCreateDto dto)
        {
            var entity = new TokenUsageLog
            {
                UserId = dto.UserId,
                BotId = dto.BotId,
                TokensUsed = dto.TokensUsed,
                UsageDate = DateTime.UtcNow
            };

            _context.TokenUsageLogs.Add(entity);
            await _context.SaveChangesAsync();

            var response = new TokenUsageLogResponseDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                BotId = entity.BotId,
                TokensUsed = entity.TokensUsed,
                UsageDate = entity.UsageDate
            };

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
        }

    [HttpDelete("{id}")]
    [HasPermission("CanDeleteTokenUsageLogs")]
    public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.TokenUsageLogs.FindAsync(id);

            if (entity == null)
                return NotFound();

            _context.TokenUsageLogs.Remove(entity);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
