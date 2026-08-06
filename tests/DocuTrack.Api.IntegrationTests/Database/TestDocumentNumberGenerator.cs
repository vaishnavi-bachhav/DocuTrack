using DocuTrack.Application.Abstractions.Persistence;

namespace DocuTrack.Api.IntegrationTests.Database;

public sealed class TestDocumentNumberGenerator
    : IDocumentNumberGenerator
{
    private long _currentNumber;

    public Task<string> GenerateAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        long nextNumber =
            Interlocked.Increment(
                ref _currentNumber);

        return Task.FromResult(
            $"DOC-{nextNumber:D6}");
    }

    public void Reset()
    {
        Interlocked.Exchange(
            ref _currentNumber,
            0);
    }
}