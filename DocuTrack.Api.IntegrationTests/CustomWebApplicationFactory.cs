using DocuTrack.Api.IntegrationTests.Authentication;
using DocuTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DocuTrack.Api.IntegrationTests;

public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private const string TestJwtKey =
       "DocuTrack-Integration-Test-Key-12345678901234567890";

    private readonly InMemoryDatabaseRoot _databaseRoot = new();

    private readonly string _databaseName =
        $"DocuTrackTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseEnvironment("Testing");

        // These keys must match JwtSettings properties exactly.
        builder.UseSetting(
            "Jwt:Issuer",
            "DocuTrack.Tests");

        builder.UseSetting(
            "Jwt:Audience",
            "DocuTrack.Tests");

        builder.UseSetting(
            "Jwt:Key",
            TestJwtKey);

        builder.ConfigureTestServices(services =>
        {
            ReplaceDatabase(services);
            ReplaceAuthentication(services);
        });
    }

    private void ReplaceDatabase(
        IServiceCollection services)
    {
        services.RemoveAll<DocuTrackDbContext>();
        services.RemoveAll<DbContextOptions>();
        services.RemoveAll<
            DbContextOptions<DocuTrackDbContext>>();

        services.RemoveAll<
            IDbContextOptionsConfiguration<
                DocuTrackDbContext>>();

        services.AddDbContext<DocuTrackDbContext>(
            options =>
            {
                options.UseInMemoryDatabase(
                    _databaseName,
                    _databaseRoot);
            });
    }

    private static void ReplaceAuthentication(
        IServiceCollection services)
    {
        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme =
                    TestAuthHandler.AuthenticationScheme;

                options.DefaultAuthenticateScheme =
                    TestAuthHandler.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    TestAuthHandler.AuthenticationScheme;

                options.DefaultForbidScheme =
                    TestAuthHandler.AuthenticationScheme;
            })
            .AddScheme<
                AuthenticationSchemeOptions,
                TestAuthHandler>(
                TestAuthHandler.AuthenticationScheme,
                _ =>
                {
                });
    }
}