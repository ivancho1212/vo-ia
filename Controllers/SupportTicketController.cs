using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.SupportTicket;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupportTicketsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SupportTicketsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/SupportTickets
        [HttpGet]
        public async Task<IActionResult> GetSupportTickets()
        {
            // Obtiene todos los tickets de soporte con uniones necesarias (si las hubiera)
            var tickets = await _context.SupportTickets.ToListAsync();

            // Si no se encuentran tickets, retorna un 404
            if (tickets == null || tickets.Count == 0)
            {
                return NotFound("No tickets found.");
            }

            return Ok(tickets);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSupportTicket([FromBody] CreateSupportTicketDto createTicketDto)
        {
            if (createTicketDto == null)
            {
                return BadRequest("Ticket data is required");
            }

            // Validación de campos requeridos
            if (string.IsNullOrWhiteSpace(createTicketDto.Subject))
            {
                return BadRequest("Subject is required.");
            }

            if (string.IsNullOrWhiteSpace(createTicketDto.Message))
            {
                return BadRequest("Message is required.");
            }

            var ticket = new SupportTicket
            {
                UserId = createTicketDto.UserId,
                Subject = createTicketDto.Subject,
                Message = createTicketDto.Message,
                Status = createTicketDto.Status
                // No es necesario asignar CreatedAt y UpdatedAt, ya que se gestionan automáticamente
            };

            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(CreateSupportTicket), new { id = ticket.Id }, ticket);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupportTicket(int id, [FromBody] UpdateSupportTicketDto updateDto)
        {
            if (updateDto == null || id != updateDto.Id)
            {
                return BadRequest("Invalid ticket data.");
            }

            var ticket = await _context.SupportTickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound("Ticket not found.");
            }

            ticket.Subject = updateDto.Subject;
            ticket.Message = updateDto.Message;
            ticket.Status = updateDto.Status;
            ticket.UpdatedAt = DateTime.Now;

            _context.SupportTickets.Update(ticket);
            await _context.SaveChangesAsync();

            return Ok(ticket);
        }
        // Controllers/SupportTicketsController.cs

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupportTicket(int id)
        {
            var ticket = await _context.SupportTickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound(new { message = "Ticket no encontrado." });
            }

            _context.SupportTickets.Remove(ticket);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Ticket eliminado exitosamente." });
        }

    }
}
