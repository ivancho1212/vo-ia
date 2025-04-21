using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BotsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Bot>>> GetBots(
           [FromQuery] bool? isActive,
           [FromQuery] string? name = null)
        {
            try
            {
                var query = _context.Bots
                                    .Include(b => b.User)  // Incluye la relación User para que no sea null
                                    .AsQueryable();

                if (isActive.HasValue)
                {
                    query = query.Where(b => b.IsActive == isActive.Value);
                }

                if (!string.IsNullOrEmpty(name))
                {
                    query = query.Where(b => b.Name.Contains(name));
                }

                var bots = await query.ToListAsync();

                if (bots.Count == 0)
                {
                    return Ok(new { Message = "No bots found" });
                }

                return Ok(bots);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred", Details = ex.Message });
            }
        }



        // POST: api/Bots
        [HttpPost]
        public async Task<ActionResult<Bot>> CreateBot([FromBody] Bot bot)
        {
            bot.CreatedAt = DateTime.UtcNow;
            bot.UpdatedAt = DateTime.UtcNow;

            _context.Bots.Add(bot);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBots), new { id = bot.Id }, bot);
        }

        // PUT: api/Bots/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBot(int id, [FromBody] Bot botUpdate)
        {
            var bot = await _context.Bots.FindAsync(id);
            if (bot == null)
                return NotFound(new { Message = "Bot not found" });

            bot.Name = botUpdate.Name;
            bot.Description = botUpdate.Description;
            bot.ApiKey = botUpdate.ApiKey;
            bot.ModelUsed = botUpdate.ModelUsed;
            bot.IsActive = botUpdate.IsActive;
            bot.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(bot);
        }

        // DELETE: api/Bots/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBot(int id)
        {
            var bot = await _context.Bots.FindAsync(id);
            if (bot == null)
                return NotFound(new { Message = "Bot not found" });

            _context.Bots.Remove(bot);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}
