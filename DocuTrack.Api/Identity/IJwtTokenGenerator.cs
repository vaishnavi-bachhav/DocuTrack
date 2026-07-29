using DocuTrack.Api.Contracts.Responses;
using DocuTrack.Infrastructure.Identity;

namespace DocuTrack.Api.Identity
{
    public interface IJwtTokenGenerator
    {
        Task<AuthenticationResponse> GenerateAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    }
}
