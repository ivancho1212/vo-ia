using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.UserPreferences;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Voia.Api.Controllers
{
    [Authorize(Roles = "Admin,User")]
    [Route("api/[controller]")]
    [ApiController]
    public class UserPreferencesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserPreferencesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todas las preferencias de usuario.
        /// </summary>
        /// <returns>Lista de preferencias de usuario.</returns>
        // GET: api/UserPreferences
        [HttpGet]
        [HasPermission("CanViewUsers")]
        public async Task<ActionResult<IEnumerable<UserPreference>>> GetAll()
        {
            return await _context.UserPreferences.ToListAsync();
        }

        /// <summary>
        /// Obtiene la preferencia de usuario por ID.
        /// </summary>
        /// <param name="id">ID de la preferencia de usuario.</param>
        /// <returns>Preferencia de usuario.</returns>
        // GET: api/UserPreferences/5
        [HttpGet("{id}")]
        [HasPermission("CanViewUsers")]
        public async Task<ActionResult<UserPreference>> GetById(int id)
        {
            var preference = await _context.UserPreferences.FindAsync(id);
            if (preference == null)
                return NotFound();

            return preference;
        }

        /// <summary>
        /// Crea una nueva preferencia de usuario.
        /// </summary>
        /// <param name="dto">Datos de la nueva preferencia de usuario.</param>
        /// <returns>Preferencia de usuario creada.</returns>
        // POST: api/UserPreferences
        [HttpPost]
        [HasPermission("CanViewUsers")]
        public async Task<ActionResult<UserPreference>> Create([FromBody] CreateUserPreferenceDto dto)
        {
            // Verificar si ya existe la preferencia para este usuario
            var exists = await _context.UserPreferences
                .AnyAsync(up => up.UserId == dto.UserId && up.InterestId == dto.InterestId);

            if (exists)
            {
                return BadRequest(new { Message = "User preference already exists" });
            }

            var preference = new UserPreference
            {
                UserId = dto.UserId,
                InterestId = dto.InterestId
            };

            _context.UserPreferences.Add(preference);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = preference.Id }, preference);
        }

        /// <summary>
        /// Actualiza la preferencia de usuario existente.
        /// </summary>
        /// <param name="id">ID de la preferencia de usuario a actualizar.</param>
        /// <param name="dto">Nuevo dato de la preferencia de usuario.</param>
        /// <returns>Estado de la operación.</returns>
        // PUT: api/UserPreferences/5
        [HttpPut("{id}")]
        [HasPermission("CanViewUsers")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserPreferenceDto dto)
        {
            var preference = await _context.UserPreferences.FindAsync(id);
            if (preference == null)
                return NotFound();

            preference.InterestId = dto.InterestId;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Elimina la preferencia de usuario por ID.
        /// </summary>
        /// <param name="id">ID de la preferencia de usuario a eliminar.</param>
        /// <returns>Estado de la operación.</returns>
        // DELETE: api/UserPreferences/5
        [HttpDelete("{id}")]
        [HasPermission("CanViewUsers")]
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
