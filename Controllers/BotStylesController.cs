using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using System;
using System.Collections.Generic; // Añadir esta línea para evitar el error con IEnumerable
using System.Threading.Tasks;
using Voia.Api.Models;
using Voia.Api.Models.DTOs; 
using Voia.Api.Data; 

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotStylesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public BotStylesController(ApplicationDbContext context)        
        {
            _context = context;
        }

        // GET: api/BotStyles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BotStyle>>> GetAllBotStyles()
        {
            var styles = await _context.BotStyles.ToListAsync();
            return Ok(styles);
        }


        [HttpPut("{botId}")]
        public async Task<IActionResult> UpdateBotStyle(int botId, UpdateBotStyleDto dto)
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


        // POST: api/BotStyles
        [HttpPost]
        public async Task<IActionResult> CreateBotStyle(CreateBotStyleDto dto)
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

        // DELETE: api/BotStyles/5
        [HttpDelete("{botId}")]
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
