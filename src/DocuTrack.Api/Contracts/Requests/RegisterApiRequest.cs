using System.ComponentModel.DataAnnotations;

namespace DocuTrack.Api.Contracts.Requests
{
    public class RegisterApiRequest
    {
        [Required]
        [StringLength(100)]
        public required string FullName { get; init; }

        [Required]
        [EmailAddress]
        public required string Email { get; init; }

        [Required]
        [MinLength(8)]
        public required string Password { get; init; }
    }
}
