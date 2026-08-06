using DocuTrack.Api.IntegrationTests.Authentication;
using DocuTrack.Api.IntegrationTests.Database;
using DocuTrack.Api.IntegrationTests.Transactions;
using DocuTrack.Application.Abstractions.Authentication;
using DocuTrack.Application.Abstractions.Persistence;
using DocuTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DocuTrack.Api.IntegrationTests;

public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private const string TestJwtKey =
        "DocuTrack-Integration-Test-JWT-Key-12345678901234567890";

    private readonly InMemoryDatabaseRoot _databaseRoot = new();

    private readonly string _databaseName =
        $"DocuTrackApiTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Program.cs validates JWT settings during startup.
        // The fake authentication handler is used for protected
        // document endpoints, but valid JWT settings are still
        // required to let the application start.
        builder.UseSetting(
            "Jwt:Issuer",
            "DocuTrack.Api.IntegrationTests");

        builder.UseSetting(
            "Jwt:Audience",
            "DocuTrack.Api.IntegrationTests");

        builder.UseSetting(
            "Jwt:Key",
            TestJwtKey);

        builder.UseSetting(
            "Jwt:ExpirationMinutes",
            "60");

        builder.ConfigureTestServices(services =>
        {
            ReplaceDatabase(services);
            ReplaceAuthentication(services);
            ReplaceDocumentNumberGenerator(services);
            ReplaceIdentityTransactionFactory(services);
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
                    TestAuthHandler.SchemeName;

                options.DefaultAuthenticateScheme =
                    TestAuthHandler.SchemeName;

                options.DefaultChallengeScheme =
                    TestAuthHandler.SchemeName;

                options.DefaultForbidScheme =
                    TestAuthHandler.SchemeName;
            })
            .AddScheme<
                AuthenticationSchemeOptions,
                TestAuthHandler>(
                TestAuthHandler.SchemeName,
                _ =>
                {
                });
    }

    private static void ReplaceDocumentNumberGenerator(
        IServiceCollection services)
    {
        services.RemoveAll<IDocumentNumberGenerator>();

        services.AddSingleton<
            IDocumentNumberGenerator,
            TestDocumentNumberGenerator>();
    }

    private static void ReplaceIdentityTransactionFactory(
        IServiceCollection services)
    {
        services.RemoveAll<IIdentityTransactionFactory>();

        services.AddSingleton<
            IIdentityTransactionFactory,
            TestIdentityTransactionFactory>();
    }
}