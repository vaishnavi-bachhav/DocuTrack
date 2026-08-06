using System.Text;
using DocuTrack.Application.Abstractions.Authentication;
using DocuTrack.Application.Abstractions.Persistence;
using DocuTrack.Application.Abstractions.Time;
using DocuTrack.Infrastructure.Authentication;
using DocuTrack.Infrastructure.Documents;
using DocuTrack.Infrastructure.Identity;
using DocuTrack.Infrastructure.Persistence;
using DocuTrack.Infrastructure.Repositories;
using DocuTrack.Infrastructure.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DocuTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        AddPersistence(services, configuration, environment);

        AddIdentity(services);

        AddJwtOptions(services, configuration);

        services.AddScoped<IDocumentRepository, EfDocumentRepository>();

        services.AddScoped<IDocumentNumberGenerator, SqlDocumentNumberGenerator>();

        services.AddScoped<IIdentityService, IdentityService>();

        services.AddScoped<IIdentityTransactionFactory, EfIdentityTransactionFactory>();

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        services.AddSingleton<IClock, SystemClock>();

        return services;
    }

    private static void AddPersistence(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Integration tests replace this registration.
        if (environment.IsEnvironment("Testing"))
        {
            return;
        }

        string connectionString =
            configuration.GetConnectionString(
                "DocuTrackDb")
            ?? throw new InvalidOperationException(
                "Connection string 'DocuTrackDb' was not found.");

        services.AddDbContext<DocuTrackDbContext>(
            options =>
            {
                options.UseSqlServer(
                    connectionString,
                    sqlServerOptions =>
                    {
                        sqlServerOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay:
                                TimeSpan.FromSeconds(5),
                            errorNumbersToAdd: null);
                    });
            });
    }

    private static void AddIdentity(
        IServiceCollection services)
    {
        services.AddDataProtection();

        services
            .AddIdentityCore<ApplicationUser>(
                options =>
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
                    options.Lockout.DefaultLockoutTimeSpan =
                        TimeSpan.FromMinutes(15);
                })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<
                DocuTrackDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();
    }

    private static void AddJwtOptions(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<JwtSettings>()
            .Bind(
                configuration.GetSection(
                    JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                settings =>
                    Encoding.UTF8.GetByteCount(
                        settings.Key) >= 32,
                "JWT signing key must be at least 32 bytes.")
            .ValidateOnStart();
    }
}