using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.UserPreferences;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserPreferencesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserPreferencesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/UserPreferences
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserPreference>>> GetAll()
        {
            return await _context.UserPreferences.ToListAsync();
        }

        // GET: api/UserPreferences/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserPreference>> GetById(int id)
        {
            var preference = await _context.UserPreferences.FindAsync(id);
            if (preference == null)
                return NotFound();

            return preference;
        }

        // POST: api/UserPreferences
        [HttpPost]
        public async Task<ActionResult<UserPreference>> Create([FromBody] CreateUserPreferenceDto dto)
        {
            var preference = new UserPreference
            {
                UserId = dto.UserId,
                InterestId = dto.InterestId
            };

            _context.UserPreferences.Add(preference);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = preference.Id }, preference);
        }

        // PUT: api/UserPreferences/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserPreferenceDto dto)
        {
            var preference = await _context.UserPreferences.FindAsync(id);
            if (preference == null)
                return NotFound();

            preference.InterestId = dto.InterestId;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/UserPreferences/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var preference = await _context.UserPreferences.FindAsync(id);
            if (preference == null)
                return NotFound();

            _context.UserPreferences.Remove(preference);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
