namespace DocuTrack.Core.Identity
{
    public interface ICurrentUser
    {
        Guid UserId { get; }
        bool IsAuthenticated { get; }
        string? Email { get; }
    }
}
