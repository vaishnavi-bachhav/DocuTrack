namespace DocuTrack.Core.Identity
{
    public interface IIdentityTransactionFactory
    {
        Task<IIdentityTransaction> BeginAsync(CancellationToken cancellationToken = default);
    }
}
