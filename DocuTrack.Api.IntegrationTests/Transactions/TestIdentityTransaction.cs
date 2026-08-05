using DocuTrack.Application.Abstractions.Authentication;

namespace DocuTrack.Api.IntegrationTests.Transactions;

public sealed class TestIdentityTransaction
    : IIdentityTransaction
{
    public bool WasCommitted { get; private set; }

    public bool WasRolledBack { get; private set; }

    public Task CommitAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        WasCommitted = true;

        return Task.CompletedTask;
    }

    public Task RollbackAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        WasRolledBack = true;

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}