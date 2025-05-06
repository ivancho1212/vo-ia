using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.GeneratedImages;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;

namespace Voia.Api.Controllers
{
    [Authorize(Roles = "Admin,User")]
    [Route("api/[controller]")]
    [ApiController]
    public class GeneratedImagesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GeneratedImagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todas las imágenes generadas.
        /// </summary>
        /// <returns>Lista de imágenes generadas.</returns>
        /// <response code="200">Devuelve una lista de todas las imágenes generadas.</response>
        /// <response code="500">Si ocurre un error interno.</response>
        [HttpGet]
        [HasPermission("CanViewGeneratedImages")]
        public async Task<ActionResult<IEnumerable<GeneratedImage>>> GetAll()
        {
            var images = await _context.GeneratedImages.ToListAsync();
            return Ok(images);
        }

        /// <summary>
        /// Obtiene una imagen generada por su ID.
        /// </summary>
        /// <param name="id">ID de la imagen a obtener.</param>
        /// <returns>Imagen generada.</returns>
        /// <response code="200">Devuelve la imagen generada.</response>
        /// <response code="404">Si no se encuentra la imagen.</response>
        [HttpGet("{id}")]
        [HasPermission("CanViewGeneratedImages")]
        public async Task<ActionResult<GeneratedImage>> GetById(int id)
        {
            var image = await _context.GeneratedImages.FindAsync(id);
            if (image == null)
                return NotFound(new { message = "Image not found." });

            return Ok(image);
        }

        /// <summary>
        /// Crea una nueva imagen generada.
        /// </summary>
        /// <param name="dto">Datos necesarios para crear la imagen.</param>
        /// <returns>La imagen generada.</returns>
        /// <response code="201">Devuelve la imagen recién creada.</response>
        /// <response code="400">Si los datos de entrada son inválidos.</response>
        [HttpPost]
        [HasPermission("CanCreateGeneratedImages")]
        public async Task<ActionResult<GeneratedImage>> Create([FromBody] CreateGeneratedImageDto dto)
        {
            var image = new GeneratedImage
            {
                UserId = dto.UserId,
                BotId = dto.BotId,
                Prompt = dto.Prompt,
                ImageUrl = dto.ImageUrl,
                CreatedAt = DateTime.UtcNow
            };

            _context.GeneratedImages.Add(image);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = image.Id }, image);
        }

        /// <summary>
        /// Actualiza una imagen generada existente.
        /// </summary>
        /// <param name="id">ID de la imagen que se desea actualizar.</param>
        /// <param name="dto">Datos actualizados de la imagen.</param>
        /// <returns>Resultado de la actualización.</returns>
        /// <response code="204">Imagen actualizada correctamente.</response>
        /// <response code="404">Si la imagen no existe.</response>
        [HttpPut("{id}")]
        [HasPermission("CanUpdateGeneratedImages")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateGeneratedImageDto dto)
        {
            var existing = await _context.GeneratedImages.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Image not found." });

            existing.Prompt = dto.Prompt ?? existing.Prompt;
            existing.ImageUrl = dto.ImageUrl ?? existing.ImageUrl;

            _context.GeneratedImages.Update(existing);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Elimina una imagen generada por su ID.
        /// </summary>
        /// <param name="id">ID de la imagen a eliminar.</param>
        /// <returns>Resultado de la eliminación.</returns>
        /// <response code="204">Imagen eliminada correctamente.</response>
        /// <response code="404">Si la imagen no existe.</response>
        [HttpDelete("{id}")]
        [HasPermission("CanDeleteGeneratedImages")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _context.GeneratedImages.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Image not found." });

            _context.GeneratedImages.Remove(existing);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
