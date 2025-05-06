using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.BotProfiles;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Authorization;

namespace Voia.Api.Controllers
{
    [Authorize(Roles = "Admin,User")]
    [Route("api/[controller]")]
    [ApiController]
    public class BotProfilesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BotProfilesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todos los perfiles de bots.
        /// </summary>
        /// <returns>Lista de perfiles de bots.</returns>
        /// <response code="200">Devuelve la lista de perfiles de bots.</response>
        /// <response code="500">Si ocurre un error interno.</response>
        [HttpGet]
        [HasPermission("CanViewBotProfiles")]
        public async Task<ActionResult<IEnumerable<BotProfile>>> GetBotProfiles()
        {
            var botProfiles = await _context.BotProfiles
                .Include(b => b.Bot) // Incluimos la relación con la tabla de Bots
                .ToListAsync();
            return Ok(botProfiles);
        }

        /// <summary>
        /// Obtiene el perfil de un bot por su ID.
        /// </summary>
        /// <param name="bot_id">ID del bot para obtener su perfil.</param>
        /// <returns>El perfil del bot.</returns>
        /// <response code="200">Devuelve el perfil del bot.</response>
        /// <response code="404">Si no se encuentra el perfil del bot.</response>
        [HttpGet("{bot_id}")]
        [HasPermission("CanViewBotProfiles")]
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

        /// <summary>
        /// Crea un perfil de bot.
        /// </summary>
        /// <param name="createBotProfile">Datos para crear el perfil del bot.</param>
        /// <returns>El perfil de bot creado.</returns>
        /// <response code="200">Devuelve el perfil de bot creado.</response>
        /// <response code="400">Si el BotId no existe en la base de datos.</response>
        [HttpPost]
        [HasPermission("CanCreateBotProfiles")]
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

        /// <summary>
        /// Actualiza el perfil de un bot.
        /// </summary>
        /// <param name="bot_id">ID del bot cuyo perfil se desea actualizar.</param>
        /// <param name="updatedProfile">Datos para actualizar el perfil del bot.</param>
        /// <returns>El perfil actualizado del bot.</returns>
        /// <response code="200">Devuelve el perfil actualizado del bot.</response>
        /// <response code="404">Si no se encuentra el perfil del bot.</response>
        /// <response code="400">Si hay un desajuste de ID entre el perfil y el bot.</response>
        [HttpPut("{bot_id}")]
        [HasPermission("CanUpdateBotProfiles")]
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

        /// <summary>
        /// Elimina el perfil de un bot.
        /// </summary>
        /// <param name="bot_id">ID del bot cuyo perfil se desea eliminar.</param>
        /// <returns>Mensaje de confirmación.</returns>
        /// <response code="204">El perfil fue eliminado correctamente.</response>
        /// <response code="404">Si no se encuentra el perfil del bot.</response>
        [HttpDelete("{bot_id}")]
        [HasPermission("CanDeleteBotProfiles")]
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
