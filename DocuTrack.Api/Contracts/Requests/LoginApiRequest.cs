using System.ComponentModel.DataAnnotations;

namespace DocuTrack.Api.Contracts.Requests
{
    public class LoginApiRequest
    {
        [Required]
        [EmailAddress]
        public required string Email { get; init; }

        [Required]
        public required string Password { get; init; }
    }
}
