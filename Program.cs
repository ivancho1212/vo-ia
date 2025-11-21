using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Voia.Api.Data;
using Voia.Api.Hubs;
using Voia.Api.Services;
using Voia.Api.Services.Interfaces;
using System.Net.Http.Headers;
using Voia.Api.Services.IAProviders;
using Voia.Api.Services.Chat;
using Voia.Api.Services.Upload;
using Voia.Api.Services.Security;
using Api.Services;
using Voia.Api.Services.Mocks;
using System.Text.Json.Serialization;
using Voia.Api.Models.BotIntegrations;
using Voia.Api.Middleware;
using StackExchange.Redis;
using Voia.Api.Data.Interceptors;
using Serilog;
using Serilog.Events;
using Voia.Api.Services.Caching;
using DotNetEnv;

//  Load environment variables from .env file
var envPath = Path.Combine(AppContext.BaseDirectory, ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);

//  SERILOG CONFIGURATION
// Configurar Serilog para structured logging con file sinks y rotacin
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentUserName()
    .Enrich.WithProperty("Application", "Voia.Api")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: Path.Combine("Logs", "voia-api-.txt"),
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 31457280, // 30 MB
        retainedFileCountLimit: 30,
        shared: true)
    .WriteTo.File(
        path: Path.Combine("Logs", "voia-api-errors-.txt"),
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        restrictedToMinimumLevel: LogEventLevel.Error,
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 31457280, // 30 MB
        retainedFileCountLimit: 30,
        shared: true)
    .CreateLogger();

// Reemplazar el logger de ASP.NET Core con Serilog
builder.Host.UseSerilog();

// CONFIGURAR SERVICIOS
builder.Services.AddScoped<JwtService>();
builder.Services.AddSingleton<IGeoLocationService, GeoLocationService>();
builder.Services.AddHttpContextAccessor();

// Registrar Identity para habilitar UserManager<User> y dependencias
builder.Services.AddIdentity<Voia.Api.Models.User, Voia.Api.Models.Role>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var auditInterceptor = serviceProvider.GetRequiredService<AuditInterceptor>();
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection")),
        mySqlOptions => mySqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
    )
    .EnableSensitiveDataLogging()
    .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information)
    .AddInterceptors(auditInterceptor);
});

builder.Services.AddScoped<AuditInterceptor>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var key = builder.Configuration["Jwt:Key"];
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!)),
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.NameIdentifier
    };

    // Support receiving access token from query string for SignalR negotiate/ws requests.
    // This keeps standard Authorization header behavior while allowing the client
    // SignalR JS to send the token as a query param (access_token) during negotiate.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var request = context.Request;
            // Check query string for access_token or token when connecting to SignalR hub
            var accessToken = request.Query["access_token"].FirstOrDefault() ?? request.Query["token"].FirstOrDefault();
            if (!string.IsNullOrEmpty(accessToken) && request.Path.StartsWithSegments("/chatHub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

//  CORS CONFIGURATION - Extract origins from environment variables for production flexibility
var frontendOrigins = (builder.Configuration["CORS:FrontendOrigins"] ?? 
    "http://localhost:3000,http://127.0.0.1:3000,http://127.0.0.1:5500,http://localhost:5500,https://voia-client.lat")
    .Split(",")
    .Select(o => o.Trim())
    .Where(o => !string.IsNullOrEmpty(o))
    .ToArray();

var widgetOrigins = (builder.Configuration["CORS:WidgetOrigins"] ?? 
    "http://localhost:3000,http://127.0.0.1:3000,http://localhost:5006,http://127.0.0.1:5006")
    .Split(",")
    .Select(o => o.Trim())
    .Where(o => !string.IsNullOrEmpty(o))
    .ToArray();

builder.Services.AddCors(options =>
{
    //  Policy: AllowFrontend - Strict whitelist for dashboard access
    // Only trusted frontend origins with credentials enabled
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(frontendOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("X-Widget-Token");
    });

    //  Policy: AllowWidgets - For embedded widgets (carefully restricted)
    // NOTE: Widgets require cross-origin access for image embedding.
    // In production, consider:
    // 1. Restricting to known CDN/hosting domains
    // 2. Using Content-Security-Policy headers
    // 3. Implementing origin validation in code for sensitive operations
    options.AddPolicy("AllowWidgets", policy =>
    {
        //  DEVELOPMENT: Explicitly allow localhost for widget development
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://127.0.0.1:3000",
                "http://localhost:5006",
                "http://127.0.0.1:5006"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("X-Widget-Token", "Content-Type", "X-Content-Type-Options");
    });

    //  Policy: AllowLocalhost - Development policy for local testing
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:5006",
                "http://127.0.0.1:3000",
                "http://127.0.0.1:5006"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("X-Widget-Token", "Content-Type", "X-Content-Type-Options");
    });
});

// Log CORS configuration for debugging
Log.Information(" CORS Configuration Loaded");
Log.Information("   Frontend Origins: {FrontendOrigins}", string.Join(", ", frontendOrigins));
Log.Information("   Widget Origins: {WidgetOrigins}", string.Join(", ", widgetOrigins));


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

//  GET REDIS CONNECTION STRING (needed early for cache configuration)
var redisConnection = builder.Configuration["REDIS_CONNECTION"]; // env var or appsettings

//  CSRF TOKEN SERVICE
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICsrfTokenService, CsrfTokenService>();

//  REDIS DISTRIBUTED CACHE CONFIGURATION
// Implementar IDistributedCache para cach distribuido (using Microsoft.Extensions.Caching.StackExchangeRedis)
// TTLs: Bots (1h), Templates (24h), Users (1h)
if (!string.IsNullOrEmpty(redisConnection))
{
    try
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.ConfigurationOptions = StackExchange.Redis.ConfigurationOptions.Parse(redisConnection);
            options.ConfigurationOptions.AbortOnConnectFail = false;
        });
        Log.Information(" Redis Distributed Cache configured successfully");
    }
    catch (Exception ex)
    {
        Log.Warning($" Redis Distributed Cache configuration failed: {ex.Message}. Falling back to Distributed Memory Cache.");
        builder.Services.AddDistributedMemoryCache();
    }
}
else
{
    // Fallback to Distributed Memory Cache if Redis not available
    Log.Warning(" REDIS_CONNECTION not configured. Using Distributed Memory Cache (single-instance only).");
    builder.Services.AddDistributedMemoryCache();
}

//  REGISTER CACHE SERVICE
// Centralizado para Get/Set/GetOrSet con TTL automtico
builder.Services.AddScoped<ICacheService, CacheService>();

//  REGISTER XXE PROTECTION SERVICE
// Proteccin contra ataques XML External Entity
builder.Services.AddScoped<IXxeProtectionService, XxeProtectionService>();

//  REGISTER SANITIZATION SERVICE
// Proteccin contra XSS mediante sanitizacin de HTML y texto
builder.Services.AddScoped<ISanitizationService, SanitizationService>();

//  REGISTER FILE ACCESS AUTHORIZATION SERVICE
// Validacin de permisos de acceso a archivos basada en propiedad de recursos
builder.Services.AddScoped<IFileAccessAuthorizationService, FileAccessAuthorizationService>();

//  REGISTER PROMPT INJECTION PROTECTION SERVICE
// Prevencin de prompt injection attacks en prompts de LLM
builder.Services.AddScoped<IPromptInjectionProtectionService, PromptInjectionProtectionService>();

// Configure SignalR and optional Redis backplane
var signalRBuilder = builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10 MB
});

// Redis optional: if REDIS_CONNECTION provided, register RedisService and PresenceService.
// NOTE: To enable the SignalR Redis backplane (distribute groups across instances) add the
// NuGet package Microsoft.AspNetCore.SignalR.StackExchangeRedis and uncomment the sample below.
// signalRBuilder.AddStackExchangeRedis(redisConnection, options => { options.Configuration.ChannelPrefix = "voia-signalr"; });

if (!string.IsNullOrEmpty(redisConnection))
{
    // Register RedisService and PresenceService when redis is configured
    builder.Services.AddSingleton<RedisService>(sp =>
    {
        var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RedisService>>();
        return new RedisService(redisConnection, logger);
    });
    builder.Services.AddSingleton<PresenceService>(sp =>
    {
        var redis = sp.GetService<RedisService>();
        var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<PresenceService>>();
        return new PresenceService(redis, logger);
    });
}
else
{
    // No redis: register a PresenceService with null Redis (in-memory fallback could be added later)
    builder.Services.AddSingleton<PresenceService>(sp =>
    {
        var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<PresenceService>>();
        return new PresenceService(null, logger);
    });
}

builder.Services.AddEndpointsApiExplorer();

// Hosted services
builder.Services.AddHostedService<InactiveConversationWorker>();

// Message processing queue and worker - use RedisStreamMessageQueue when Redis is configured, otherwise fallback to in-memory
if (!string.IsNullOrEmpty(redisConnection))
{
    // RedisService is already registered above when redisConnection is set
    builder.Services.AddSingleton<RedisStreamMessageQueue>();
    builder.Services.AddSingleton<IMessageQueue>(sp => sp.GetRequiredService<RedisStreamMessageQueue>());
}
else
{
    builder.Services.AddSingleton<InMemoryMessageQueue>();
    builder.Services.AddSingleton<IMessageQueue>(sp => sp.GetRequiredService<InMemoryMessageQueue>());
}

builder.Services.AddHostedService<MessageProcessingWorker>();

// SWAGGER con JWT + XML
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Voia API",
        Version = "v1",
        Description = "API para la gestin de usuarios, roles, permisos y chat.",
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa el token JWT con el prefijo 'Bearer ', por ejemplo: Bearer eyJhbGciOi...",
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            Array.Empty<string>()
        },
    });

    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    c.IncludeXmlComments(xmlPath);
});

// Servicios de la aplicacin
builder.Services.AddHttpClient<FastApiService>();

builder.Services.AddScoped<IAiProviderService, MockAiProviderService>();
builder.Services.AddScoped<IChatFileService, ChatFileService>();
// File signature checker validates magic numbers and blocks executables/scripts before
// moving files to the public uploads folder. In production this can be replaced or
// augmented with cloud storage validation + antivirus scanning.
builder.Services.AddSingleton<Voia.Api.Services.Upload.IFileSignatureChecker, Voia.Api.Services.Upload.FileSignatureChecker>();
builder.Services.AddScoped<IAClientFactory>();
builder.Services.AddScoped<BotDataCaptureService>();
builder.Services.AddScoped<DataExtractionService>();

builder.Services.AddScoped<TextExtractionService>();
builder.Services.AddScoped<TextChunkingService>();
builder.Services.AddSingleton<TokenCounterService>();
builder.Services.AddScoped<JwtService>();

// HttpClients para proveedores de IA
builder.Services.AddHttpClient<OpenAIClient>();

builder.Services.AddHttpClient<DeepSeekClient>()
    .ConfigureHttpClient((sp, client) =>
    {
        using var scope = sp.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var config = context.BotIaProviders.FirstOrDefault(p => p.Name == "deepseek");
        if (config != null)
        {
            client.BaseAddress = new Uri(config.ApiEndpoint);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", config.ApiKey);
        }
    });

builder.Services.AddHttpClient<GeminiClient>()
    .ConfigureHttpClient((sp, client) =>
    {
        using var scope = sp.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var config = context.BotIaProviders.FirstOrDefault(p => p.Name == "gemini");
        if (config != null)
        {
            client.BaseAddress = new Uri(config.ApiEndpoint);
        }
    });

// Registro de PromptBuilderService con HttpClient
builder.Services.AddHttpClient<PromptBuilderService>();

// Presigned upload service (in-memory local implementation). In production this can be
// replaced by a service that requests signed URLs from S3 / Azure Blob.
builder.Services.AddSingleton<Voia.Api.Services.Chat.IPresignedUploadService, Voia.Api.Services.Chat.PresignedUploadService>();

// Configuracin del HttpClient para ChatHub
// Esto es necesario para que ChatHub pueda hacer llamadas a otros endpoints de la misma API.
builder.Services.AddHttpClient<ChatHub>(client =>
{
    // Asigna la direccin base del servidor. En un entorno de produccin,
    // esta URL debera venir de appsettings.json.
    // Para desarrollo, asumimos que la API corre en el mismo host y puerto.
    client.BaseAddress = new Uri("http://localhost:5006");
});

// Vector search service
builder.Services.AddHttpClient<VectorSearchService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(5); // Timeout ms corto para evitar bloqueos
});

//  RATE LIMITING CONFIGURATION
// Registrar Rate Limiting Middleware con opciones por defecto (60 req/min)
var rateLimitOptions = new RateLimitOptions
{
    RequestsPerMinute = int.TryParse(builder.Configuration["RateLimit:RequestsPerMinute"], out var limit) ? limit : 60,
    Enabled = builder.Configuration.GetValue("RateLimit:Enabled", true)
};
builder.Services.AddSingleton(rateLimitOptions);

// Registrar IConnectionMultiplexer para Rate Limiting (si Redis est disponible)
IConnectionMultiplexer? redisMultiplexer = null;
if (!string.IsNullOrEmpty(redisConnection))
{
    try
    {
        redisMultiplexer = ConnectionMultiplexer.Connect(redisConnection);
        builder.Services.AddSingleton(redisMultiplexer);
    }
    catch (Exception ex)
    {
        Console.WriteLine($" Redis connection failed for Rate Limiting: {ex.Message}. Falling back to in-memory.");
    }
}

//  JWT REFRESH TOKEN CONFIGURATION
builder.Services.AddScoped<IJwtRefreshTokenService, JwtRefreshTokenService>();

//  NOTE: CORS policies already configured above - see "AllowFrontend", "AllowWidgets", "AllowLocalhost"
// No need for additional CORS configuration here.

// FIN CONFIGURACIN DE SERVICIOS

var app = builder.Build();

// Configure static files
app.UseStaticFiles(); // For wwwroot folder

// Configure static files for uploads
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "Uploads")),
    RequestPath = "/uploads"
});

app.UseRouting();

// CORS must go AFTER UseRouting() but BEFORE MapControllers()
// This allows controller-level [EnableCors] attributes to work properly
app.UseCors("AllowFrontend"); // Default for dashboard

// Authentication/Authorization must be after UseRouting
app.UseAuthentication();
app.UseAuthorization();

//  USE CSRF PROTECTION MIDDLEWARE
// Valida CSRF tokens en POST, PUT, PATCH, DELETE
// OPTIONS requests (CORS preflight) are always allowed through
app.UseMiddleware<CsrfProtectionMiddleware>();

//  USE SECURITY HEADERS MIDDLEWARE
// Agrega headers de seguridad (X-Frame-Options, CSP, HSTS, etc)
app.UseMiddleware<SecurityHeadersMiddleware>();

//  USE CORS ORIGIN VALIDATION MIDDLEWARE
// Validacin adicional de CORS para endpoints crticos (admin, security, data deletion)
// Se ejecuta DESPUS del middleware CORS estndar
app.UseMiddleware<CorsOriginValidationMiddleware>();

//  REQUEST/RESPONSE LOGGING MIDDLEWARE
// Registra informacin de requests, responses, duracin y contexto
app.UseMiddleware<RequestResponseLoggingMiddleware>();

//  USE RATE LIMITING MIDDLEWARE
// Debe ir DESPUS de authentication/authorization pero ANTES de map endpoints
if (rateLimitOptions.Enabled)
{
    app.UseMiddleware<RateLimitingMiddleware>();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHub<ChatHub>("/chatHub");

app.Run();


