namespace DocuTrack.Core.Identity
{
    public interface IIdentityTransaction : IAsyncDisposable
    {
        Task CommitAsync(CancellationToken cancellationToken = default);

        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
