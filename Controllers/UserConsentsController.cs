// Controllers/UserConsentsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voia.Api.Data;
using Voia.Api.Models;

namespace Voia.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserConsentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserConsentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/UserConsents/me
        [HttpGet("me")]
        public async Task<IActionResult> GetMyConsents()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var consents = await _context.UserConsents
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return Ok(consents);
        }

        // PUT: api/UserConsents/me/{consentType}
        [HttpPut("me/{consentType}")]
        public async Task<IActionResult> UpdateMyConsent(string consentType, [FromBody] bool granted)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var consent = await _context.UserConsents
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ConsentType == consentType);

            if (consent == null)
            {
                // Si no existe, lo creamos
                consent = new UserConsent
                {
                    UserId = userId,
                    ConsentType = consentType,
                    Granted = granted,
                    GrantedAt = granted ? DateTime.UtcNow : null
                };
                _context.UserConsents.Add(consent);
            }
            else
            {
                // Si existe, lo actualizamos
                consent.Granted = granted;
                consent.GrantedAt = granted ? DateTime.UtcNow : null;
                _context.UserConsents.Update(consent);
            }

            await _context.SaveChangesAsync();
            return Ok(consent);
        }
    }
}
