using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Voia.Api.Data;
using Voia.Api.Hubs;
using Voia.Api.Services;
using Voia.Api.Services.Interfaces;
using System.Net.Http.Headers;
using Voia.Api.Services.IAProviders;
using Voia.Api.Services.Chat;


var builder = WebApplication.CreateBuilder(args);

// CONFIGURAR SERVICIOS
builder.Services.AddScoped<JwtService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
    // Activamos el logging de EF Core hacia la console
    .EnableSensitiveDataLogging()
    .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information)
);


builder
    .Services.AddAuthentication(options =>
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

            // ✅ ESTA LÍNEA es la clave
            RoleClaimType = ClaimTypes.Role,
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

builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true; // Acepta PascalCase y camelCase
    });
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10 MB de archivos (ajusta si necesitas más)
});
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHostedService<InactiveConversationWorker>();
// ✅ SWAGGER CON JWT + XML
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "Voia API",
            Version = "v1",
            Description = "API para la gestión de usuarios, roles, permisos y chat.",
        }
    );

    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "Ingresa el token JWT con el prefijo 'Bearer ', por ejemplo: Bearer eyJhbGciOi...",
        }
    );

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
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
        }
    );

    // Documentación XML (asegúrate de que esté habilitado en el .csproj)
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    c.IncludeXmlComments(xmlPath);
});
builder.Services.AddHttpClient<FastApiService>();

builder.Services.AddScoped<IAiProviderService, AiProviderService>();

builder.Services.AddScoped<IChatFileService, ChatFileService>();

builder.Services.AddScoped<IAClientFactory>();

// ✅ Registro seguro de HttpClients para cada proveedor
builder.Services.AddHttpClient<OpenAIClient>(client =>
{
    // Configuración base si aplica (por ejemplo, timeout)
});

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
            // Gemini usa la API key en la query, no requiere auth header
        }
    });

builder.Services.AddScoped<TextExtractionService>();
builder.Services.AddScoped<TextChunkingService>();


var app = builder.Build();


app.UseCors("AllowFrontend"); // 👈 Mover CORS arriba
app.UseStaticFiles();         // ✅ Luego los archivos estáticos

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