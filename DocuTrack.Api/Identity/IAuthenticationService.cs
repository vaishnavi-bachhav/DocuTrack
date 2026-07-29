using DocuTrack.Api.Contracts.Requests;
using DocuTrack.Api.Contracts.Responses;

namespace DocuTrack.Api.Identity
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResponse> LoginAsync(LoginApiRequest request, CancellationToken cancellationToken = default);
        Task<AuthenticationResponse> RegisterAsync(RegisterApiRequest request, CancellationToken cancellationToken = default);
    }
}