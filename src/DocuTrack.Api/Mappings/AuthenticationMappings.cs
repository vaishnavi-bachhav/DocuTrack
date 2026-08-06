using DocuTrack.Api.Contracts.Requests;
using DocuTrack.Api.Contracts.Responses;
using DocuTrack.Application.Authentication.Commands;
using DocuTrack.Application.Authentication.Results;

namespace DocuTrack.Api.Mappings
{
    public static class AuthenticationMappings
    {
        public static LoginCommand ToCommand(
        this LoginApiRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new LoginCommand
            {
                Email = request.Email.Trim(),
                Password = request.Password
            };
        }

        public static RegisterUserCommand ToCommand(
       this RegisterApiRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new RegisterUserCommand
            {
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim(),
                Password = request.Password
            };
        }

        public static RefreshTokenCommand ToCommand(
            this RefreshTokenApiRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            return new RefreshTokenCommand
            {
                RefreshToken = request.RefreshToken
            };
        }

        public static RevokeRefreshTokenCommand ToCommand(
            this RevokeRefreshTokenApiRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            return new RevokeRefreshTokenCommand
            {
                RefreshToken = request.RefreshToken
            };
        }

        public static AuthenticationResponse ToResponse(
        this AuthenticationResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            return new AuthenticationResponse
            {
                AccessToken = result.AccessToken,
                AccessTokenExpiresAt = result.AccessTokenExpiresAt,
                RefreshToken = result.RefreshToken,
                RefreshTokenExpiresAt = result.RefreshTokenExpiresAt,
                UserId = result.UserId,
                Email = result.Email,
                FullName = result.FullName,
                Roles = result.Roles
            };
        }
    }
}
