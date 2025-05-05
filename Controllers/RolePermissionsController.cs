using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;

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

        [HttpGet("{roleId}")]
        public async Task<ActionResult<IEnumerable<Permission>>> GetPermissionsByRole(int roleId)
        {
            var permissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Include(rp => rp.Permission)
                .Select(rp => rp.Permission)
                .ToListAsync();

            return permissions;
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignPermissionToRole(int roleId, int permissionId)
        {
            var exists = await _context.RolePermissions
                .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (exists)
                return BadRequest(new { Message = "Permission already assigned to role" });

            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Permission assigned successfully" });
        }

        [HttpDelete("revoke")]
        public async Task<IActionResult> RevokePermissionFromRole(int roleId, int permissionId)
        {
            var rp = await _context.RolePermissions
                .FirstOrDefaultAsync(r => r.RoleId == roleId && r.PermissionId == permissionId);

            if (rp == null)
                return NotFound(new { Message = "Permission not found for this role" });

            _context.RolePermissions.Remove(rp);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Permission revoked successfully" });
        }
    }
}
