namespace DocuTrack.Application.Abstractions.Authentication
{
    public sealed record IssuedRefreshToken(
    Guid TokenId,
    Guid FamilyId,
    string RawToken,
    DateTimeOffset ExpiresAt);

    public sealed record RefreshTokenRotationResult(
        Guid UserId,
        IssuedRefreshToken NewToken);
}
