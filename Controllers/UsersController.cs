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
using Voia.Api.Models.Subscriptions;
using Voia.Api.Models.bot;


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
                Country = user.Country,  // ✅ nuevo
                City = user.City,        // ✅ nuevo
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
                .Include(u => u.Subscriptions)
                    .ThenInclude(s => s.Plan)
                .Include(u => u.Bots) // cargamos los bots
                .AsSplitQuery() // Add this line
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { Message = "User not found" });

            // Tomar solo la última suscripción activa
            var activeSubscription = user.Subscriptions
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefault();

            var userDto = new GetUserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Country = user.Country,
                City = user.City,
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
                },
                Subscription = activeSubscription != null
                    ? new SubscriptionDto
                    {
                        Id = activeSubscription.Id,
                        UserId = user.Id,
                        UserName = user.Name,
                        UserEmail = user.Email,
                        PlanId = activeSubscription.Plan.Id,
                        PlanName = activeSubscription.Plan.Name,
                        StartedAt = activeSubscription.StartedAt,
                        ExpiresAt = activeSubscription.ExpiresAt,
                        Status = activeSubscription.Status
                    }
                    : null,
                Bots = user.Bots
                    .Where(b => b.IsActive) // solo activos
                    .Select(b => new BotDto
                    {
                        Id = b.Id,
                        Name = b.Name,
                        Description = b.Description
                    })
                    .ToList()
            };

            return Ok(userDto);
        }

        [HttpPut("me/avatar")]
        [Authorize(Roles = "Admin,User,Support,Trainer,Viewer")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { Message = "User not found" });

            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "Archivo no válido" });

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var savePath = Path.Combine("wwwroot", "uploads", fileName);

            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            user.AvatarUrl = $"/uploads/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new { avatarUrl = user.AvatarUrl });
        }


        [HttpPut("me/document-photo")]
        [Authorize(Roles = "Admin,User,Support,Trainer,Viewer")]
        public async Task<IActionResult> UploadDocumentPhoto(IFormFile file)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { Message = "Usuario no encontrado" });

            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "Archivo no válido" });

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var savePath = Path.Combine("wwwroot", "uploads", fileName);

            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            user.DocumentPhotoUrl = $"/uploads/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new { documentPhotoUrl = user.DocumentPhotoUrl });
        }
        /// <summary>
        /// Actualiza el perfil del usuario autenticado.
        /// </summary>
        /// <param name="updateDto">Datos a actualizar.</param>
        /// <returns>Perfil actualizado.</returns>
        [HttpPut("me")]
        [Authorize(Roles = "Admin,User,Support,Trainer,Viewer")]
        public async Task<IActionResult> UpdateMyProfile(UpdateMyProfileDto updateDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound(new { Message = "Usuario no encontrado" });

            // Validaciones (email, documento o teléfono duplicado)
            if (!string.Equals(user.Email, updateDto.Email, StringComparison.OrdinalIgnoreCase)
                && await _context.Users.AnyAsync(u => u.Email == updateDto.Email))
            {
                return BadRequest(new { Message = "El email ya está en uso" });
            }

            if (!string.Equals(user.DocumentNumber, updateDto.DocumentNumber, StringComparison.OrdinalIgnoreCase)
                && await _context.Users.AnyAsync(u => u.DocumentNumber == updateDto.DocumentNumber))
            {
                return BadRequest(new { Message = "El número de documento ya está en uso" });
            }

            if (!string.Equals(user.Phone, updateDto.Phone, StringComparison.OrdinalIgnoreCase)
                && await _context.Users.AnyAsync(u => u.Phone == updateDto.Phone))
            {
                return BadRequest(new { Message = "El número de teléfono ya está en uso" });
            }

            // Actualizar datos
            user.Name = updateDto.Name;
            user.Email = updateDto.Email;
            user.Phone = updateDto.Phone;
            user.Country = updateDto.Country;  // ✅ nuevo
            user.City = updateDto.City;        // ✅ nuevo
            user.Address = updateDto.Address;
            user.DocumentNumber = updateDto.DocumentNumber;
            user.UpdatedAt = DateTime.UtcNow;


            await _context.SaveChangesAsync();

            return Ok(new { Message = "Perfil actualizado correctamente" });
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
        public async Task<IActionResult> PostUser(AdminCreateUserDto createUserDto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == createUserDto.Email))
            {
                return BadRequest(new { Message = "Email is already in use" });
            }

            var user = new User
            {
                Name = createUserDto.Name,
                Email = createUserDto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password),
                RoleId = createUserDto.RoleId,
                DocumentTypeId = createUserDto.DocumentTypeId,
                Phone = createUserDto.Phone,
                Country = createUserDto.Country,  // ✅ nuevo
                City = createUserDto.City,        // ✅ nuevo
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
            try
            {
                // Validación de email duplicado
                if (await _context.Users.AnyAsync(u => u.Email == createUserDto.Email))
                {
                    return BadRequest(new { Message = "El email ya está en uso" });
                }

                // Validación de documento duplicado
                if (await _context.Users.AnyAsync(u => u.DocumentNumber == createUserDto.DocumentNumber))
                {
                    return BadRequest(new { Message = "El número de documento ya está en uso" });
                }

                // Validación de teléfono duplicado
                if (await _context.Users.AnyAsync(u => u.Phone == createUserDto.Phone))
                {
                    return BadRequest(new { Message = "El número de teléfono ya está en uso" });
                }

                // Asignar roleId fijo (2)
                const int fixedRoleId = 2;

                // Creación del usuario
                var user = new User
                {
                    Name = createUserDto.Name,
                    Email = createUserDto.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password),
                    RoleId = fixedRoleId,
                    DocumentTypeId = createUserDto.DocumentTypeId,
                    Phone = createUserDto.Phone,
                    Country = createUserDto.Country,  // ✅ nuevo
                    City = createUserDto.City,        // ✅ nuevo
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

                return Ok(new { Message = "Usuario registrado exitosamente" });
            }
            catch (Exception ex)
            {
                // En producción, considera registrar el error en un sistema de logs en vez de mostrarlo
                return StatusCode(500, new { Message = "Error interno del servidor", Details = ex.ToString() });
            }
        }

        // PUT: api/Users/{id}
        [HttpPut("{id}")]
        [HasPermission("CanEditUsers")]
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
            user.Country = updateUserDto.Country;  // ✅ nuevo
            user.City = updateUserDto.City;        // ✅ nuevo
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
                .Include(u => u.Role) // Cargando el rol del usuario
                .Include(u => u.Subscriptions) // Incluir las suscripciones del usuario
                                .ThenInclude(s => s.Plan) // Incluir el plan asociado a la suscripción
                .AsSplitQuery() // Add this line
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
            {
                return Unauthorized(new { Message = "Invalid credentials" });
            }

            // Verificar que el usuario tenga al menos una suscripción activa
            var activeSubscription = user.Subscriptions?.FirstOrDefault(s => s.Status == "active");
            if (activeSubscription != null)
            {
                var plan = activeSubscription.Plan; // Obtener el plan del usuario
                // Aquí también podrías agregar cualquier validación de plan
            }

            var token = _jwtService.GenerateToken(user);

            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    Role = new { user.Role.Id, user.Role.Name },
                    Plan = activeSubscription?.Plan != null ? new { activeSubscription.Plan.Id, activeSubscription.Plan.Name } : null // Incluir el plan si existe
                }
            });
        }

    }
}
