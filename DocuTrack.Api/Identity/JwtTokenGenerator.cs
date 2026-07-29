using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DocuTrack.Api.Contracts.Responses;
using DocuTrack.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DocuTrack.Api.Identity
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtSettings _settings;

        public JwtTokenGenerator(UserManager<ApplicationUser> userManager, IOptions<JwtSettings> settings)
        {
            _userManager = userManager;
            _settings = settings.Value;
        }

        public async Task<AuthenticationResponse> GenerateAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(user);

            IList<string> roles = await _userManager.GetRolesAsync(user);

            DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddMinutes(_settings.ExpirationMinutes);

            List<Claim> claims = [
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Name, user.FullName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ];

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_settings.Key));

            SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: expiresAt.UtcDateTime,
                signingCredentials: credentials
            );

            string tokenValue = new JwtSecurityTokenHandler().WriteToken(token);

            return new AuthenticationResponse
            {
                AccessToken = tokenValue,
                ExpiresAt = expiresAt,
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Roles = roles.ToList()
            };
        }
    }
}
