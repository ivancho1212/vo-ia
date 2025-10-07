using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.ConversationTag;
using Voia.Api.Models.Messages;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConversationTagsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ConversationTagsController(ApplicationDbContext context)
        {
            _context = context;
        }

    // GET: api/ConversationTags/conversation/5
    [HttpGet("conversation/{conversationId}")]
    [HasPermission("CanViewConversationTags")]
    public async Task<ActionResult<IEnumerable<ConversationTagDto>>> GetTagsByConversation(int conversationId)
        {
            var tags = await _context.ConversationTags
                .Where(t => t.ConversationId == conversationId)
                .Include(t => t.HighlightedMessage)
                .Select(t => new ConversationTagDto
                {
                    Id = t.Id,
                    ConversationId = t.ConversationId,
                    Label = t.Label,
                    HighlightedMessageId = t.HighlightedMessageId,
                    HighlightedMessageText = t.HighlightedMessage != null ? t.HighlightedMessage.MessageText : null,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(tags);
        }

    // POST: api/ConversationTags
    [HttpPost]
    [HasPermission("CanEditConversationTags")]
    public async Task<ActionResult<ConversationTagDto>> CreateTag([FromBody] ConversationTagCreateDto dto)
        {
            var tag = new ConversationTag
            {
                ConversationId = dto.ConversationId,
                Label = dto.Label,
                HighlightedMessageId = dto.HighlightedMessageId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ConversationTags.Add(tag);
            await _context.SaveChangesAsync();

            // Obtener el texto del mensaje destacado si aplica
            string? highlightedMessageText = null;
            if (tag.HighlightedMessageId.HasValue)
            {
                highlightedMessageText = await _context.Messages
                    .Where(m => m.Id == tag.HighlightedMessageId.Value)
                    .Select(m => m.MessageText)
                    .FirstOrDefaultAsync();
            }

            var resultDto = new ConversationTagDto
            {
                Id = tag.Id,
                ConversationId = tag.ConversationId,
                Label = tag.Label,
                HighlightedMessageId = tag.HighlightedMessageId,
                HighlightedMessageText = highlightedMessageText,
                CreatedAt = tag.CreatedAt
            };

            return CreatedAtAction(nameof(GetTagsByConversation), new { conversationId = tag.ConversationId }, resultDto);
        }

    // DELETE: api/ConversationTags/5
    [HttpDelete("{id}")]
    [HasPermission("CanDeleteConversationTags")]
    public async Task<IActionResult> DeleteTag(int id)
        {
            var tag = await _context.ConversationTags.FindAsync(id);
            if (tag == null)
            {
                return NotFound();
            }

            _context.ConversationTags.Remove(tag);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
