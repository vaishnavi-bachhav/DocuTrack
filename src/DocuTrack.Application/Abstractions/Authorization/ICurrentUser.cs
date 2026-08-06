namespace DocuTrack.Application.Abstractions.Authorization
{
    public interface ICurrentUser
    {
        Guid UserId { get; }
        bool IsAuthenticated { get; }
        string? Email { get; }
    }
}
