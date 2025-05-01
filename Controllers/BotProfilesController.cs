using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.BotProfiles;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotProfilesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BotProfilesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/bot_profiles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BotProfile>>> GetBotProfiles()
        {
            var botProfiles = await _context.BotProfiles
                .Include(b => b.Bot) // Incluimos la relación con la tabla de Bots
                .ToListAsync();
            return Ok(botProfiles);
        }

        // GET: api/bot_profiles/{bot_id}
        [HttpGet("{bot_id}")]
        public async Task<ActionResult<BotProfile>> GetBotProfileById(int bot_id)
        {
            var botProfile = await _context.BotProfiles
                .Include(b => b.Bot)
                .FirstOrDefaultAsync(b => b.BotId == bot_id);

            if (botProfile == null)
            {
                return NotFound(new { message = "Bot profile not found." });
            }

            return Ok(botProfile);
        }

        [HttpPost]
        public async Task<ActionResult<BotProfile>> CreateBotProfile([FromBody] CreateBotProfile createBotProfile)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verificar si el BotId existe en la base de datos
            var bot = await _context.Bots.FindAsync(createBotProfile.BotId);
            if (bot == null)
            {
                return BadRequest(new { message = "Bot not found." });
            }

            var botProfile = new BotProfile
            {
                BotId = createBotProfile.BotId,
                Name = createBotProfile.Name,
                AvatarUrl = createBotProfile.AvatarUrl,
                Bio = createBotProfile.Bio,
                PersonalityTraits = createBotProfile.PersonalityTraits,
                Language = createBotProfile.Language,
                Tone = createBotProfile.Tone,
                Restrictions = createBotProfile.Restrictions,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BotProfiles.Add(botProfile);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBotProfileById), new { bot_id = botProfile.BotId }, botProfile);
        }

        [HttpPut("{bot_id}")]
        public async Task<IActionResult> UpdateBotProfile(int bot_id, [FromBody] UpdateBotProfile updatedProfile)
        {
            if (bot_id != updatedProfile.BotId)
            {
                return BadRequest(new { message = "Bot ID mismatch." });
            }

            var existingProfile = await _context.BotProfiles.FirstOrDefaultAsync(b => b.BotId == bot_id);
            if (existingProfile == null)
            {
                return NotFound(new { message = "Bot profile not found." });
            }

            // Actualiza los campos permitidos
            existingProfile.Name = updatedProfile.Name;
            existingProfile.AvatarUrl = updatedProfile.AvatarUrl;
            existingProfile.Bio = updatedProfile.Bio;
            existingProfile.PersonalityTraits = updatedProfile.PersonalityTraits;
            existingProfile.Language = updatedProfile.Language;
            existingProfile.Tone = updatedProfile.Tone;
            existingProfile.Restrictions = updatedProfile.Restrictions;
            existingProfile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(existingProfile);
        }


        // DELETE: api/bot_profiles/{bot_id}
        [HttpDelete("{bot_id}")]
        public async Task<IActionResult> DeleteBotProfile(int bot_id)
        {
            var botProfile = await _context.BotProfiles.FindAsync(bot_id);
            if (botProfile == null)
            {
                return NotFound(new { message = "Bot profile not found." });
            }

            _context.BotProfiles.Remove(botProfile);
            await _context.SaveChangesAsync();

            return NoContent(); // 204 No Content
        }
    }
}
