// Controllers/UsersController.cs
using Microsoft.AspNetCore.Mvc;
using Voia.Api.Data;
using Voia.Api.Models;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Models.DTOs;
using Voia.Api.Services; // Asegúrate de agregar esta referencia

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService; // Servicio JWT para generar el token

        public UsersController(ApplicationDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService; // Inyección de dependencias
        }

        // GET: api/Users
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Role) // Incluye la información de Role
                .ToListAsync();

            var userDtos = users.Select(user => new GetUserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                DocumentNumber = user.DocumentNumber,
                DocumentPhotoUrl = user.DocumentPhotoUrl,
                AvatarUrl = user.AvatarUrl,
                IsVerified = user.IsVerified,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Role = new RoleDto
                {
                    Id = user.Role.Id,
                    Name = user.Role.Name
                }
            }).ToList();

            return Ok(userDtos);
        }

        // POST: api/Users
        [HttpPost]
        public async Task<IActionResult> PostUser(CreateUserDto createUserDto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == createUserDto.Email))
            {
                return BadRequest(new { Message = "Email is already in use" });
            }

            var user = new User
            {
                Name = createUserDto.Name,
                Email = createUserDto.Email,
                Password = createUserDto.Password, // Encriptar la contraseña
                RoleId = createUserDto.RoleId,
                DocumentTypeId = createUserDto.DocumentTypeId,
                Phone = createUserDto.Phone,
                Address = createUserDto.Address,
                DocumentNumber = createUserDto.DocumentNumber,
                DocumentPhotoUrl = createUserDto.DocumentPhotoUrl,
                AvatarUrl = createUserDto.AvatarUrl,
                IsVerified = createUserDto.IsVerified,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, user);
        }

        // PUT: api/Users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, UpdateUserDto updateUserDto)
        {
            // Validación de que los IDs coinciden
            if (id != updateUserDto.Id)
            {
                return BadRequest(new { Message = "User ID mismatch" });
            }

            // Buscar el usuario por su ID
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            // Actualizar los campos del usuario
            user.Name = updateUserDto.Name;
            user.Email = updateUserDto.Email;
            user.Phone = updateUserDto.Phone;
            user.Address = updateUserDto.Address;
            user.DocumentNumber = updateUserDto.DocumentNumber;
            user.DocumentPhotoUrl = updateUserDto.DocumentPhotoUrl;
            user.AvatarUrl = updateUserDto.AvatarUrl;
            user.IsVerified = updateUserDto.IsVerified;
            user.UpdatedAt = DateTime.UtcNow;

            try
            {
                // Marcar la entrada como modificada y guardar los cambios
                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                // Devolver el usuario actualizado
                return Ok(user);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Si ocurre un error en la base de datos, se captura y maneja
                return StatusCode(500, new { Message = "An error occurred while updating the user" });
            }
        }

        // DELETE: api/Users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Users/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            // Buscar al usuario por email y contraseña
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == loginRequest.Email && u.Password == loginRequest.Password); // **En producción usar hash para la contraseña**

            if (user == null)
            {
                return Unauthorized("Credenciales inválidas");
            }

            // Generar el token
            var token = _jwtService.GenerateToken(user);
            return Ok(new { token });
        }
    }
}
