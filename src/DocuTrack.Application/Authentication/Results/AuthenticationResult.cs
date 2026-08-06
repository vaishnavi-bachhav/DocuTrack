namespace DocuTrack.Application.Authentication.Results
{
    public sealed class AuthenticationResult
    {
        public required string AccessToken { get; init; }
        public required DateTimeOffset AccessTokenExpiresAt { get; init; }
        public required string RefreshToken { get; init; }
        public required DateTimeOffset RefreshTokenExpiresAt { get; init; }
        public required Guid UserId { get; init; }
        public required string Email { get; init; }
        public required string FullName { get; init; }
        public required IReadOnlyCollection<string> Roles { get; init; }
    }
}
