namespace DocuTrack.Api.Identity
{
    public sealed class SeedAdminSettings
    {
        public string? Email { get; init; }
        public string? Password { get; init; }
        public string FullName { get; init; } =
            "System Administrator";
    }
}
