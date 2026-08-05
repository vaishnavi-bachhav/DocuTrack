namespace DocuTrack.Application.Authentication.Commands
{
    public sealed class RegisterUserCommand
    {
        public required string FullName { get; init; }
        public required string Email { get; init; }
        public required string Password { get; init; }
    }
}
