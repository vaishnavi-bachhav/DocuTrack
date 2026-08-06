using DocuTrack.Application.Abstractions.Authentication;

namespace DocuTrack.Api.IntegrationTests.Transactions;

public sealed class TestIdentityTransactionFactory
    : IIdentityTransactionFactory
{
    public Task<IIdentityTransaction> BeginAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IIdentityTransaction transaction =
            new TestIdentityTransaction();

        return Task.FromResult(transaction);
    }
}