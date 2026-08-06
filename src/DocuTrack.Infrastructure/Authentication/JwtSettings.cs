using System.ComponentModel.DataAnnotations;

namespace DocuTrack.Infrastructure.Authentication
{
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        [Required]
        public string Issuer { get; init; } = string.Empty;

        [Required]
        public string Audience { get; init; } = string.Empty;

        [Required]
        [MinLength(32)]
        public string Key { get; init; } = string.Empty;

        [Range(1, 60)]
        public int AccessTokenExpirationMinutes { get; init; } = 15;

        [Range(1, 90)]
        public int RefreshTokenExpirationDays { get; init; } = 7;
    }
}
