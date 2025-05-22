using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Voia.Api.Models.DTOs;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BotInstallationSettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BotInstallationSettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/botinstallationsettings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BotInstallationSettingResponseDto>>> GetAll()
        {
            var items = await _context.BotInstallationSettings
                .Select(x => new BotInstallationSettingResponseDto
                {
                    Id = x.Id,
                    BotId = x.BotId,
                    InstallationMethod = x.InstallationMethod,
                    InstallationInstructions = x.InstallationInstructions,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        // GET: api/botinstallationsettings/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<BotInstallationSettingResponseDto>> GetById(int id)
        {
            var setting = await _context.BotInstallationSettings.FindAsync(id);

            if (setting == null)
                return NotFound();

            return new BotInstallationSettingResponseDto
            {
                Id = setting.Id,
                BotId = setting.BotId,
                InstallationMethod = setting.InstallationMethod,
                InstallationInstructions = setting.InstallationInstructions,
                CreatedAt = setting.CreatedAt,
                UpdatedAt = setting.UpdatedAt
            };
        }

        // POST: api/botinstallationsettings
        [HttpPost]
        public async Task<ActionResult<BotInstallationSettingResponseDto>> Create(BotInstallationSettingCreateDto dto)
        {
            var setting = new BotInstallationSetting
            {
                BotId = dto.BotId,
                InstallationMethod = dto.InstallationMethod,
                InstallationInstructions = dto.InstallationInstructions,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BotInstallationSettings.Add(setting);
            await _context.SaveChangesAsync();

            var response = new BotInstallationSettingResponseDto
            {
                Id = setting.Id,
                BotId = setting.BotId,
                InstallationMethod = setting.InstallationMethod,
                InstallationInstructions = setting.InstallationInstructions,
                CreatedAt = setting.CreatedAt,
                UpdatedAt = setting.UpdatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = setting.Id }, response);
        }

        // PUT: api/botinstallationsettings/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, BotInstallationSettingUpdateDto dto)
        {
            var setting = await _context.BotInstallationSettings.FindAsync(id);

            if (setting == null)
                return NotFound();

            setting.BotId = dto.BotId;
            setting.InstallationMethod = dto.InstallationMethod;
            setting.InstallationInstructions = dto.InstallationInstructions;
            setting.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/botinstallationsettings/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var setting = await _context.BotInstallationSettings.FindAsync(id);

            if (setting == null)
                return NotFound();

            _context.BotInstallationSettings.Remove(setting);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
