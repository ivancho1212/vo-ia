using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.GeneratedImages;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GeneratedImagesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GeneratedImagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/GeneratedImages
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GeneratedImage>>> GetAll()
        {
            return await _context.GeneratedImages.ToListAsync();
        }

        // GET: api/GeneratedImages/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GeneratedImage>> GetById(int id)
        {
            var image = await _context.GeneratedImages.FindAsync(id);
            if (image == null)
                return NotFound(new { message = "Image not found." });

            return image;
        }

        // POST: api/GeneratedImages
        [HttpPost]
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

        // PUT: api/GeneratedImages/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateGeneratedImageDto dto)
        {
            var existing = await _context.GeneratedImages.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Image not found." });

            existing.Prompt = dto.Prompt;
            existing.ImageUrl = dto.ImageUrl;

            _context.GeneratedImages.Update(existing);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/GeneratedImages/5
        [HttpDelete("{id}")]
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
