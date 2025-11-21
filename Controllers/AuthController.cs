using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Services;
using Voia.Api.Models.DTOs;
using Voia.Api.Models.Users;
using Voia.Api.Models;
using Voia.Api.Data;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ICsrfTokenService _csrfTokenService;
        private readonly IJwtRefreshTokenService _jwtService;
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ICsrfTokenService csrfTokenService,
            IJwtRefreshTokenService jwtService,
            UserManager<User> userManager,
            ApplicationDbContext context,
            ILogger<AuthController> logger)
        {
            _csrfTokenService = csrfTokenService;
            _jwtService = jwtService;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Genera un nuevo CSRF token para el cliente
        /// Se debe llamar al cargar la página o antes de hacer POST/PUT/DELETE
        /// </summary>
        /// <returns>Objeto con el token CSRF</returns>
        [HttpGet("csrf-token")]
        [AllowAnonymous]
        public IActionResult GetCsrfToken()
        {
            try
            {
                var token = _csrfTokenService.GenerateToken();
                
                // También establecer en cookie para mayor seguridad (double-submit cookie pattern)
                Response.Cookies.Append(
                    _csrfTokenService.GetCookieName(),
                    token,
                    new CookieOptions
                    {
                        HttpOnly = false, // Necesita ser accesible desde JS
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Strict,
                        MaxAge = TimeSpan.FromHours(1)
                    }
                );

                return Ok(new
                {
                    token = token,
                    header_name = _csrfTokenService.GetHeaderName(),
                    expires_in_seconds = 3600
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generando CSRF token: {ex.Message}");
                return StatusCode(500, new { error = "Failed to generate CSRF token" });
            }
        }

        /// <summary>
        /// Autentica un usuario y retorna access token + refresh token (cookie)
        /// </summary>
        /// <param name="request">Email y password del usuario</param>
        /// <returns>AccessToken y información del usuario</returns>
        /// <response code="200">Login exitoso, access token en response, refresh token en httpOnly cookie</response>
        /// <response code="401">Email o contraseña incorrectos</response>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrEmpty(request?.Email) || string.IsNullOrEmpty(request?.Password))
                return BadRequest(new { error = "Email and password are required" });

            try
            {
                // Validar credenciales
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
                {
                    _logger.LogWarning($"Login attempt con email inválido: {request.Email}");
                    return Unauthorized(new { error = "Invalid email or password" });
                }

                // Generar tokens
                var accessToken = _jwtService.GenerateAccessToken(user.Id.ToString(), user.Email, user.Email);
                var refreshToken = _jwtService.GenerateRefreshToken();

                // Guardar refresh token en base de datos
                var refreshTokenEntity = new RefreshToken
                {
                    UserId = user.Id.ToString(),
                    Token = refreshToken,
                    TokenJti = Guid.NewGuid().ToString(),
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString()
                };

                await _context.RefreshTokens.AddAsync(refreshTokenEntity);
                await _context.SaveChangesAsync();

                // Configurar httpOnly refresh token cookie
                Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
                {
                    HttpOnly = true, // No accesible desde JS
                    Secure = true, // Solo en HTTPS
                    SameSite = SameSiteMode.Strict, // Protege contra CSRF
                    Expires = DateTime.UtcNow.AddDays(7)
                });

                _logger.LogInformation($"Usuario logueado exitosamente: {user.Email}");

                return Ok(new LoginResponse
                {
                    AccessToken = accessToken,
                    TokenType = "Bearer",
                    ExpiresIn = 900, // 15 minutos en segundos
                    User = new UserInfoDto
                    {
                        Id = user.Id.ToString(),
                        Email = user.Email,
                        Name = user.Email,
                        UserName = user.Email
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en login: {ex.Message}");
                return StatusCode(500, new { error = "An error occurred during login" });
            }
        }

        /// <summary>
        /// Renueva el access token usando el refresh token
        /// El refresh token debe estar en la httpOnly cookie
        /// </summary>
        /// <returns>Nuevo access token</returns>
        /// <response code="200">Token refrescado exitosamente</response>
        /// <response code="401">Refresh token inválido o expirado</response>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh()
        {
            try
            {
                // Obtener refresh token de la cookie
                if (!Request.Cookies.TryGetValue("refresh_token", out var refreshTokenValue))
                {
                    _logger.LogWarning("Intento de refresh sin token en cookie");
                    return Unauthorized(new { error = "Refresh token not found" });
                }

                // Validar refresh token en base de datos
                var storedToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == refreshTokenValue && !rt.IsRevoked);

                if (storedToken == null)
                {
                    _logger.LogWarning("Intento de refresh con token no encontrado");
                    return Unauthorized(new { error = "Invalid refresh token" });
                }

                if (storedToken.ExpiryDate < DateTime.UtcNow)
                {
                    _logger.LogWarning($"Intento de refresh con token expirado para usuario: {storedToken.UserId}");
                    return Unauthorized(new { error = "Refresh token expired" });
                }

                // Obtener usuario
                var user = await _userManager.FindByIdAsync(storedToken.UserId);
                if (user == null)
                {
                    _logger.LogWarning($"Usuario no encontrado para refresh token: {storedToken.UserId}");
                    return Unauthorized(new { error = "User not found" });
                }

                // Generar nuevo access token
                var newAccessToken = _jwtService.GenerateAccessToken(user.Id.ToString(), user.Email, user.Email);

                // Generar nuevo refresh token (token rotation)
                var newRefreshToken = _jwtService.GenerateRefreshToken();
                storedToken.Token = newRefreshToken;
                storedToken.ExpiryDate = DateTime.UtcNow.AddDays(7);
                storedToken.LastUsedAt = DateTime.UtcNow;
                _context.RefreshTokens.Update(storedToken);
                await _context.SaveChangesAsync();

                // Actualizar cookie con nuevo refresh token
                Response.Cookies.Append("refresh_token", newRefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(7)
                });

                _logger.LogInformation($"Token refrescado para usuario: {user.Email}");

                return Ok(new RefreshResponse
                {
                    AccessToken = newAccessToken,
                    TokenType = "Bearer",
                    ExpiresIn = 900 // 15 minutos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en refresh: {ex.Message}");
                return StatusCode(500, new { error = "An error occurred during token refresh" });
            }
        }

        /// <summary>
        /// Cierra la sesión del usuario revocando su refresh token
        /// </summary>
        /// <returns>Mensaje de confirmación</returns>
        /// <response code="200">Logout exitoso</response>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Obtener y revocar refresh token si existe
                if (Request.Cookies.TryGetValue("refresh_token", out var refreshToken))
                {
                    var storedToken = await _context.RefreshTokens
                        .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

                    if (storedToken != null && !storedToken.IsRevoked)
                    {
                        storedToken.IsRevoked = true;
                        _context.RefreshTokens.Update(storedToken);
                        await _context.SaveChangesAsync();
                    }
                }

                // Limpiar cookie
                Response.Cookies.Delete("refresh_token");

                _logger.LogInformation($"Usuario logueado fuera");

                return Ok(new LogoutResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en logout: {ex.Message}");
                return StatusCode(500, new { error = "An error occurred during logout" });
            }
        }
    }
}
