namespace DocuTrack.Application.Authentication.Results
{
    public sealed record AccessTokenResult(
    string Token,
    DateTimeOffset ExpiresAt);
}
