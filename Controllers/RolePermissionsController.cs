using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Voia.Api.Models.DTOs;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolePermissionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RolePermissionsController(ApplicationDbContext context)
        {
            _context = context;
        }
        /// <summary>
        /// Obtiene todos los permisos asignados a un rol específico.
        /// </summary>
        /// <param name="roleId">ID del rol.</param>
        /// <returns>Lista de permisos asignados al rol.</returns>
        /// <response code="200">Permisos obtenidos correctamente.</response>
        // Obtener todos los permisos de un rol
        [HttpGet("{roleId}")]
        [HasPermission("ViewRolePermissions")]
        public async Task<ActionResult<IEnumerable<Permission>>> GetPermissionsByRole(int roleId)
        {
            var permissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Include(rp => rp.Permission)
                .Select(rp => rp.Permission)
                .ToListAsync();

            return Ok(permissions);
        }
        /// <summary>
        /// Asigna un permiso a un rol.
        /// </summary>
        /// <param name="dto">Objeto que contiene el ID del rol y del permiso.</param>
        /// <returns>Resultado de la operación.</returns>
        /// <response code="200">Permiso asignado correctamente.</response>
        /// <response code="400">El permiso ya está asignado al rol.</response>
        // Asignar un permiso a un rol
        [HttpPost("assign")]
        [HasPermission("AssignPermissionToRole")]
        public async Task<IActionResult> AssignPermissionToRole([FromBody] RolePermissionDto dto)
        {
            // Verificar si el permiso ya está asignado al rol
            var exists = await _context.RolePermissions
                .AnyAsync(rp => rp.RoleId == dto.RoleId && rp.PermissionId == dto.PermissionId);

            if (exists)
            {
                return BadRequest(new { Message = "Permission already assigned to role" });
            }

            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = dto.RoleId,
                PermissionId = dto.PermissionId
            });

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Permission assigned successfully" });
        }

        /// <summary>
        /// Revoca un permiso previamente asignado a un rol.
        /// </summary>
        /// <param name="dto">Objeto que contiene el ID del rol y del permiso.</param>
        /// <returns>Resultado de la operación.</returns>
        /// <response code="200">Permiso revocado correctamente.</response>
        /// <response code="404">Permiso no encontrado para este rol.</response>
        // Revocar un permiso de un rol
        [HttpDelete("revoke")]
        [HasPermission("RevokePermissionFromRole")]
        public async Task<IActionResult> RevokePermissionFromRole([FromBody] RolePermissionDto dto)
        {
            var rp = await _context.RolePermissions
                .FirstOrDefaultAsync(r => r.RoleId == dto.RoleId && r.PermissionId == dto.PermissionId);

            if (rp == null)
                return NotFound(new { Message = "Permission not found for this role" });

            _context.RolePermissions.Remove(rp);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Permission revoked successfully" });
        }
    }
}
