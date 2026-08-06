using Microsoft.AspNetCore.Identity;

namespace DocuTrack.Infrastructure.Identity
{
    public sealed class ApplicationUser : IdentityUser<Guid>
    {
        public required string FullName { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
