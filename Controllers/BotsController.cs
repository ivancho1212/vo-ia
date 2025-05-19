using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Security.Claims;

namespace Voia.Api.Controllers
{
    [Authorize(Roles = "Admin,User")]
    [Route("api/[controller]")]
    [ApiController]
    public class BotsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BotsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todos los bots filtrados por nombre o estado.
        /// </summary>
        /// <param name="isActive">Filtra por estado activo o no activo.</param>
        /// <param name="name">Filtra por el nombre del bot.</param>
        /// <returns>Lista de bots.</returns>
        /// <response code="200">Devuelve una lista de bots.</response>
        /// <response code="500">Si ocurre un error interno.</response>
        [HttpGet]
        [HasPermission("CanViewBots")]
        public async Task<ActionResult<IEnumerable<Bot>>> GetBots([FromQuery] bool? isActive, [FromQuery] string? name = null)
        {
            try
            {
                var query = _context.Bots
                    .Include(b => b.User) // Incluye la relación User para que no sea null
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

        [HttpGet("me")]
        [HasPermission("CanViewBots")]
        public async Task<ActionResult<IEnumerable<Bot>>> GetMyBots()
        {
            try
            {
                var userId = int.Parse(User.FindFirst("id")!.Value);

                var bots = await _context.Bots
                    .Include(b => b.User)
                    .Where(b => b.UserId == userId && b.IsActive)
                    .ToListAsync();

                return Ok(bots);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred", Details = ex.Message });
            }
        }

        /// <summary>
        /// Crea un nuevo bot.
        /// </summary>
        /// <param name="botDto">Datos del bot a crear.</param>
        /// <returns>El bot creado.</returns>
        /// <response code="201">Devuelve el bot creado.</response>
        /// <response code="400">Si los datos son inválidos o el bot ya existe.</response>
        [HttpPost]
        [HasPermission("CanCreateBot")]
        public async Task<ActionResult<Bot>> CreateBot([FromBody] CreateBotDto botDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);

            // Verifica si ya existe un bot con el mismo nombre
            var existingBot = await _context.Bots.FirstOrDefaultAsync(b => b.Name == botDto.Name);
            if (existingBot != null)
            {
                return BadRequest(new { Message = "A bot with the same name already exists." });
            }

            var bot = new Bot
            {
                Name = botDto.Name,
                Description = botDto.Description,
                ApiKey = botDto.ApiKey,
                ModelUsed = botDto.ModelUsed,
                IsActive = botDto.IsActive,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Bots.Add(bot);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBots), new { id = bot.Id }, bot);
        }

        /// <summary>
        /// Actualiza la información de un bot existente.
        /// </summary>
        /// <param name="id">ID del bot a actualizar.</param>
        /// <param name="botDto">Datos para actualizar el bot.</param>
        /// <returns>El bot actualizado.</returns>
        /// <response code="200">Devuelve el bot actualizado.</response>
        /// <response code="404">Si el bot no se encuentra.</response>
        [HttpPut("{id}")]
        [HasPermission("CanUpdateBot")]
        public async Task<IActionResult> UpdateBot(int id, [FromBody] UpdateBotDto botDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var bot = await _context.Bots.FindAsync(id);
            if (bot == null)
                return NotFound(new { Message = "Bot not found" });

            bot.Name = botDto.Name;
            bot.Description = botDto.Description;
            bot.ApiKey = botDto.ApiKey;
            bot.ModelUsed = botDto.ModelUsed;
            bot.IsActive = botDto.IsActive;
            bot.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(bot);
        }

        /// <summary>
        /// Elimina un bot (soft delete).
        /// </summary>
        /// <param name="id">ID del bot a eliminar.</param>
        /// <returns>Mensaje de confirmación.</returns>
        /// <response code="200">Bot desactivado correctamente.</response>
        /// <response code="404">Si el bot no se encuentra.</response>
        [HttpDelete("{id}")]
        [HasPermission("CanDeleteBot")]
        public async Task<IActionResult> DeleteBot(int id)
        {
            var bot = await _context.Bots.FindAsync(id);
            if (bot == null)
                return NotFound(new { Message = "Bot not found" });

            bot.IsActive = false;
            bot.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Bot disabled (soft deleted)" });
        }

        /// <summary>
        /// Obtiene un bot por su ID.
        /// </summary>
        /// <param name="id">ID del bot.</param>
        /// <returns>Bot encontrado.</returns>
        /// <response code="200">Devuelve el bot encontrado.</response>
        /// <response code="404">Si el bot no se encuentra.</response>
        [HttpGet("{id}")]
        [HasPermission("CanViewBot")]
        public async Task<ActionResult<Bot>> GetBotById(int id)
        {
            var bot = await _context.Bots
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bot == null)
                return NotFound(new { Message = "Bot not found" });

            return Ok(bot);
        }
    }
}
