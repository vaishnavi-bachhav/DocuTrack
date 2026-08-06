using DocuTrack.Application.Authentication.Results;

namespace DocuTrack.Application.Abstractions.Authentication
{
    public interface IJwtTokenGenerator
    {
        AccessTokenResult GenerateAccessToken(
        IdentityUserResult user,
        IReadOnlyCollection<string> roles);
    }
}
