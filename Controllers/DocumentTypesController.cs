using Microsoft.AspNetCore.Mvc;
using Voia.Api.Data;
using Voia.Api.Models;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;

namespace Voia.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentTypesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public DocumentTypesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDocumentTypes()
        {
            var types = _context.DocumentTypes.Select(dt => new {
                id = dt.Id,
                name = dt.Name,
                abbreviation = dt.Abbreviation,
                description = dt.Description
            }).ToList();
            return Ok(types);
        }
    }
}
