using System.Data;
using DocuTrack.Application.Abstractions.Persistence;
using DocuTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocuTrack.Infrastructure.Documents
{
    public sealed class SqlDocumentNumberGenerator : IDocumentNumberGenerator
    {
        private readonly DocuTrackDbContext _dbContext;

        public SqlDocumentNumberGenerator(
            DocuTrackDbContext dbContext)
        {
            _dbContext = dbContext
                ?? throw new ArgumentNullException(
                    nameof(dbContext));
        }

        public async Task<string> GenerateAsync(
        CancellationToken cancellationToken = default)
        {
            var connection =
                _dbContext.Database.GetDbConnection();

            bool shouldCloseConnection =
                connection.State != ConnectionState.Open;

            if (shouldCloseConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                await using var command =
                    connection.CreateCommand();

                command.CommandText =
                    "SELECT NEXT VALUE FOR dbo.DocumentNumberSequence";

                object? result =
                    await command.ExecuteScalarAsync(
                        cancellationToken);

                if (result is null || result == DBNull.Value)
                {
                    throw new InvalidOperationException(
                        "Failed to generate a document number.");
                }

                long nextNumber =
                    Convert.ToInt64(result);

                return $"DOC-{nextNumber:D6}";
            }
            finally
            {
                if (shouldCloseConnection)
                {
                    await connection.CloseAsync();
                }
            }
        }
    }
}
