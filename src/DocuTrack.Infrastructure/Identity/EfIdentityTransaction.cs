using DocuTrack.Application.Abstractions.Authentication;
using Microsoft.EntityFrameworkCore.Storage;

namespace DocuTrack.Infrastructure.Identity
{
    public sealed class EfIdentityTransaction : IIdentityTransaction
    {
        private readonly IDbContextTransaction _transaction;
        private bool _completed;

        public EfIdentityTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_completed)
            {
                throw new InvalidOperationException(
                "The transaction has already completed.");
            }

            await _transaction.CommitAsync(cancellationToken);
            _completed = true;
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_completed)
            {
                return;
            }

            await _transaction.RollbackAsync(cancellationToken);
            _completed = true;
        }

        public async ValueTask DisposeAsync()
        {
            await _transaction.DisposeAsync();
        }
    }
}
