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
    public class SupportResponsesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SupportResponsesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todas las respuestas de soporte.
        /// </summary>
        /// <returns>Lista de respuestas de soporte.</returns>
        /// <response code="200">Devuelve la lista de respuestas de soporte.</response>
        /// <response code="500">Si ocurre un error interno.</response>
        [HttpGet]
        [HasPermission("CanViewSupportResponses")]
        public async Task<ActionResult<IEnumerable<SupportResponse>>> GetSupportResponses()
        {
            var responses = await _context.SupportResponses.ToListAsync();
            return Ok(responses);
        }

        /// <summary>
        /// Crea una nueva respuesta de soporte.
        /// </summary>
        /// <param name="dto">Datos de la nueva respuesta de soporte.</param>
        /// <returns>La respuesta de soporte recién creada.</returns>
        /// <response code="201">La respuesta de soporte fue creada exitosamente.</response>
        /// <response code="400">Si los datos no son válidos o faltan.</response>
        [HttpPost]
        [HasPermission("CanCreateSupportResponses")]
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

            return CreatedAtAction(nameof(GetSupportResponses), new { id = response.Id }, response);
        }

        /// <summary>
        /// Actualiza una respuesta de soporte existente.
        /// </summary>
        /// <param name="id">ID de la respuesta de soporte a actualizar.</param>
        /// <param name="dto">Datos de la respuesta actualizada.</param>
        /// <returns>Resultado de la actualización.</returns>
        /// <response code="200">La respuesta de soporte fue actualizada exitosamente.</response>
        /// <response code="400">Si los datos de la respuesta son inválidos.</response>
        /// <response code="404">Si la respuesta de soporte no se encuentra.</response>
        [HttpPut("{id}")]
        [HasPermission("CanUpdateSupportResponses")]
        public async Task<IActionResult> UpdateSupportResponse(int id, UpdateSupportResponseDto dto)
        {
            var response = await _context.SupportResponses.FindAsync(id);
            if (response == null)
                return NotFound(new { message = "Support response not found." });

            if (dto.ResponderId.HasValue)
            {
                var userExists = await _context.Users.AnyAsync(u => u.Id == dto.ResponderId.Value);
                if (!userExists)
                    return BadRequest(new { message = $"Responder with ID {dto.ResponderId} does not exist." });
            }

            // Actualizamos los campos permitidos
            response.ResponderId = dto.ResponderId;
            response.Message = dto.Message ?? response.Message;

            _context.SupportResponses.Update(response);
            await _context.SaveChangesAsync();

            return NoContent(); // 204 No Content
        }

        /// <summary>
        /// Elimina una respuesta de soporte por su ID.
        /// </summary>
        /// <param name="id">ID de la respuesta de soporte a eliminar.</param>
        /// <returns>Resultado de la eliminación.</returns>
        /// <response code="204">La respuesta de soporte fue eliminada exitosamente.</response>
        /// <response code="404">Si la respuesta no se encuentra.</response>
        [HttpDelete("{id}")]
        [HasPermission("CanDeleteSupportResponses")]
        public async Task<IActionResult> DeleteSupportResponse(int id)
        {
            var response = await _context.SupportResponses.FindAsync(id);
            if (response == null)
                return NotFound(new { message = "Support response not found." });

            _context.SupportResponses.Remove(response);
            await _context.SaveChangesAsync();

            return NoContent(); // 204 No Content
        }
    }
}
