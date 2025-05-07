using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using Voia.Api.Services;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Hubs;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// CONFIGURAR SERVICIOS

// Configuración de CORS (para acceder desde dominios específicos)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", builder =>
    {
        builder.WithOrigins("https://miappfrontend.com")  // Tu frontend
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Configuración de DBContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);

// Servicio de JWT
builder.Services.AddScoped<JwtService>();

// Configuración de Autenticación y Autorización con JWT
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

// Configuración de Swagger con JWT + XML
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Voia API", 
        Version = "v1",
        Description = "API para la gestión de usuarios, roles, permisos y chat."
    });

    // Seguridad JWT en Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa el token JWT con el prefijo 'Bearer ', por ejemplo: Bearer eyJhbGciOi..."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Documentación XML (asegúrate de que esté habilitado en el .csproj)
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// MIDDLEWARE

// Asegúrate de usar HTTPS en todo momento
app.UseHttpsRedirection();

// Habilitar Swagger solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Voia API v1");
        c.RoutePrefix = string.Empty; // Opcional: hace que Swagger se muestre en la raíz
    });
}

app.UseCors("AllowFrontend"); // 👈 Este debe ir aquí

app.UseAuthentication(); 
app.UseAuthorization();

// Mapeo de controladores
app.MapControllers();

// Mapeo de SignalR (para chat en tiempo real)
app.MapHub<ChatHub>("/chatHub");

// Ejecutar la aplicación
app.Run();
