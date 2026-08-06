using DocuTrack.Api.DependencyInjection;
using DocuTrack.Api.ExceptionHandling;
using DocuTrack.Api.HealthChecks;
using DocuTrack.Api.Identity;
using DocuTrack.Application;
using DocuTrack.Application.Abstractions.Authorization;
using DocuTrack.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// Controllers and JSON
// ---------------------------------------------------------

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions
            .Converters
            .Add(new JsonStringEnumConverter());
    });

// ---------------------------------------------------------
// ProblemDetails and global exception handling
// ---------------------------------------------------------

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<
    GlobalExceptionHandler>();

// ---------------------------------------------------------
// Application and Infrastructure
// ---------------------------------------------------------

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    builder.Configuration,
    builder.Environment);

builder.Services.AddApiRateLimiting();
// ---------------------------------------------------------
// Authentication and authorization
// ---------------------------------------------------------

builder.Services.AddApiAuthentication(
    builder.Configuration);

builder.Services.AddApiAuthorization();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<
    ICurrentUser,
    CurrentUser>();

// ---------------------------------------------------------
// Swagger
// ---------------------------------------------------------

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerDocumentation();

// ---------------------------------------------------------
// Seed settings
// ---------------------------------------------------------

builder.Services.Configure<SeedAdminSettings>(
    builder.Configuration.GetSection(
        "SeedAdmin"));

// ---------------------------------------------------------
// Build
// ---------------------------------------------------------

var app = builder.Build();

// ---------------------------------------------------------
// Middleware
// ---------------------------------------------------------

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "DocuTrack API v1");

        options.DocumentTitle =
            "DocuTrack API Documentation";

        options.DisplayRequestDuration();
    });

    await RoleSeeder.SeedAsync(app.Services);
    await UserSeeder.SeedAsync(app.Services);
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        // No dependency checks; process is alive.
        Predicate = _ => false,
        ResponseWriter =
            HealthCheckResponseWriter.WriteResponseAsync
    })
    .AllowAnonymous();

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration =>
            registration.Tags.Contains("ready"),
        ResponseWriter =
            HealthCheckResponseWriter.WriteResponseAsync
    })
    .AllowAnonymous();

app.Run();

public partial class Program;