using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using System.Linq;
using Microsoft.Extensions.Logging;

public class HasPermissionAttribute : AuthorizeAttribute, IAuthorizationFilter
{
    private readonly string _permission;

    public HasPermissionAttribute(string permission)
    {
        _permission = permission;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var db = context.HttpContext.RequestServices.GetService(typeof(ApplicationDbContext)) as ApplicationDbContext;

        var userIdClaim = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        // Get a logger if available so we can record why the authorization failed
        var logger = context.HttpContext.RequestServices.GetService(typeof(ILogger<HasPermissionAttribute>)) as ILogger<HasPermissionAttribute>;

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            logger?.LogWarning("Authorization failed: no user id claim present. RequiredPermission={Permission}", _permission);
            context.Result = new ForbidResult();
            return;
        }

        if (db == null)
        {
            logger?.LogWarning("Authorization failed: ApplicationDbContext not available. RequiredPermission={Permission}", _permission);
            context.HttpContext.Response.StatusCode = 403;
            context.Result = new JsonResult(new { Message = "Forbidden: internal error.", RequiredPermission = _permission });
            return;
        }

        var user = db.Users
            .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstOrDefault(u => u.Id == userId);

        if (user == null)
        {
            logger?.LogWarning("Authorization failed: user not found in DB. UserId={UserId} RequiredPermission={Permission}", userId, _permission);
            context.HttpContext.Response.StatusCode = 403;
            context.Result = new JsonResult(new { Message = "Forbidden: user not found.", RequiredPermission = _permission });
            return;
        }

        // Collect permission names for logging/debugging (safe handling of nulls)
        var userRoleName = user.Role?.Name ?? "(no-role)";
        var userPermissionNames = new System.Collections.Generic.List<string>();
        if (user.Role?.RolePermissions != null)
        {
            foreach (var rp in user.Role.RolePermissions)
            {
                if (rp?.Permission?.Name != null)
                {
                    userPermissionNames.Add(rp.Permission.Name);
                }
            }
        }

        var hasPerm = user.Role != null && user.Role.RolePermissions != null && user.Role.RolePermissions.Any(rp => rp.Permission != null && rp.Permission.Name == _permission);
        if (!hasPerm)
        {
            logger?.LogWarning("Authorization failed for user {UserId}. Role={RoleName}. Permissions=[{Permissions}]. RequiredPermission={Permission}", userId, userRoleName ?? "(no-role)", string.Join(",", userPermissionNames), _permission);
            context.HttpContext.Response.StatusCode = 403;
            context.Result = new JsonResult(new { Message = "Forbidden: missing required permission.", RequiredPermission = _permission });
            return;
        }
    }
}

