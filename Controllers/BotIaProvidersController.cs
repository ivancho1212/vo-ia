using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Voia.Api.Models.DTOs;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BotIaProvidersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BotIaProvidersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BotIaProviderResponseDto>>> GetAll()
        {
            var items = await _context.BotIaProviders
                .Select(x => new BotIaProviderResponseDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    ApiEndpoint = x.ApiEndpoint,
                    ApiKey = x.ApiKey,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BotIaProviderResponseDto>> GetById(int id)
        {
            var item = await _context.BotIaProviders.FindAsync(id);

            if (item == null)
                return NotFound();

            return new BotIaProviderResponseDto
            {
                Id = item.Id,
                Name = item.Name,
                ApiEndpoint = item.ApiEndpoint,
                ApiKey = item.ApiKey,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            };
        }

        [HttpPost]
        public async Task<ActionResult<BotIaProviderResponseDto>> Create(BotIaProviderCreateDto dto)
        {
            if (_context.BotIaProviders.Any(x => x.Name == dto.Name))
                return Conflict("Ya existe un proveedor IA con este nombre.");

            var entity = new BotIaProvider
            {
                Name = dto.Name,
                ApiEndpoint = dto.ApiEndpoint,
                ApiKey = dto.ApiKey,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BotIaProviders.Add(entity);
            await _context.SaveChangesAsync();

            var response = new BotIaProviderResponseDto
            {
                Id = entity.Id,
                Name = entity.Name,
                ApiEndpoint = entity.ApiEndpoint,
                ApiKey = entity.ApiKey,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, BotIaProviderUpdateDto dto)
        {
            var entity = await _context.BotIaProviders.FindAsync(id);

            if (entity == null)
                return NotFound();

            entity.Name = dto.Name;
            entity.ApiEndpoint = dto.ApiEndpoint;
            entity.ApiKey = dto.ApiKey;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.BotIaProviders.FindAsync(id);

            if (entity == null)
                return NotFound();

            _context.BotIaProviders.Remove(entity);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
