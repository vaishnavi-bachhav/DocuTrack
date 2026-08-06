using DocuTrack.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace DocuTrack.Api.DependencyInjection;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        JwtSettings settings =
            configuration
                .GetRequiredSection(
                    JwtSettings.SectionName)
                .Get<JwtSettings>()
            ?? throw new InvalidOperationException(
                "JWT settings are missing.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = false;

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = settings.Issuer,

                        ValidateAudience = true,
                        ValidAudience = settings.Audience,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(
                                    settings.Key)),

                        ValidateLifetime = true,

                        ClockSkew =
                            TimeSpan.FromMinutes(1),

                        NameClaimType =
                            ClaimTypes.Name,

                        RoleClaimType =
                            ClaimTypes.Role
                    };
            });

        return services;
    }
}