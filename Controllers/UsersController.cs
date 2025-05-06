// Controllers/UsersController.cs
using Microsoft.AspNetCore.Mvc;
using Voia.Api.Data;
using Voia.Api.Models;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Models.DTOs;
using Voia.Api.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BCrypt.Net;


namespace Voia.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;

        public UsersController(ApplicationDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService; // Inyección de dependencias
        }
        /// <summary>
        /// Obtiene la lista de todos los usuarios con su información de rol.
        /// </summary>
        /// <returns>Una lista de usuarios.</returns>
        /// <response code="200">Retorna la lista de usuarios</response>

        // GET: api/Users
        [HttpGet]
        [HasPermission("CanViewUsers")]
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
        /// <summary>
        /// Obtiene el perfil del usuario autenticado.
        /// </summary>
        /// <returns>Datos del usuario actual.</returns>
        /// <response code="200">Usuario encontrado.</response>
        /// <response code="404">Usuario no encontrado.</response>
        [HttpGet("me")]
        [Authorize(Roles = "Admin,User,Support,Trainer,Viewer")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { Message = "User not found" });

            var userDto = new GetUserDto
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
            };

            return Ok(userDto);
        }

        /// <summary>
        /// Crea un nuevo usuario.
        /// </summary>
        /// <param name="createUserDto">Objeto con los datos del nuevo usuario.</param>
        /// <returns>El usuario creado.</returns>
        /// <response code="201">Usuario creado exitosamente</response>
        /// <response code="400">Error de validación o email duplicado</response>
        // POST: api/Users
        [HttpPost]
        [HasPermission("CanEditUsers")]
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

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(CreateUserDto createUserDto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == createUserDto.Email))
            {
                return BadRequest(new { Message = "Email is already in use" });
            }

            // Puedes asignar el rol por defecto si el cliente no lo manda
            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            if (defaultRole == null)
            {
                return StatusCode(500, new { Message = "Default role not found" });
            }

            var user = new User
            {
                Name = createUserDto.Name,
                Email = createUserDto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password),
                RoleId = defaultRole.Id,
                DocumentTypeId = createUserDto.DocumentTypeId,
                Phone = createUserDto.Phone,
                Address = createUserDto.Address,
                DocumentNumber = createUserDto.DocumentNumber,
                DocumentPhotoUrl = createUserDto.DocumentPhotoUrl,
                AvatarUrl = createUserDto.AvatarUrl,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "User registered successfully" });
        }

        // PUT: api/Users/{id}
        [HttpPut("{id}")]
[       HasPermission("CanEditUsers")]
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
        /// <summary>
        /// Elimina un usuario por su ID.
        /// </summary>
        /// <param name="id">ID del usuario a eliminar.</param>
        /// <returns>Resultado de la operación.</returns>
        /// <response code="204">Usuario eliminado correctamente.</response>
        /// <response code="404">Usuario no encontrado.</response>
        // DELETE: api/Users/{id}
        [HttpDelete("{id}")]
        [HasPermission("CanDeleteUsers")]
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
        /// <summary>
        /// Autentica a un usuario y genera un token JWT.
        /// </summary>
        /// <param name="loginDto">Credenciales del usuario.</param>
        /// <returns>Token JWT y datos del usuario.</returns>
        /// <response code="200">Autenticación exitosa.</response>
        /// <response code="401">Credenciales incorrectas.</response>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
            {
                return Unauthorized(new { Message = "Invalid credentials" });
            }

            if (user == null)
                return Unauthorized();

            var token = _jwtService.GenerateToken(user);

            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    Role = new { user.Role.Id, user.Role.Name }
                }
            });
        }


    }
}
