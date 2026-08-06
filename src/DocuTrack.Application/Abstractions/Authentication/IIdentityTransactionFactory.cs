namespace DocuTrack.Application.Abstractions.Authentication
{
    public interface IIdentityTransactionFactory
    {
        Task<IIdentityTransaction> BeginAsync(CancellationToken cancellationToken = default);
    }
}
