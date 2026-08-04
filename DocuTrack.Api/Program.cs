using DocuTrack.Api.ExceptionHandling;
using DocuTrack.Api.Identity;
using DocuTrack.Core.Repositories;
using DocuTrack.Core.Services;
using DocuTrack.Infrastructure.Identity;
using DocuTrack.Infrastructure.Persistence;
using DocuTrack.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using DocuTrack.Core.Identity;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// Controllers and JSON
// ---------------------------------------------------------

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });


// ---------------------------------------------------------
// OpenAPI
// ---------------------------------------------------------

builder.Services.AddOpenApi();

// ---------------------------------------------------------
// Global exception handling
// ---------------------------------------------------------

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ---------------------------------------------------------
// Database
// Integration tests register their own in-memory DbContext.
// ---------------------------------------------------------

if (!builder.Environment.IsEnvironment("Testing"))
{
    string connectionString = builder.Configuration.GetConnectionString("DocuTrackDb")
        ?? throw new InvalidOperationException(
            "Connection string 'DocuTrackDb' was not found.");

    builder.Services.AddDbContext<DocuTrackDbContext>(
        options =>
        {
            options.UseSqlServer(connectionString);
        });
}

// ---------------------------------------------------------
// Data Protection
// Required by Identity token providers.
// ---------------------------------------------------------

builder.Services.AddDataProtection();


// ---------------------------------------------------------
// ASP.NET Core Identity
// ---------------------------------------------------------

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredUniqueChars = 1;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<DocuTrackDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// ---------------------------------------------------------
// Seed-admin settings
// ---------------------------------------------------------

builder.Services.Configure<SeedAdminSettings>(
    builder.Configuration.GetSection("SeedAdmin"));

// ---------------------------------------------------------
// JWT settings
// ---------------------------------------------------------

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

JwtSettings jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                            ?? throw new InvalidOperationException(
                               "JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
{
    throw new InvalidOperationException("JWT issuer is missing.");
}
if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
{
    throw new InvalidOperationException("JWT audience is missing.");
}
if (string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    throw new InvalidOperationException("JWT signing key is missing.");
}
if (Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32)
{
    throw new InvalidOperationException("JWT signing key must be at least 32 bytes.");
}

// ---------------------------------------------------------
// JWT authentication
// ---------------------------------------------------------

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            };
    });

// ---------------------------------------------------------
// Authorization
// ---------------------------------------------------------

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();

// ---------------------------------------------------------
// Application Repositories
// ---------------------------------------------------------

builder.Services.AddScoped<IDocumentRepository, EfDocumentRepository>();

// ---------------------------------------------------------
// Application services
// ---------------------------------------------------------

builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IIdentityTransactionFactory, EfIdentityTransactionFactory>();

// ---------------------------------------------------------
// Build application
// ---------------------------------------------------------

var app = builder.Build();

// ---------------------------------------------------------
// Middleware
// ---------------------------------------------------------

app.UseExceptionHandler();

// ---------------------------------------------------------
// Development API documentation
// Built-in OpenAPI generates the document; Swagger UI is
// a separate visualization package.
// ---------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "DocuTrack API v1");
    });
}

// ---------------------------------------------------------
// Development seed data
// Skip this during integration testing.
// ---------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    await RoleSeeder.SeedAsync(app.Services);
    await UserSeeder.SeedAsync(app.Services);
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Required for WebApplicationFactory<Program> integration tests.
public partial class Program;