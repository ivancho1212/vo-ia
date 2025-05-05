using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Voia.Api.Models.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace Voia.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RolesController(ApplicationDbContext context)
        {
            _context = context;
        }
        /// <summary>
        /// Obtiene todos los roles con sus permisos asociados.
        /// </summary>
        /// <returns>Lista de roles.</returns>
        /// <response code="200">Roles obtenidos correctamente.</response>
        [HttpGet]
        [HasPermission("CanManageRoles")]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
        {
            var roles = await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .ToListAsync();

            var result = roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Permissions = r.RolePermissions.Select(rp => rp.Permission.Name).ToList()
            });

            return Ok(result);
        }
        /// <summary>
        /// Obtiene un rol específico por su ID.
        /// </summary>
        /// <param name="id">ID del rol.</param>
        /// <returns>El rol solicitado.</returns>
        /// <response code="200">Rol encontrado.</response>
        /// <response code="404">Rol no encontrado.</response>
        [HttpGet("{id}")]
        [HasPermission("CanManageRoles")]
        public async Task<ActionResult<RoleDto>> GetRole(int id)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null) return NotFound();

            var roleDto = new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                Permissions = role.RolePermissions.Select(rp => rp.Permission.Name).ToList()
            };

            return Ok(roleDto);
        }
        /// <summary>
        /// Crea un nuevo rol.
        /// </summary>
        /// <param name="dto">Datos del nuevo rol.</param>
        /// <returns>Rol creado.</returns>
        /// <response code="201">Rol creado correctamente.</response>
        
        [HttpPost]
        [HasPermission("CanManageRoles")]
        public async Task<ActionResult<RoleDto>> CreateRole(CreateRoleDto dto)
        {
            // Verificar si ya existe un rol con el mismo nombre (ignorar mayúsculas/minúsculas)
            var exists = await _context.Roles
                .AnyAsync(r => r.Name.ToLower() == dto.Name.ToLower());

            if (exists)
            {
                return BadRequest(new { Message = "A role with the same name already exists." });
            }

            var role = new Role
            {
                Name = dto.Name,
                Description = dto.Description
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            var roleDto = new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                Permissions = new List<string>()
            };

            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, roleDto);
        }

        /// <summary>
        /// Actualiza un rol existente.
        /// </summary>
        /// <param name="id">ID del rol a actualizar.</param>
        /// <param name="dto">Datos actualizados del rol.</param>
        /// <returns>Resultado de la operación.</returns>
        /// <response code="204">Rol actualizado correctamente.</response>
        /// <response code="404">Rol no encontrado.</response>
        [HttpPut("{id}")]
        [HasPermission("CanManageRoles")]
        public async Task<IActionResult> UpdateRole(int id, UpdateRoleDto dto)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                return NotFound();

            // Verificar si ya existe otro rol con el mismo nombre (ignorando mayúsculas/minúsculas)
            var nameExists = await _context.Roles
                .AnyAsync(r => r.Id != id && r.Name.ToLower() == dto.Name.ToLower());

            if (nameExists)
            {
                return BadRequest(new { Message = "Another role with the same name already exists." });
            }

            role.Name = dto.Name;
            role.Description = dto.Description;
            _context.Entry(role).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Elimina un rol por su ID.
        /// </summary>
        /// <param name="id">ID del rol a eliminar.</param>
        /// <returns>Resultado de la operación.</returns>
        /// <response code="204">Rol eliminado correctamente.</response>
        /// <response code="404">Rol no encontrado.</response>
        [HttpDelete("{id}")]
        [HasPermission("CanManageRoles")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return NotFound();

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
