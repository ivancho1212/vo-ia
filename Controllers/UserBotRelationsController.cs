using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Voia.Api.Data;
using Voia.Api.Models.UserBotRelations;
using System;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserBotRelationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserBotRelationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/UserBotRelations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserBotRelation>>> GetAll()
        {
            return await _context.UserBotRelations.ToListAsync();
        }

        // GET: api/UserBotRelations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserBotRelation>> GetById(int id)
        {
            var relation = await _context.UserBotRelations.FindAsync(id);
            if (relation == null)
                return NotFound();

            return relation;
        }

        // POST: api/UserBotRelations
        [HttpPost]
        public async Task<ActionResult<UserBotRelation>> Create([FromBody] CreateUserBotRelationDto dto)
        {
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

        // PUT: api/UserBotRelations/5
        [HttpPut("{id}")]
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

        // DELETE: api/UserBotRelations/5
        [HttpDelete("{id}")]
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
