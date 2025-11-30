// Controllers/UsersController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
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
using Voia.Api.Services.Caching;


namespace Voia.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;
        private readonly ICacheService _cacheService;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public UsersController(ApplicationDbContext context, JwtService jwtService, ICacheService cacheService, UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            _context = context;
            _jwtService = jwtService;
            _cacheService = cacheService;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        /// <summary>
        /// Obtiene la lista de todos los usuarios con su información de rol.
        /// </summary>
        /// <returns>Una lista de usuarios.</returns>
        /// <response code="200">Retorna la lista de usuarios</response>

        // GET: api/Users
        [HttpGet]
        [HasPermission("CanViewUsers")]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        {
            // Si no hay búsqueda, intentar obtener del caché
            if (string.IsNullOrWhiteSpace(search))
            {
                var cacheKey = CacheConstants.GetAllUsersKey(page, pageSize);
                var cached = await _cacheService.GetAsync<object>(cacheKey);
                if (cached != null)
                {
                    return Ok(cached);
                }
            }

            var query = _context.Users
                .Include(u => u.Role)
                .Include(u => u.Bots)
                .Include(u => u.Subscriptions)
                .Include(u => u.DocumentType)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u =>
                    u.Name.Contains(search) ||
                    u.Email.Contains(search) ||
                    u.Phone.Contains(search) ||
                    u.DocumentNumber.Contains(search)
                );
            }

            var total = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userDtos = users.Select(user => new GetUserDto
            {
                Id = user.Id,
                Name = user.Name ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Phone = user.Phone ?? string.Empty,
                Country = user.Country ?? string.Empty,
                City = user.City ?? string.Empty,
                Address = user.Address ?? string.Empty,
                DocumentNumber = user.DocumentNumber ?? string.Empty,
                DocumentPhotoUrl = user.DocumentPhotoUrl ?? string.Empty,
                AvatarUrl = user.AvatarUrl ?? string.Empty,
                IsVerified = user.IsVerified,
                IsActive = user.IsActive,
                Status = string.IsNullOrEmpty(user.Status) ? (user.IsActive ? "active" : "inactive") : user.Status,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                DocumentTypeName = user.DocumentType != null ? user.DocumentType.Name : null,
                Role = user.Role != null ? new RoleDto
                {
                    Id = user.Role.Id,
                    Name = user.Role.Name
                } : null,
                Plan = user.Subscriptions?.OrderByDescending(s => s.StartedAt).FirstOrDefault(s => s.Status == "active")?.Plan != null
                    ? new PlanDto
                    {
                        Id = user.Subscriptions.OrderByDescending(s => s.StartedAt).FirstOrDefault(s => s.Status == "active")?.Plan?.Id ?? 0,
                        Name = user.Subscriptions.OrderByDescending(s => s.StartedAt).FirstOrDefault(s => s.Status == "active")?.Plan?.Name ?? string.Empty,
                        Description = user.Subscriptions.OrderByDescending(s => s.StartedAt).FirstOrDefault(s => s.Status == "active")?.Plan?.Description ?? string.Empty,
                        Price = user.Subscriptions.OrderByDescending(s => s.StartedAt).FirstOrDefault(s => s.Status == "active")?.Plan?.Price ?? 0,
                        MaxTokens = user.Subscriptions.OrderByDescending(s => s.StartedAt).FirstOrDefault(s => s.Status == "active")?.Plan?.MaxTokens ?? 0,
                        BotsLimit = user.Subscriptions.OrderByDescending(s => s.StartedAt).FirstOrDefault(s => s.Status == "active")?.Plan?.BotsLimit ?? 0
                    }
                    : null,
                Bots = user.Bots?.Where(b => b.IsActive).Select(b => new BotDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description
                }).ToList() ?? new List<BotDto>(),
            }).ToList();

            var response = new {
                total,
                page,
                pageSize,
                users = userDtos
            };

            // Guardar en caché solo si no hay búsqueda
            if (string.IsNullOrWhiteSpace(search))
            {
                var cacheKey = CacheConstants.GetAllUsersKey(page, pageSize);
                await _cacheService.SetAsync(cacheKey, response, CacheConstants.USER_TTL);
            }

            return Ok(response);
        }
        /// <summary>
        /// Obtiene el perfil del usuario autenticado.
        /// </summary>
        /// <returns>Datos del usuario actual.</returns>
        /// <response code="200">Usuario encontrado.</response>
        /// <response code="404">Usuario no encontrado.</response>
        [HttpGet("me")]
            [Authorize(Roles = "Admin,User,Support,Trainer,Viewer,Super Admin")]
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
                        Description = b.Description,
                        IsReady = b.IsReady // Nuevo: exponer estado de entrenamiento
                    })
                    .ToList()
            };

            return Ok(userDto);
        }

        [HttpPut("me/avatar")]
            [Authorize(Roles = "Admin,User,Support,Trainer,Viewer,Super Admin")]
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
            [Authorize(Roles = "Admin,User,Support,Trainer,Viewer,Super Admin")]
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
            [Authorize(Roles = "Admin,User,Support,Trainer,Viewer,Super Admin")]
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
            if (await _userManager.FindByEmailAsync(createUserDto.Email) != null)
            {
                return BadRequest(new { Message = "Email is already in use" });
            }

            var user = new User
            {
                UserName = createUserDto.Email,
                Name = createUserDto.Name,
                Email = createUserDto.Email,
                DocumentTypeId = createUserDto.DocumentTypeId,
                Phone = createUserDto.Phone,
                Country = createUserDto.Country,
                City = createUserDto.City,
                Address = createUserDto.Address,
                DocumentNumber = createUserDto.DocumentNumber,
                DocumentPhotoUrl = createUserDto.DocumentPhotoUrl,
                AvatarUrl = createUserDto.AvatarUrl,
                IsVerified = createUserDto.IsVerified,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, createUserDto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new { Message = "Error creating user", Errors = result.Errors });
            }

            // Asignar rol si se especifica
            if (!string.IsNullOrWhiteSpace(createUserDto.RoleName))
            {
                var roleExists = await _roleManager.RoleExistsAsync(createUserDto.RoleName);
                if (!roleExists)
                {
                    return BadRequest(new { Message = $"Role '{createUserDto.RoleName}' does not exist" });
                }
                await _userManager.AddToRoleAsync(user, createUserDto.RoleName);
            }

            return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, user);
        }


        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(CreateUserDto createUserDto)
        {
            Console.WriteLine($"[Register] Email recibido: {createUserDto.Email}");
            try
            {
                // Validación de email duplicado
                if (await _userManager.FindByEmailAsync(createUserDto.Email) != null)
                {
                    return BadRequest(new { Message = "El email ya está en uso" });
                }

                var user = new User
                {
                    UserName = createUserDto.Email,
                    Name = createUserDto.Name,
                    Email = createUserDto.Email,
                    DocumentTypeId = createUserDto.DocumentTypeId,
                    Phone = createUserDto.Phone,
                    Country = createUserDto.Country,
                    City = createUserDto.City,
                    Address = createUserDto.Address,
                    DocumentNumber = createUserDto.DocumentNumber,
                    DocumentPhotoUrl = createUserDto.DocumentPhotoUrl,
                    AvatarUrl = createUserDto.AvatarUrl,
                    IsVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, createUserDto.Password);
                if (!result.Succeeded)
                {
                    return BadRequest(new { Message = "Error registrando usuario", Errors = result.Errors });
                }

                // Asignar rol si se especifica
                if (!string.IsNullOrWhiteSpace(createUserDto.RoleName))
                {
                    var roleExists = await _roleManager.RoleExistsAsync(createUserDto.RoleName);
                    if (!roleExists)
                    {
                        return BadRequest(new { Message = $"Role '{createUserDto.RoleName}' does not exist" });
                    }
                    await _userManager.AddToRoleAsync(user, createUserDto.RoleName);
                }

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
            user.Country = updateUserDto.Country;
            user.City = updateUserDto.City;
            user.Address = updateUserDto.Address;
            user.DocumentNumber = updateUserDto.DocumentNumber;
            user.DocumentPhotoUrl = updateUserDto.DocumentPhotoUrl;
            user.AvatarUrl = updateUserDto.AvatarUrl;
            user.IsVerified = updateUserDto.IsVerified;
            // Actualizar rol usando Identity si RoleName está presente
            if (!string.IsNullOrWhiteSpace(updateUserDto.RoleName))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                var roleExists = await _roleManager.RoleExistsAsync(updateUserDto.RoleName);
                if (roleExists)
                {
                    await _userManager.AddToRoleAsync(user, updateUserDto.RoleName);
                }
            }
            user.DocumentTypeId = updateUserDto.DocumentTypeId;
            user.UpdatedAt = DateTime.UtcNow;
            // Lógica de status
            if (!string.IsNullOrEmpty(updateUserDto.Status))
            {
                user.Status = updateUserDto.Status;
                if (updateUserDto.Status == "blocked")
                {
                    user.IsActive = false;
                }
                else if (updateUserDto.Status == "inactive")
                {
                    user.IsActive = false;
                }
                else if (updateUserDto.Status == "active")
                {
                    user.IsActive = true;
                }
            }


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
            var user = await _userManager.Users
                .Include(u => u.Role)
                .Include(u => u.Subscriptions)
                    .ThenInclude(s => s.Plan)
                .AsSplitQuery()
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null || !(await _userManager.CheckPasswordAsync(user, loginDto.Password)))
            {
                return Unauthorized(new { Message = "Invalid credentials" });
            }

            // Obtener permisos del rol
            var permissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == user.RoleId)
                .Include(rp => rp.Permission)
                .Select(rp => rp.Permission.Name)
                .ToListAsync();

            // Protección: Si el usuario no tiene rol o no tiene permisos, denegar acceso
            if (user.Role == null || permissions == null || permissions.Count == 0)
            {
                return Unauthorized(new { Message = "El usuario no tiene un rol válido o no tiene permisos asignados." });
            }

            // Verificar que el usuario tenga al menos una suscripción activa
            var activeSubscription = user.Subscriptions?.FirstOrDefault(s => s.Status == "active");
            if (activeSubscription != null)
            {
                var plan = activeSubscription.Plan;
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
                    Role = user.Role != null ? new { user.Role.Id, user.Role.Name } : null,
                    Permissions = permissions,
                    Plan = activeSubscription?.Plan != null ? new { activeSubscription.Plan.Id, activeSubscription.Plan.Name } : null
                }
            });
        }

    }
}
