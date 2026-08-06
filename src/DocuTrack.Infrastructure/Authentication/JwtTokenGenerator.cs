using DocuTrack.Application.Abstractions.Authentication;
using DocuTrack.Application.Abstractions.Time;
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
        private readonly IClock _clock;
        public JwtTokenGenerator(
            IOptions<JwtSettings> options,
            IClock clock)
        {
            ArgumentNullException.ThrowIfNull(options);

            _settings = options.Value;
            _clock = clock
            ?? throw new ArgumentNullException(
                nameof(clock));
        }

        public AccessTokenResult GenerateAccessToken(
        IdentityUserResult user,
        IReadOnlyCollection<string> roles)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(roles);

            DateTimeOffset issuedAt =
                DateTimeOffset.UtcNow;

            DateTimeOffset expiresAt =
                issuedAt.AddMinutes(
                    _settings.AccessTokenExpirationMinutes);
            
            ValidateSettings();

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
                ClaimTypes.Email,
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
          roles
              .Distinct(StringComparer.OrdinalIgnoreCase)
              .Select(role =>
                  new Claim(
                      ClaimTypes.Role,
                      role)));

            byte[] keyBytes =
            Encoding.UTF8.GetBytes(
                _settings.Key);

            SymmetricSecurityKey signingKey =
                new(keyBytes);

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

            string tokenValue =
          new JwtSecurityTokenHandler()
              .WriteToken(token);

            return new AccessTokenResult(
                Token: tokenValue,
                ExpiresAt: expiresAt);
        }

        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_settings.Issuer))
            {
                throw new InvalidOperationException(
                    "JWT issuer is missing.");
            }

            if (string.IsNullOrWhiteSpace(_settings.Audience))
            {
                throw new InvalidOperationException(
                    "JWT audience is missing.");
            }

            if (string.IsNullOrWhiteSpace(_settings.Key))
            {
                throw new InvalidOperationException(
                    "JWT signing key is missing.");
            }

            if (Encoding.UTF8.GetByteCount(_settings.Key) < 32)
            {
                throw new InvalidOperationException(
                    "JWT signing key must be at least 32 bytes.");
            }

            if (_settings.AccessTokenExpirationMinutes < 1)
            {
                throw new InvalidOperationException(
                    "JWT access-token expiration must be greater than zero.");
            }
        }
    }
}
