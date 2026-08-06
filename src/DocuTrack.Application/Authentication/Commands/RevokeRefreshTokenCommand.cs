namespace DocuTrack.Application.Authentication.Commands
{
    public sealed class RevokeRefreshTokenCommand
    {
        public required string RefreshToken { get; init; }
    }
}
