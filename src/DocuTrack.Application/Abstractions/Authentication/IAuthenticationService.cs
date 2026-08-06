using DocuTrack.Application.Authentication.Commands;
using DocuTrack.Application.Authentication.Results;

namespace DocuTrack.Application.Abstractions.Authentication
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default);
        Task<AuthenticationResult> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken = default);
        Task<AuthenticationResult> RefreshAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default);
        Task RevokeAsync(RevokeRefreshTokenCommand command, CancellationToken cancellationToken = default);
    }
}