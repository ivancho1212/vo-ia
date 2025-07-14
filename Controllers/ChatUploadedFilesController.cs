using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.Chat;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatUploadedFilesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChatUploadedFilesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("conversation/{conversationId}")]
        public async Task<IActionResult> GetFilesByConversation(int conversationId)
        {
            var files = await _context.ChatUploadedFiles
                .Where(f => f.ConversationId == conversationId)
                .ToListAsync();

            return Ok(files);
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile([FromBody] ChatUploadedFile file)
        {
            _context.ChatUploadedFiles.Add(file);
            await _context.SaveChangesAsync();
            return Ok(file);
        }
    }
}
