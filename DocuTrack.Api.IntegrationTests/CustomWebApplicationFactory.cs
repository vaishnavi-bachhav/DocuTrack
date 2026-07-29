using DocuTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DocuTrack.Api.IntegrationTests
{
    public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly InMemoryDatabaseRoot _databaseRoot = new();

        private readonly string _databaseName = $"DocuTrackTests-{Guid.NewGuid()}";
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DocuTrackDbContext>();
                services.RemoveAll<DbContextOptions>();
                services.RemoveAll<DbContextOptions<DocuTrackDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<DocuTrackDbContext>>();

                services.AddDbContext<DocuTrackDbContext>(
                    options =>
                    {
                        options.UseInMemoryDatabase(_databaseName, _databaseRoot);
                    }
                );
            });
        }
    }
}
