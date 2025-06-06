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
    // [Authorize(Roles = "Admin,User")]
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
        public async Task<ActionResult<IEnumerable<BotStyle>>> GetAllStyles()
        {
            try
            {
                var styles = await _context.BotStyles.ToListAsync();
                return Ok(styles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace });
            }
        }


        /// <summary>
        /// Obtiene el estilo de un bot por su ID.
        /// </summary>
        /// <param name="botId">ID del bot para obtener su estilo.</param>
        /// <returns>El estilo del bot.</returns>
        /// <response code="200">Devuelve el estilo del bot.</response>
        /// <response code="404">Si no se encuentra el estilo para el bot.</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<BotStyle>> GetStyleById(int id)
        {
            var style = await _context.BotStyles.FindAsync(id);

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
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStyle(int id, [FromBody] UpdateBotStyleDto dto)
        {
            var style = await _context.BotStyles.FindAsync(id);

            if (style == null)
            {
                return NotFound(new { message = "Style not found" });
            }

            style.StyleTemplateId = dto.StyleTemplateId;
            style.Theme = dto.Theme;
            style.PrimaryColor = dto.PrimaryColor;
            style.SecondaryColor = dto.SecondaryColor;
            style.FontFamily = dto.FontFamily;
            style.AvatarUrl = dto.AvatarUrl;
            style.Position = dto.Position;
            style.CustomCss = dto.CustomCss;
            style.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(style);
        }



        /// <summary>
        /// Crea un nuevo estilo para un bot.
        /// </summary>
        /// <param name="dto">Datos para crear el estilo del bot.</param>
        /// <returns>El estilo creado.</returns>
        /// <response code="200">Devuelve el estilo creado.</response>
        /// <response code="400">Si el BotId no existe en la base de datos.</response>
        [HttpPost]
        public async Task<IActionResult> CreateStyle([FromBody] CreateBotStyleDto dto)
        {
            var style = new BotStyle
            {
                StyleTemplateId = dto.StyleTemplateId,
                Theme = dto.Theme,
                PrimaryColor = dto.PrimaryColor,
                SecondaryColor = dto.SecondaryColor,
                FontFamily = dto.FontFamily,
                AvatarUrl = dto.AvatarUrl,
                Position = dto.Position,
                CustomCss = dto.CustomCss,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BotStyles.Add(style);
            await _context.SaveChangesAsync();

            return Ok(style);
        }



        /// <summary>
        /// Elimina un estilo de bot.
        /// </summary>
        /// <param name="botId">ID del bot cuyo estilo se desea eliminar.</param>
        /// <returns>Mensaje de confirmación.</returns>
        /// <response code="200">El estilo fue eliminado correctamente.</response>
        /// <response code="404">Si no se encuentra el estilo del bot.</response>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStyle(int id)
        {
            var style = await _context.BotStyles.FindAsync(id);

            if (style == null)
            {
                return NotFound(new { message = "Style not found" });
            }

            _context.BotStyles.Remove(style);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Style deleted successfully" });
        }

    }
}

