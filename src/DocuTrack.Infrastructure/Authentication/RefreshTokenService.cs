using DocuTrack.Application.Abstractions.Authentication;
using DocuTrack.Application.Abstractions.Time;
using DocuTrack.Application.Authentication.Commands;
using DocuTrack.Application.Authentication.Results;
using DocuTrack.Application.Common.Exceptions;
using DocuTrack.Infrastructure.Identity;
using DocuTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace DocuTrack.Infrastructure.Authentication
{
    public sealed class RefreshTokenService : IRefreshTokenService
    {
        private readonly DocuTrackDbContext _dbContext;
        private readonly IClock _clock;
        private readonly JwtSettings _settings;

        public RefreshTokenService(
            DocuTrackDbContext dbContext,
            IClock clock,
            IOptions<JwtSettings> settings)
        {
            _dbContext = dbContext;
            _clock = clock;
            _settings = settings.Value;
        }

        public async Task<IssuedRefreshToken> IssueAsync(Guid userId, Guid? familyId = null, CancellationToken cancellationToken = default)
        {
            string rawToken = CreateSecureToken();
            DateTimeOffset now = _clock.UtcNow;

            RefreshToken token = new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = HashToken(rawToken),
                FamilyId = familyId ?? Guid.NewGuid(),
                CreatedAt = now,
                ExpiresAt = now.AddDays(_settings.RefreshTokenExpirationDays)
            };

            _dbContext.RefreshTokens.Add(token);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new IssuedRefreshToken(token.Id, token.FamilyId, rawToken, token.ExpiresAt);
        }

        public async Task RevokeAsync(string rawRefreshToken, string reason, CancellationToken cancellationToken = default)
        {
            string tokenHash = HashToken(rawRefreshToken);

            RefreshToken? token = await _dbContext.RefreshTokens.FirstOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

            if (token is null || token.IsRevoked)
            {
                return;
            }

            token.RevokedAt = _clock.UtcNow;
            token.RevokedReason = reason;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<RefreshTokenRotationResult> RotateAsync(string rawRefreshToken, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rawRefreshToken, nameof(rawRefreshToken));
            string tokenHash = HashToken(rawRefreshToken);
            DateTimeOffset now = _clock.UtcNow;

            RefreshToken? existingToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

            if (existingToken is null)
            {
                throw new AuthenticationFailedException();
            }

            if (existingToken.IsRevoked)
            {
                await RevokeFamilyAsync(existingToken.FamilyId, "Refresh token reuse detected.", cancellationToken);
                throw new AuthenticationFailedException();
            }

            if (existingToken.IsExpired(now))
            {
                throw new AuthenticationFailedException();
            }

            string newRawToken = CreateSecureToken();

            RefreshToken newToken = new()
            {
                Id = Guid.NewGuid(),
                UserId = existingToken.UserId,
                TokenHash = HashToken(newRawToken),
                FamilyId = existingToken.FamilyId,
                CreatedAt = now,
                ExpiresAt = now.AddDays(_settings.RefreshTokenExpirationDays)
            };

            existingToken.ReplacedByTokenId = newToken.Id;

            _dbContext.RefreshTokens.Add(newToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            IssuedRefreshToken issuedToken = new(
                newToken.Id,
                newToken.FamilyId,
                newRawToken,
                newToken.ExpiresAt);

            return new RefreshTokenRotationResult(existingToken.UserId, issuedToken);
        }

        private async Task RevokeFamilyAsync(Guid familyId, string reason, CancellationToken cancellationToken)
        {
            DateTimeOffset now = _clock.UtcNow;

            List<RefreshToken> tokens = await _dbContext.RefreshTokens
                .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (RefreshToken token in tokens)
            {
                token.RevokedAt = _clock.UtcNow;
                token.RevokedReason = reason;
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private static string CreateSecureToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(64);

            return Convert.ToBase64String(bytes);
        }

        private static string HashToken(string rawToken)
        {
            byte[] bytes =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(rawToken));

            return Convert.ToHexString(bytes);
        }
    }
}
