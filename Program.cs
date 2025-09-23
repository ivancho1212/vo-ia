using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query; // Add this line
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Voia.Api.Data;
using Voia.Api.Hubs;
using Voia.Api.Services;
using Voia.Api.Services.Interfaces;
using System.Net.Http.Headers;
using Voia.Api.Services.IAProviders;
using Voia.Api.Services.Chat;
using Api.Services;
using Voia.Api.Services.Mocks;


var builder = WebApplication.CreateBuilder(args);

// CONFIGURAR SERVICIOS
builder.Services.AddScoped<JwtService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection")),
        mySqlOptions => mySqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery) // ✅ Aquí dentro
    )
    .EnableSensitiveDataLogging() // ⚠️ Solo en desarrollo
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
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    );
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10 MB
});

builder.Services.AddEndpointsApiExplorer();

// Hosted services
builder.Services.AddHostedService<InactiveConversationWorker>();

// SWAGGER con JWT + XML
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Voia API",
        Version = "v1",
        Description = "API para la gestión de usuarios, roles, permisos y chat.",
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

// Servicios de la aplicación
builder.Services.AddHttpClient<FastApiService>();

builder.Services.AddScoped<IAiProviderService, MockAiProviderService>();
builder.Services.AddScoped<IChatFileService, ChatFileService>();
builder.Services.AddScoped<IAClientFactory>();
builder.Services.AddScoped<BotDataCaptureService>();
builder.Services.AddScoped<DataExtractionService>(); // ✅ FIX: Registrar el nuevo servicio

builder.Services.AddScoped<TextExtractionService>();
builder.Services.AddScoped<TextChunkingService>();
builder.Services.AddSingleton<TokenCounterService>();

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

// 👇 INICIO: Configuración del HttpClient para ChatHub
// Esto es necesario para que ChatHub pueda hacer llamadas a otros endpoints de la misma API.
builder.Services.AddHttpClient<ChatHub>(client =>
{
    // Asigna la dirección base del servidor. En un entorno de producción,
    // esta URL debería venir de appsettings.json.
    // Para desarrollo, asumimos que la API corre en el mismo host y puerto.
    client.BaseAddress = new Uri("http://localhost:5006"); // <-- AJUSTA EL PUERTO SI ES DIFERENTE
});
// 👆 FIN: Configuración del HttpClient para ChatHub

// Vector search service
builder.Services.AddHttpClient<VectorSearchService>();

// FIN CONFIGURACIÓN DE SERVICIOS

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseStaticFiles();

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
