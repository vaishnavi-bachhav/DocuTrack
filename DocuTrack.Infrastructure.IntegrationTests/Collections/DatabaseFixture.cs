using DocuTrack.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace DocuTrack.Infrastructure.IntegrationTests.Collections
{
    public sealed class DatabaseFixture : IAsyncLifetime
    {
        private const string TestDatabaseName =
        "DocuTrackIntegrationTests";

        private MsSqlContainer? _container;

        private string? _connectionString;

        public string ConnectionString =>
            _connectionString
            ?? throw new InvalidOperationException(
                "The test database has not been initialized.");

        public async Task InitializeAsync()
        {
            _container = new MsSqlBuilder()
                .WithCleanUp(true)
                .Build();

            await _container.StartAsync();

            SqlConnectionStringBuilder connectionBuilder =
                new(_container.GetConnectionString())
                {
                    InitialCatalog = TestDatabaseName
                };

            _connectionString =
                connectionBuilder.ConnectionString;

            await using DocuTrackDbContext context =
                CreateDbContext();

            await context.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            SqlConnection.ClearAllPools();

            if (_container is not null)
            {
                await _container.DisposeAsync();
            }
        }

        public DocuTrackDbContext CreateDbContext()
        {
            DbContextOptions<DocuTrackDbContext> options =
                new DbContextOptionsBuilder<DocuTrackDbContext>()
                    .UseSqlServer(ConnectionString)
                    .EnableDetailedErrors()
                    .EnableSensitiveDataLogging()
                    .Options;

            return new DocuTrackDbContext(options);
        }

        public async Task ResetDatabaseAsync()
        {
            SqlConnection.ClearAllPools();

            await using DocuTrackDbContext context =
                CreateDbContext();

            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();
        }
    }
}
