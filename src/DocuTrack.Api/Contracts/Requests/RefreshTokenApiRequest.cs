namespace DocuTrack.Api.Contracts.Requests
{
    public sealed class RefreshTokenApiRequest
    {
        public required string RefreshToken { get; init; }
    }
}
