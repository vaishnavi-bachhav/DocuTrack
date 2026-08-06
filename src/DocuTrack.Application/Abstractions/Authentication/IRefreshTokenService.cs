namespace DocuTrack.Application.Abstractions.Authentication
{
    public interface IRefreshTokenService
    {
        Task<IssuedRefreshToken> IssueAsync(
            Guid userId,
            Guid? familyId = null,
            CancellationToken cancellationToken = default);

        Task<RefreshTokenRotationResult> RotateAsync(
            string rawRefreshToken,
            CancellationToken cancellationToken = default);

        Task RevokeAsync(
            string rawRefreshToken,
            string reason,
            CancellationToken cancellationToken = default);
    }
}
