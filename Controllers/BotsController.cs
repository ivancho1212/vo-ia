using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;

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
        public async Task<ActionResult<IEnumerable<Bot>>> GetBots()
        {
            return await _context.Bots.Include(b => b.User).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Bot>> GetBot(int id)
        {
            var bot = await _context.Bots.Include(b => b.User).FirstOrDefaultAsync(b => b.Id == id);

            if (bot == null)
            {
                return NotFound();
            }

            return bot;
        }

        [HttpPost]
        public async Task<ActionResult<Bot>> CreateBot(Bot bot)
        {
            _context.Bots.Add(bot);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetBot), new { id = bot.Id }, bot);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBot(int id, Bot bot)
        {
            if (id != bot.Id)
                return BadRequest();

            _context.Entry(bot).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBot(int id)
        {
            var bot = await _context.Bots.FindAsync(id);
            if (bot == null)
                return NotFound();

            _context.Bots.Remove(bot);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
