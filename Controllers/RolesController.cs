// Controllers/RolesController.cs
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

        // Obtiene todos los roles con sus permisos asociados.
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

        // Obtiene un rol específico por su ID
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

        // Crea un nuevo rol
        [HttpPost]
        [HasPermission("CanManageRoles")]
        public async Task<ActionResult<RoleDto>> CreateRole(CreateRoleDto dto)
        {
            // Verificar si ya existe un rol con el mismo nombre
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

            // Asignación de permisos al rol si es necesario
            if (dto.Permissions != null && dto.Permissions.Any())
            {
                var permissions = await _context.Permissions
                    .Where(p => dto.Permissions.Contains(p.Name))
                    .ToListAsync();

                foreach (var permission in permissions)
                {
                    var rolePermission = new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permission.Id
                    };

                    _context.RolePermissions.Add(rolePermission);
                }

                await _context.SaveChangesAsync();
            }

            var roleDto = new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                Permissions = dto.Permissions ?? new List<string>()
            };

            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, roleDto);
        }

        // Actualiza un rol existente
        [HttpPut("{id}")]
        [HasPermission("CanManageRoles")]
        public async Task<IActionResult> UpdateRole(int id, UpdateRoleDto dto)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                return NotFound();

            // Verificar si ya existe otro rol con el mismo nombre
            var nameExists = await _context.Roles
                .AnyAsync(r => r.Id != id && r.Name.ToLower() == dto.Name.ToLower());

            if (nameExists)
            {
                return BadRequest(new { Message = "Another role with the same name already exists." });
            }

            role.Name = dto.Name;
            role.Description = dto.Description;

            // Eliminar permisos actuales antes de asignar nuevos
            var currentPermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == id)
                .ToListAsync();

            _context.RolePermissions.RemoveRange(currentPermissions);

            // Asignar los nuevos permisos si se envían en el DTO
            if (dto.Permissions != null && dto.Permissions.Any())
            {
                var permissions = await _context.Permissions
                    .Where(p => dto.Permissions.Contains(p.Name))
                    .ToListAsync();

                foreach (var permission in permissions)
                {
                    var rolePermission = new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permission.Id
                    };

                    _context.RolePermissions.Add(rolePermission);
                }
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Elimina un rol por su ID
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
