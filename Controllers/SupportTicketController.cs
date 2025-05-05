using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.SupportTicket;
using Voia.Api.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Voia.Api.Controllers
{
    [Authorize(Roles = "Admin, Support")]
    [Route("api/[controller]")]
    [ApiController]
    public class SupportTicketsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SupportTicketsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todos los tickets de soporte.
        /// </summary>
        /// <returns>Lista de tickets de soporte.</returns>
        /// <response code="200">Devuelve la lista de tickets de soporte.</response>
        /// <response code="404">Si no hay tickets encontrados.</response>
        [HttpGet]
        [HasPermission("CanViewSupportTickets")]
        public async Task<IActionResult> GetSupportTickets()
        {
            var tickets = await _context.SupportTickets.ToListAsync();

            if (tickets == null || tickets.Count == 0)
            {
                return NotFound(new { message = "No tickets found." });
            }

            return Ok(tickets);
        }

        /// <summary>
        /// Crea un nuevo ticket de soporte.
        /// </summary>
        /// <param name="createTicketDto">Datos del nuevo ticket de soporte.</param>
        /// <returns>El ticket de soporte recién creado.</returns>
        /// <response code="201">El ticket fue creado exitosamente.</response>
        /// <response code="400">Si los datos proporcionados no son válidos.</response>
        [HttpPost]
        [HasPermission("CanCreateSupportTickets")]
        public async Task<IActionResult> CreateSupportTicket([FromBody] CreateSupportTicketDto createTicketDto)
        {
            if (createTicketDto == null)
            {
                return BadRequest(new { message = "Ticket data is required" });
            }

            if (string.IsNullOrWhiteSpace(createTicketDto.Subject))
            {
                return BadRequest(new { message = "Subject is required." });
            }

            if (string.IsNullOrWhiteSpace(createTicketDto.Message))
            {
                return BadRequest(new { message = "Message is required." });
            }

            var ticket = new SupportTicket
            {
                UserId = createTicketDto.UserId,
                Subject = createTicketDto.Subject,
                Message = createTicketDto.Message,
                Status = createTicketDto.Status
            };

            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(CreateSupportTicket), new { id = ticket.Id }, ticket);
        }

        /// <summary>
        /// Actualiza un ticket de soporte existente.
        /// </summary>
        /// <param name="id">ID del ticket de soporte a actualizar.</param>
        /// <param name="updateDto">Datos de la actualización del ticket.</param>
        /// <returns>El ticket de soporte actualizado.</returns>
        /// <response code="200">El ticket fue actualizado exitosamente.</response>
        /// <response code="400">Si los datos proporcionados no son válidos.</response>
        /// <response code="404">Si el ticket no se encuentra.</response>
        [HttpPut("{id}")]
        [HasPermission("CanUpdateSupportTickets")]
        public async Task<IActionResult> UpdateSupportTicket(int id, [FromBody] UpdateSupportTicketDto updateDto)
        {
            if (updateDto == null || id != updateDto.Id)
            {
                return BadRequest(new { message = "Invalid ticket data." });
            }

            var ticket = await _context.SupportTickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound(new { message = "Ticket not found." });
            }

            ticket.Subject = updateDto.Subject;
            ticket.Message = updateDto.Message;
            ticket.Status = updateDto.Status;
            ticket.UpdatedAt = DateTime.Now;

            _context.SupportTickets.Update(ticket);
            await _context.SaveChangesAsync();

            return Ok(ticket);
        }

        /// <summary>
        /// Elimina un ticket de soporte por su ID.
        /// </summary>
        /// <param name="id">ID del ticket de soporte a eliminar.</param>
        /// <returns>Resultado de la eliminación.</returns>
        /// <response code="200">El ticket fue eliminado exitosamente.</response>
        /// <response code="404">Si el ticket no se encuentra.</response>
        [HttpDelete("{id}")]
        [HasPermission("CanDeleteSupportTickets")]
        public async Task<IActionResult> DeleteSupportTicket(int id)
        {
            var ticket = await _context.SupportTickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound(new { message = "Ticket not found." });
            }

            _context.SupportTickets.Remove(ticket);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Ticket deleted successfully." });
        }
    }
}
