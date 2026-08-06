using DocuTrack.Application.Authentication.Results;

namespace DocuTrack.Application.Abstractions.Authentication
{
    public interface IJwtTokenGenerator
    {
        AuthenticationResult Generate(
        IdentityUserResult user,
        IReadOnlyCollection<string> roles);
    }
}
