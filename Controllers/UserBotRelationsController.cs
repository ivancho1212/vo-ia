using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Voia.Api.Data;
using Voia.Api.Models.UserBotRelations;
using Microsoft.AspNetCore.Authorization;
using System;

namespace Voia.Api.Controllers
{
    [Authorize(Roles = "Admin,User")]
    [Route("api/[controller]")]
    [ApiController]
    public class UserBotRelationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserBotRelationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todas las relaciones entre usuarios y bots.
        /// </summary>
        /// <returns>Lista de relaciones entre usuarios y bots.</returns>
        /// <response code="200">Devuelve la lista de relaciones entre usuarios y bots.</response>
        [HttpGet]
        [HasPermission("CanViewUserBotRelations")]
        public async Task<ActionResult<IEnumerable<UserBotRelation>>> GetAll()
        {
            return await _context.UserBotRelations.ToListAsync();
        }

        /// <summary>
        /// Obtiene una relación específica entre usuario y bot por ID.
        /// </summary>
        /// <param name="id">ID de la relación entre el usuario y el bot.</param>
        /// <returns>Relación entre usuario y bot.</returns>
        /// <response code="200">Devuelve la relación entre usuario y bot.</response>
        /// <response code="404">Si no se encuentra la relación.</response>
        [HttpGet("{id}")]
        [HasPermission("CanViewUserBotRelations")]
        public async Task<ActionResult<UserBotRelation>> GetById(int id)
        {
            var relation = await _context.UserBotRelations.FindAsync(id);
            if (relation == null)
                return NotFound();

            return relation;
        }

        /// <summary>
        /// Obtiene todas las relaciones entre un usuario y los bots asociados.
        /// </summary>
        /// <param name="userId">ID del usuario para obtener sus relaciones con los bots.</param>
        /// <returns>Una lista de relaciones entre el usuario y los bots.</returns>
        /// <response code="200">Devuelve una lista de relaciones entre el usuario y los bots.</response>
        /// <response code="404">Si no se encuentran relaciones para el usuario.</response>
        [HttpGet("user/{userId}")]
        [HasPermission("CanViewUserBotRelations")]
        public async Task<ActionResult<IEnumerable<UserBotRelation>>> GetByUserId(int userId)
        {
            var relations = await _context.UserBotRelations
                .Where(ubr => ubr.UserId == userId)
                .ToListAsync();

            if (relations.Count == 0)
                return NotFound();

            return Ok(relations);
        }

        /// <summary>
        /// Crea una nueva relación entre un usuario y un bot.
        /// </summary>
        /// <param name="dto">Datos de la nueva relación entre usuario y bot.</param>
        /// <returns>Relación entre usuario y bot creada.</returns>
        // POST: api/UserBotRelations
        [HttpPost]
        [HasPermission("CanCreateUserBotRelation")]
        public async Task<ActionResult<UserBotRelation>> Create([FromBody] CreateUserBotRelationDto dto)
        {
            // Verificar si la relación entre el usuario y el bot ya existe
            var exists = await _context.UserBotRelations
                .AnyAsync(ubr => ubr.UserId == dto.UserId && ubr.BotId == dto.BotId);

            if (exists)
            {
                return BadRequest(new { Message = "User-Bot relation already exists" });
            }

            var relation = new UserBotRelation
            {
                UserId = dto.UserId,
                BotId = dto.BotId,
                RelationshipType = dto.RelationshipType ?? "otro",
                InteractionScore = dto.InteractionScore ?? 0,
                LastInteraction = dto.LastInteraction,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserBotRelations.Add(relation);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = relation.Id }, relation);
        }

        /// <summary>
        /// Actualiza una relación entre un usuario y un bot.
        /// </summary>
        /// <param name="id">ID de la relación que se actualizará.</param>
        /// <param name="dto">Datos de la nueva relación entre usuario y bot.</param>
        /// <returns>Estado de la operación.</returns>
        // PUT: api/UserBotRelations/5
        [HttpPut("{id}")]
        [HasPermission("CanEditUserBotRelations")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserBotRelationDto dto)
        {
            var relation = await _context.UserBotRelations.FindAsync(id);
            if (relation == null)
                return NotFound();

            relation.RelationshipType = dto.RelationshipType ?? relation.RelationshipType;
            relation.InteractionScore = dto.InteractionScore ?? relation.InteractionScore;
            relation.LastInteraction = dto.LastInteraction ?? relation.LastInteraction;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Elimina una relación entre usuario y bot.
        /// </summary>
        /// <param name="id">ID de la relación que se eliminará.</param>
        /// <returns>Estado de la operación.</returns>
        // DELETE: api/UserBotRelations/5
        [HttpDelete("{id}")]
        [HasPermission("CanDeleteUserBotRelations")]
        public async Task<IActionResult> Delete(int id)
        {
            var relation = await _context.UserBotRelations.FindAsync(id);
            if (relation == null)
                return NotFound();

            _context.UserBotRelations.Remove(relation);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
