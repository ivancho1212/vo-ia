using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.Plans;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlansController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PlansController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Plan>>> GetPlans()
        {
            return await _context.Plans.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Plan>> CreatePlan(Plan plan)
        {
            // Validar nombre único
            if (await _context.Plans.AnyAsync(p => p.Name == plan.Name))
                return Conflict("Ya existe un plan con ese nombre.");

            _context.Plans.Add(plan);
            await _context.SaveChangesAsync();

            return Created($"api/Plans/{plan.Id}", plan);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePlan(int id, Plan plan)
        {
            if (id != plan.Id)
                return BadRequest("El ID del plan no coincide.");

            var existingPlan = await _context.Plans.FindAsync(id);
            if (existingPlan == null)
                return NotFound("Plan no encontrado.");

            existingPlan.Name = plan.Name;
            existingPlan.Description = plan.Description;
            existingPlan.Price = plan.Price;
            existingPlan.MaxTokens = plan.MaxTokens;
            existingPlan.BotsLimit = plan.BotsLimit;
            existingPlan.IsActive = plan.IsActive;

            await _context.SaveChangesAsync();

            return NoContent();
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlan(int id)
        {
            var plan = await _context.Plans.FindAsync(id);
            if (plan == null)
                return NotFound();

            _context.Plans.Remove(plan);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
