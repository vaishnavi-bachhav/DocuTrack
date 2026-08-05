using DocuTrack.Application.Abstractions.Authentication;
using DocuTrack.Infrastructure.Persistence;

namespace DocuTrack.Infrastructure.Identity
{
    public sealed class EfIdentityTransactionFactory : IIdentityTransactionFactory
    {
        private readonly DocuTrackDbContext _dbContext;

        public EfIdentityTransactionFactory(DocuTrackDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<IIdentityTransaction> BeginAsync(CancellationToken cancellationToken = default)
        {
            var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            return new EfIdentityTransaction(transaction);
        }
    }
}
