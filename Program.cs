﻿using System.Security.Claims;
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
using Api.Services;
using Voia.Api.Services.Mocks;
using System.Text.Json.Serialization;
using Voia.Api.Models.BotIntegrations;

var builder = WebApplication.CreateBuilder(args);

// CONFIGURAR SERVICIOS
builder.Services.AddScoped<JwtService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection")),
        mySqlOptions => mySqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
    )
    .EnableSensitiveDataLogging()
    .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information)
);

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://127.0.0.1:3000",
                "http://127.0.0.1:5500",
                "http://localhost:5500",
                "https://voia-client.lat"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("X-Widget-Token");
    });

    // Poltica especfica para widgets - permitir todos los orgenes por ahora
    // TODO: Implementar validacin de dominios ms adelante con un servicio apropiado
    options.AddPolicy("AllowWidgets", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Configure SignalR and optional Redis backplane
var signalRBuilder = builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10 MB
});

// Redis optional: if REDIS_CONNECTION provided, register RedisService and PresenceService.
// NOTE: To enable the SignalR Redis backplane (distribute groups across instances) add the
// NuGet package Microsoft.AspNetCore.SignalR.StackExchangeRedis and uncomment the sample below.
// signalRBuilder.AddStackExchangeRedis(redisConnection, options => { options.Configuration.ChannelPrefix = "voia-signalr"; });

var redisConnection = builder.Configuration["REDIS_CONNECTION"]; // env var or appsettings
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
    client.Timeout = TimeSpan.FromSeconds(5); // Timeout más corto para evitar bloqueos
});

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

// CORS debe ir DESPUS de UseRouting pero ANTES de UseAuthentication
app.UseCors("AllowFrontend"); // Default para el dashboard

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHub<ChatHub>("/chatHub");

app.Run();
