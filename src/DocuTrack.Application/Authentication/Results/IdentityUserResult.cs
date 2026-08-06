namespace DocuTrack.Application.Authentication.Results
{
    public sealed class IdentityUserResult
    {
        public required Guid UserId { get; init; }
        public required string Email { get; init; }
        public required string FullName { get; init; }
    }
}
