using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.TrainingDataSessions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Voia.Api.Controllers
{
    [Authorize(Roles = "Admin, User")]
    [Route("api/[controller]")]
    [ApiController]
    public class TrainingDataSessionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TrainingDataSessionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todas las sesiones de datos de entrenamiento.
        /// </summary>
        /// <returns>Lista de sesiones de datos de entrenamiento.</returns>
        /// <response code="200">Devuelve la lista de sesiones de datos de entrenamiento.</response>
        /// <response code="404">Si no se encuentran sesiones.</response>
        [HttpGet]
        [HasPermission("CanViewTrainingDataSessions")]
        public async Task<ActionResult<IEnumerable<ReadTrainingDataSessionDto>>> GetAll()
        {
            var sessions = await _context.TrainingDataSessions.ToListAsync();

            var result = sessions.ConvertAll(s => new ReadTrainingDataSessionDto
            {
                Id = s.Id,
                UserId = s.UserId,
                BotId = s.BotId,
                DataSummary = s.DataSummary,
                DataType = s.DataType,
                Status = s.Status,
                CreatedAt = s.CreatedAt
            });

            return Ok(result);
        }

        /// <summary>
        /// Obtiene una sesión de datos de entrenamiento por su ID.
        /// </summary>
        /// <param name="id">ID de la sesión de datos de entrenamiento.</param>
        /// <returns>La sesión de datos de entrenamiento encontrada.</returns>
        /// <response code="200">Devuelve la sesión de datos de entrenamiento.</response>
        /// <response code="404">Si no se encuentra la sesión con el ID especificado.</response>
        [HttpGet("{id}")]
        [HasPermission("CanViewTrainingDataSessions")]
        public async Task<ActionResult<ReadTrainingDataSessionDto>> GetById(int id)
        {
            var session = await _context.TrainingDataSessions.FindAsync(id);
            if (session == null) return NotFound(new { message = "Session not found." });

            return Ok(new ReadTrainingDataSessionDto
            {
                Id = session.Id,
                UserId = session.UserId,
                BotId = session.BotId,
                DataSummary = session.DataSummary,
                DataType = session.DataType,
                Status = session.Status,
                CreatedAt = session.CreatedAt
            });
        }

        /// <summary>
        /// Crea una nueva sesión de datos de entrenamiento.
        /// </summary>
        /// <param name="dto">Datos de la nueva sesión de datos de entrenamiento.</param>
        /// <returns>La sesión de datos de entrenamiento creada.</returns>
        /// <response code="201">La sesión fue creada exitosamente.</response>
        /// <response code="400">Si los datos proporcionados no son válidos.</response>
        [HttpPost]
        [HasPermission("CanCreateTrainingDataSessions")]
        public async Task<ActionResult<ReadTrainingDataSessionDto>> Create([FromBody] CreateTrainingDataSessionDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Data is required" });
            }

            var session = new TrainingDataSession
            {
                UserId = dto.UserId,
                BotId = dto.BotId,
                DataSummary = dto.DataSummary,
                DataType = dto.DataType,
                Status = dto.Status,
                CreatedAt = DateTime.UtcNow
            };

            _context.TrainingDataSessions.Add(session);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = session.Id }, session);
        }

        /// <summary>
        /// Actualiza una sesión de datos de entrenamiento existente.
        /// </summary>
        /// <param name="id">ID de la sesión de datos de entrenamiento a actualizar.</param>
        /// <param name="dto">Datos de la actualización de la sesión.</param>
        /// <returns>Resultado de la actualización.</returns>
        /// <response code="204">La sesión fue actualizada exitosamente.</response>
        /// <response code="404">Si no se encuentra la sesión con el ID especificado.</response>
        [HttpPut("{id}")]
        [HasPermission("CanUpdateTrainingDataSessions")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTrainingDataSessionDto dto)
        {
            var session = await _context.TrainingDataSessions.FindAsync(id);
            if (session == null) return NotFound(new { message = "Session not found." });

            session.DataSummary = dto.DataSummary ?? session.DataSummary;
            session.DataType = dto.DataType ?? session.DataType;
            session.Status = dto.Status ?? session.Status;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Elimina una sesión de datos de entrenamiento por su ID.
        /// </summary>
        /// <param name="id">ID de la sesión de datos de entrenamiento a eliminar.</param>
        /// <returns>Resultado de la eliminación.</returns>
        /// <response code="204">La sesión fue eliminada exitosamente.</response>
        /// <response code="404">Si no se encuentra la sesión con el ID especificado.</response>
        [HttpDelete("{id}")]
        [HasPermission("CanDeleteTrainingDataSessions")]
        public async Task<IActionResult> Delete(int id)
        {
            var session = await _context.TrainingDataSessions.FindAsync(id);
            if (session == null) return NotFound(new { message = "Session not found." });

            _context.TrainingDataSessions.Remove(session);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
