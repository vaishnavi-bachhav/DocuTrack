namespace DocuTrack.Application.Abstractions.Persistence
{
    public interface IDocumentNumberGenerator
    {
        Task<string> GenerateAsync(CancellationToken cancellationToken = default);
    }
}
