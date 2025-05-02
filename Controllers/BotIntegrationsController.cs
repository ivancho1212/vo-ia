using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.BotIntegrations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotIntegrationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BotIntegrationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/BotIntegrations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BotIntegration>>> GetAll()
        {
            return await _context.BotIntegrations.ToListAsync();
        }

        // GET: api/BotIntegrations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BotIntegration>> GetById(int id)
        {
            var integration = await _context.BotIntegrations.FindAsync(id);
            if (integration == null)
                return NotFound();

            return integration;
        }

        // POST: api/BotIntegrations
        [HttpPost]
        public async Task<ActionResult<BotIntegration>> Create([FromBody] CreateBotIntegrationDto dto)
        {
            var integration = new BotIntegration
            {
                BotId = dto.BotId,
                IntegrationType = dto.IntegrationType ?? "widget",
                AllowedDomain = dto.AllowedDomain,
                ApiToken = dto.ApiToken,
                CreatedAt = DateTime.UtcNow
            };

            _context.BotIntegrations.Add(integration);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = integration.Id }, integration);
        }

        // PUT: api/BotIntegrations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateBotIntegrationDto dto)
        {
            var integration = await _context.BotIntegrations.FindAsync(id);
            if (integration == null)
                return NotFound();

            if (dto.IntegrationType != null) integration.IntegrationType = dto.IntegrationType;
            if (dto.AllowedDomain != null) integration.AllowedDomain = dto.AllowedDomain;
            if (dto.ApiToken != null) integration.ApiToken = dto.ApiToken;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/BotIntegrations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var integration = await _context.BotIntegrations.FindAsync(id);
            if (integration == null)
                return NotFound();

            _context.BotIntegrations.Remove(integration);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
