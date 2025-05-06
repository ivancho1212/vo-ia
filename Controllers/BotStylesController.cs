using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voia.Api.Models;
using Voia.Api.Models.DTOs;
using Voia.Api.Data;
using Microsoft.AspNetCore.Authorization;

namespace Voia.Api.Controllers
{
    [Authorize(Roles = "Admin,User")]
    [Route("api/[controller]")]
    [ApiController]
    public class BotStylesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BotStylesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todos los estilos de los bots.
        /// </summary>
        /// <returns>Lista de estilos de bots.</returns>
        /// <response code="200">Devuelve la lista de estilos de bots.</response>
        /// <response code="500">Si ocurre un error interno.</response>
        [HttpGet]
        [HasPermission("CanViewBotStyles")]
        public async Task<ActionResult<IEnumerable<BotStyle>>> GetAllBotStyles()
        {
            var styles = await _context.BotStyles.ToListAsync();
            return Ok(styles);
        }

        /// <summary>
        /// Obtiene el estilo de un bot por su ID.
        /// </summary>
        /// <param name="botId">ID del bot para obtener su estilo.</param>
        /// <returns>El estilo del bot.</returns>
        /// <response code="200">Devuelve el estilo del bot.</response>
        /// <response code="404">Si no se encuentra el estilo para el bot.</response>
        [HttpGet("{botId}")]
        [HasPermission("CanViewBotStyles")]
        public async Task<ActionResult<BotStyle>> GetBotStyleById(int botId)
        {
            var style = await _context.BotStyles.FirstOrDefaultAsync(s => s.BotId == botId);
            
            if (style == null)
            {
                return NotFound(new { message = "Style not found" });
            }

            return Ok(style);
        }

        /// <summary>
        /// Actualiza el estilo de un bot existente.
        /// </summary>
        /// <param name="botId">ID del bot cuyo estilo se desea actualizar.</param>
        /// <param name="dto">Datos para actualizar el estilo del bot.</param>
        /// <returns>El estilo actualizado del bot.</returns>
        /// <response code="200">Devuelve el estilo actualizado.</response>
        /// <response code="404">Si no se encuentra el estilo del bot.</response>
        [HttpPut("{botId}")]
        [HasPermission("CanUpdateBotStyles")]
        public async Task<IActionResult> UpdateBotStyle(int botId, [FromBody] UpdateBotStyleDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var style = await _context.BotStyles.FirstOrDefaultAsync(s => s.BotId == botId);
            if (style == null)
            {
                return NotFound(new { message = "Style not found" });
            }

            try
            {
                style.Theme = dto.Theme;
                style.PrimaryColor = dto.PrimaryColor;
                style.SecondaryColor = dto.SecondaryColor;
                style.FontFamily = dto.FontFamily;
                style.AvatarUrl = dto.AvatarUrl;
                style.Position = dto.Position;
                style.CustomCss = dto.CustomCss;
                style.UpdatedAt = DateTime.UtcNow;

                _context.Entry(style).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(style);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the style", error = ex.Message });
            }
        }

        /// <summary>
        /// Crea un nuevo estilo para un bot.
        /// </summary>
        /// <param name="dto">Datos para crear el estilo del bot.</param>
        /// <returns>El estilo creado.</returns>
        /// <response code="200">Devuelve el estilo creado.</response>
        /// <response code="400">Si el BotId no existe en la base de datos.</response>
        [HttpPost]
        [HasPermission("CanCreateBotStyles")]
        public async Task<IActionResult> CreateBotStyle([FromBody] CreateBotStyleDto dto)
        {
            // Verificar si el BotId existe en la tabla Bots
            var botExists = await _context.Bots.AnyAsync(b => b.Id == dto.BotId);
            if (!botExists)
            {
                return BadRequest(new { message = "El BotId proporcionado no existe en la tabla de bots." });
            }

            var botStyle = new BotStyle
            {
                BotId = dto.BotId,
                Theme = dto.Theme,
                PrimaryColor = dto.PrimaryColor,
                SecondaryColor = dto.SecondaryColor,
                FontFamily = dto.FontFamily,
                AvatarUrl = dto.AvatarUrl,
                Position = dto.Position,
                CustomCss = dto.CustomCss
            };

            _context.BotStyles.Add(botStyle);
            await _context.SaveChangesAsync();

            return Ok(botStyle);
        }

        /// <summary>
        /// Elimina un estilo de bot.
        /// </summary>
        /// <param name="botId">ID del bot cuyo estilo se desea eliminar.</param>
        /// <returns>Mensaje de confirmación.</returns>
        /// <response code="200">El estilo fue eliminado correctamente.</response>
        /// <response code="404">Si no se encuentra el estilo del bot.</response>
        [HttpDelete("{botId}")]
        [HasPermission("CanDeleteBotStyles")]
        public async Task<IActionResult> DeleteBotStyle(int botId)
        {
            var style = await _context.BotStyles.FirstOrDefaultAsync(s => s.BotId == botId);
            
            if (style == null)
            {
                return NotFound(new { message = "Style not found" });
            }

            try
            {
                _context.BotStyles.Remove(style);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Style deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the style", error = ex.Message });
            }
        }
    }
}
