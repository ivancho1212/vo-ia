using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Microsoft.AspNetCore.Authorization;


namespace Voia.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PermissionsController(ApplicationDbContext context)
        {
            _context = context;
        }
        /// <summary>
        /// Obtiene la lista de todos los permisos existentes.
        /// </summary>
        /// <returns>Lista de objetos Permission.</returns>
        /// <response code="200">Permisos obtenidos correctamente.</response>
        [HttpGet]
        [HasPermission("CanManagePermissions")]

        public async Task<ActionResult<IEnumerable<Permission>>> GetPermissions()
        {
            return await _context.Permissions.ToListAsync();
        }
        /// <summary>
        /// Crea un nuevo permiso en el sistema.
        /// </summary>
        /// <param name="permission">Objeto Permission con los datos del nuevo permiso.</param>
        /// <returns>Permiso creado.</returns>
        /// <response code="201">Permiso creado correctamente.</response>
        /// <response code="400">Error al validar el permiso.</response>
        [HttpPost]
        [HasPermission("CanManagePermissions")]
        public async Task<ActionResult<Permission>> CreatePermission(Permission permission)
        {
            // Verificar si ya existe un permiso con el mismo nombre (ignorando mayúsculas/minúsculas)
            var exists = await _context.Permissions
                .AnyAsync(p => p.Name.ToLower() == permission.Name.ToLower());

            if (exists)
            {
                return BadRequest(new { Message = "A permission with the same name already exists." });
            }

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPermissions), new { id = permission.Id }, permission);
        }


    }
}
