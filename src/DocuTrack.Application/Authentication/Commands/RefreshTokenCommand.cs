namespace DocuTrack.Application.Authentication.Commands
{
    public sealed class RefreshTokenCommand
    {
        public required string RefreshToken { get; init; }
    }
}
