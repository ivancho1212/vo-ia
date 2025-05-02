using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.BotActions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotActionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BotActionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/BotActions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BotAction>>> GetAll()
        {
            return await _context.BotActions.ToListAsync();
        }

        // GET: api/BotActions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BotAction>> GetById(int id)
        {
            var action = await _context.BotActions.FindAsync(id);
            if (action == null)
                return NotFound();

            return action;
        }

        // POST: api/BotActions
        [HttpPost]
        public async Task<ActionResult<BotAction>> Create([FromBody] CreateBotActionDto dto)
        {
            var action = new BotAction
            {
                BotId = dto.BotId,
                TriggerPhrase = dto.TriggerPhrase,
                ActionType = dto.ActionType,
                Payload = dto.Payload,
                CreatedAt = DateTime.UtcNow
            };

            _context.BotActions.Add(action);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = action.Id }, action);
        }

        // PUT: api/BotActions/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateBotActionDto dto)
        {
            var action = await _context.BotActions.FindAsync(id);
            if (action == null)
                return NotFound();

            if (dto.TriggerPhrase != null) action.TriggerPhrase = dto.TriggerPhrase;
            if (dto.ActionType != null) action.ActionType = dto.ActionType;
            if (dto.Payload != null) action.Payload = dto.Payload;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/BotActions/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var action = await _context.BotActions.FindAsync(id);
            if (action == null)
                return NotFound();

            _context.BotActions.Remove(action);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
