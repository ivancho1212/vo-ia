using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.StyleTemplate;
using Voia.Api.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Voia.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class StyleTemplatesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StyleTemplatesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [HasPermission("CanViewStyleTemplates")]
        public async Task<IActionResult> GetAll()
        {
            var templates = await _context.StyleTemplates.ToListAsync();
            return Ok(templates);
        }

        [HttpGet("{id}")]
        [HasPermission("CanViewStyleTemplates")]
        public async Task<IActionResult> GetById(int id)
        {
            var template = await _context.StyleTemplates.FindAsync(id);
            if (template == null)
                return NotFound(new { message = "Template not found." });

            return Ok(template);
        }

        [HttpPost]
        [HasPermission("CanCreateStyleTemplates")]
        public async Task<IActionResult> Create([FromBody] CreateStyleTemplateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var template = new StyleTemplate
            {
                UserId = dto.UserId,
                Name = dto.Name,
                Theme = dto.Theme,
                PrimaryColor = dto.PrimaryColor,
                SecondaryColor = dto.SecondaryColor,
                FontFamily = dto.FontFamily,
                AvatarUrl = dto.AvatarUrl,
                Position = dto.Position,
                CustomCss = dto.CustomCss
            };

            _context.StyleTemplates.Add(template);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = template.Id }, template);
        }

        [HttpPut("{id}")]
        [HasPermission("CanUpdateStyleTemplates")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStyleTemplateDto dto)
        {
            if (!ModelState.IsValid || id != dto.Id) return BadRequest(ModelState);

            var template = await _context.StyleTemplates.FindAsync(id);
            if (template == null) return NotFound(new { message = "Template not found." });

            template.Name = dto.Name;
            template.Theme = dto.Theme;
            template.PrimaryColor = dto.PrimaryColor;
            template.SecondaryColor = dto.SecondaryColor;
            template.FontFamily = dto.FontFamily;
            template.AvatarUrl = dto.AvatarUrl;
            template.Position = dto.Position;
            template.CustomCss = dto.CustomCss;
            template.UpdatedAt = DateTime.UtcNow;

            _context.StyleTemplates.Update(template);
            await _context.SaveChangesAsync();

            return Ok(template);
        }

        [HttpDelete("{id}")]
        [HasPermission("CanDeleteStyleTemplates")]
        public async Task<IActionResult> Delete(int id)
        {
            var template = await _context.StyleTemplates.FindAsync(id);
            if (template == null) return NotFound(new { message = "Template not found." });

            _context.StyleTemplates.Remove(template);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Template deleted successfully." });
        }
    }
}
