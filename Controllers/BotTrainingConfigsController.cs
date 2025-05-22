using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Models;
using Voia.Api.Data;


[ApiController]
[Route("api/[controller]")]
public class BotTrainingConfigsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BotTrainingConfigsController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene todos los registros de entrenamiento de bots.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BotTrainingConfigResponseDto>>> GetAll()
    {
        var configs = await _context.BotTrainingConfigs
            .Select(c => new BotTrainingConfigResponseDto
            {
                Id = c.Id,
                BotId = c.BotId,
                TrainingType = c.TrainingType,
                Data = c.Data,
                CreatedAt = c.CreatedAt
            }).ToListAsync();

        return Ok(configs);
    }
    
    [HttpGet("bot/{botId}/training-data")]
    public async Task<ActionResult<IEnumerable<BotTrainingConfigResponseDto>>> GetByBot(int botId)
    {
        var configs = await _context.BotTrainingConfigs
            .Where(c => c.BotId == botId)
            .Select(c => new BotTrainingConfigResponseDto
            {
                Id = c.Id,
                BotId = c.BotId,
                TrainingType = c.TrainingType,
                Data = c.Data,
                CreatedAt = c.CreatedAt
            }).ToListAsync();

        return Ok(configs);
    }

    /// <summary>
    /// Obtiene un registro específico por ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BotTrainingConfigResponseDto>> GetById(int id)
    {
        var config = await _context.BotTrainingConfigs.FindAsync(id);

        if (config == null)
            return NotFound();

        return new BotTrainingConfigResponseDto
        {
            Id = config.Id,
            BotId = config.BotId,
            TrainingType = config.TrainingType,
            Data = config.Data,
            CreatedAt = config.CreatedAt
        };
    }

    /// <summary>
    /// Crea una nueva configuración de entrenamiento para un bot.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BotTrainingConfigResponseDto>> Create(BotTrainingConfigCreateDto dto)
    {
        var config = new BotTrainingConfig
        {
            BotId = dto.BotId,
            TrainingType = dto.TrainingType,
            Data = dto.Data
        };

        _context.BotTrainingConfigs.Add(config);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = config.Id }, new BotTrainingConfigResponseDto
        {
            Id = config.Id,
            BotId = config.BotId,
            TrainingType = config.TrainingType,
            Data = config.Data,
            CreatedAt = config.CreatedAt
        });
    }

    /// <summary>
    /// Elimina un registro de entrenamiento por ID.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var config = await _context.BotTrainingConfigs.FindAsync(id);

        if (config == null)
            return NotFound();

        _context.BotTrainingConfigs.Remove(config);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
