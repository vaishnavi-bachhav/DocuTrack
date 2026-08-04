using DocuTrack.Core.Identity;

namespace DocuTrack.Api.IntegrationTests.Authentication
{
    public sealed class TestIdentityTransactionFactory : IIdentityTransactionFactory
    {
        public Task<IIdentityTransaction> BeginAsync(CancellationToken cancellationToken = default)
        {
            IIdentityTransaction transaction = new TestIdentityTransaction();

            return Task.FromResult(transaction);
        }
    }
}