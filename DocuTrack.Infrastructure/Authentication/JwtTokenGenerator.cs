using DocuTrack.Application.Abstractions.Authentication;
using DocuTrack.Application.Authentication.Results;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DocuTrack.Infrastructure.Authentication
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtSettings _settings;

        public JwtTokenGenerator(
            IOptions<JwtSettings> options)
        {
            ArgumentNullException.ThrowIfNull(options);

            _settings = options.Value;
        }

        public AuthenticationResult Generate(
        IdentityUserResult user,
        IReadOnlyCollection<string> roles)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(roles);

            DateTimeOffset issuedAt =
                DateTimeOffset.UtcNow;

            DateTimeOffset expiresAt =
                issuedAt.AddMinutes(
                    _settings.ExpirationMinutes);

            List<Claim> claims =
            [
                new(
                JwtRegisteredClaimNames.Sub,
                user.UserId.ToString()),

            new(
                ClaimTypes.NameIdentifier,
                user.UserId.ToString()),

            new(
                JwtRegisteredClaimNames.Email,
                user.Email),

            new(
                ClaimTypes.Name,
                user.FullName),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()),

            new(
                JwtRegisteredClaimNames.Iat,
                issuedAt.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
            ];

            claims.AddRange(
                roles.Select(
                    role => new Claim(
                        ClaimTypes.Role,
                        role)));

            SymmetricSecurityKey signingKey = new(
                Encoding.UTF8.GetBytes(
                    _settings.Key));

            SigningCredentials signingCredentials = new(
                signingKey,
                SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: issuedAt.UtcDateTime,
                expires: expiresAt.UtcDateTime,
                signingCredentials: signingCredentials);

            string accessToken =
                new JwtSecurityTokenHandler()
                    .WriteToken(token);

            return new AuthenticationResult
            {
                AccessToken = accessToken,
                ExpiresAt = expiresAt,
                UserId = user.UserId,
                Email = user.Email,
                FullName = user.FullName,
                Roles = roles.ToArray()
            };
        }
    }
}
