namespace DocuTrack.Api.Contracts.Requests
{
    public sealed class RevokeRefreshTokenApiRequest
    {
        public required string RefreshToken { get; init; }
    }
}
