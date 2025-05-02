using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.SupportTicket;
using Voia.Api.DTOs;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupportResponsesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SupportResponsesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/SupportResponses
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SupportResponse>>> GetSupportResponses()
        {
            return await _context.SupportResponses.ToListAsync();
        }

        // POST: api/SupportResponses
        [HttpPost]
        public async Task<ActionResult<SupportResponse>> CreateSupportResponse(CreateSupportResponseDto dto)
        {
            var response = new SupportResponse
            {
                TicketId = dto.TicketId,
                ResponderId = dto.ResponderId,
                Message = dto.Message,
                CreatedAt = DateTime.Now
            };

            _context.SupportResponses.Add(response);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSupportResponses), null, response);
        }

        // PUT: api/SupportResponses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupportResponse(int id, UpdateSupportResponseDto dto)
        {
            var response = await _context.SupportResponses.FindAsync(id);
            if (response == null)
                return NotFound();

            if (dto.ResponderId.HasValue)
            {
                var userExists = await _context.Users.AnyAsync(u => u.Id == dto.ResponderId.Value);
                if (!userExists)
                    return BadRequest($"Responder with ID {dto.ResponderId} does not exist.");
            }

            // Actualizamos los campos permitidos
            response.ResponderId = dto.ResponderId;
            response.Message = dto.Message ?? response.Message;

            _context.SupportResponses.Update(response);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        // DELETE: api/SupportResponses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupportResponse(int id)
        {
            var response = await _context.SupportResponses.FindAsync(id);
            if (response == null)
                return NotFound();

            _context.SupportResponses.Remove(response);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
