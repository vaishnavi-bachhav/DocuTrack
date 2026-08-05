namespace DocuTrack.Application.Abstractions.Authentication
{
    public interface IIdentityTransaction : IAsyncDisposable
    {
        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
