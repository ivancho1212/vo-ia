using Microsoft.AspNetCore.Mvc;
using Voia.Api.Data;  // Asegúrate de que este namespace esté correcto dependiendo de tu proyecto

namespace Voia.Api.Controllers  // Asegúrate de que el namespace coincida con el resto de tus controladores
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestConnectionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TestConnectionController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("test-connection")]
        public IActionResult TestConnection()
        {
            try
            {
                // Intenta ejecutar una consulta simple para verificar la conexión
                _context.Database.CanConnect();
                return Ok("Connection to the database is successful.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error connecting to the database: {ex.Message}");
            }
        }
    }
}
