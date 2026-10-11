using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using DreamApp.Api.Common;
using DreamApp.Api.Data;
using DreamApp.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// Database: SQLite file path is configurable (env var DATABASE_PATH wins,
// then Database:Path from appsettings, then a sane local default). This
// is what lets the same code run in development (relative "data/" folder)
// and in production (an absolute path on the host's persistent volume).
// ---------------------------------------------------------------------
var databasePath = Environment.GetEnvironmentVariable("DATABASE_PATH")
    ?? builder.Configuration["Database:Path"]
    ?? "App_Data/dreamapp.db";
databasePath = Path.GetFullPath(databasePath, builder.Environment.ContentRootPath);
Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));

// Controllers depend on the narrower IAppDbContext interface (see Data/IAppDbContext.cs)
// so they can be unit-tested against an in-memory fake without a real database.
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

// ---------------------------------------------------------------------
// Application services
// ---------------------------------------------------------------------
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IImageStorageService, LocalImageStorageService>();

// ---------------------------------------------------------------------
// Controllers + JSON (enums serialized as readable strings, e.g. "High")
// ---------------------------------------------------------------------
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "DreamApp API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Incolla qui solo il token JWT ottenuto da /api/auth/login (senza il prefisso 'Bearer ').",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ---------------------------------------------------------------------
// JWT authentication. The signing key is NEVER hardcoded or committed:
// - development: `dotnet user-secrets set "Jwt:Key" "<random-string>"`
// - production:  environment variable Jwt__Key (double underscore, the
//                standard ASP.NET Core convention for nested config keys)
// ---------------------------------------------------------------------
// Validation parameters are built lazily from the final IConfiguration (not from
// builder.Configuration at startup), so they always use the same Jwt:* values that
// JwtTokenService signs tokens with — including overrides applied by the test host.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, configuration) =>
    {
        var jwtKey = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException(
                "Jwt:Key non configurato. In sviluppo: 'dotnet user-secrets set \"Jwt:Key\" \"<stringa-casuale-lunga>\"' " +
                "dentro backend/DreamApp.Api. In produzione: variabile d'ambiente Jwt__Key. Vedi docs/architecture.md.");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = configuration["Jwt:Issuer"] ?? "DreamApp",
            ValidateAudience = true,
            ValidAudience = configuration["Jwt:Audience"] ?? "DreamAppClients",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// ---------------------------------------------------------------------
// CORS: only the configured frontend origins may call the API from a
// browser. Configure Cors:AllowedOrigins in appsettings/production env
// (e.g. the GitHub Pages URL). Falls back to the local Angular dev server.
// ---------------------------------------------------------------------
const string CorsPolicyName = "DreamAppFrontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (allowedOrigins is null || allowedOrigins.Length == 0)
{
    allowedOrigins = new[] { "http://localhost:4200" };
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
});

// ---------------------------------------------------------------------
// Rate limiting: slows down password guessing on login/register. Applied
// only to endpoints marked [EnableRateLimiting("auth")]. The limit is per
// client IP and per minute (RateLimiting:AuthPermitLimit, default 20 — generous
// for real users, and shared between everyone behind the same proxy IP).
// ---------------------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ApiErrorDto("Troppi tentativi. Riprova tra qualche minuto."), cancellationToken);
    };
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                // Read lazily from the request's configuration so tests can override it.
                PermitLimit = httpContext.RequestServices.GetRequiredService<IConfiguration>()
                    .GetValue<int?>("RateLimiting:AuthPermitLimit") ?? 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

// Fail fast at startup if the signing key is missing (checked on the final configuration).
if (string.IsNullOrWhiteSpace(app.Configuration["Jwt:Key"]))
{
    throw new InvalidOperationException(
        "Jwt:Key non configurato. In sviluppo: 'dotnet user-secrets set \"Jwt:Key\" \"<stringa-casuale-lunga>\"' " +
        "dentro backend/DreamApp.Api. In produzione: variabile d'ambiente Jwt__Key. Vedi docs/architecture.md.");
}

// Apply pending EF Core migrations automatically at startup. Requires that
// at least one migration has been generated first — see docs/database.md.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicyName);

// Serve uploaded gift photos (see LocalImageStorageService) at /uploads/*.
var imagesPath = LocalImageStorageService.ResolveImagesPath(builder.Configuration);
Directory.CreateDirectory(imagesPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imagesPath),
    RequestPath = "/uploads"
});

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposes the implicit Program class to WebApplicationFactory<Program> in the test project.
public partial class Program
{
}
