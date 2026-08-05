namespace DocuTrack.Application.Authentication.Commands
{
    public sealed class LoginCommand
    {
        public required string Email { get; init; }
        public required string Password { get; init; }
    }
}
