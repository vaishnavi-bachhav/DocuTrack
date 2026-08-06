using DocuTrack.Application.Authentication.Results;

namespace DocuTrack.Application.Abstractions.Authentication
{
    public interface IIdentityService
    {
        Task<IdentityUserResult?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<IdentityUserResult> CreateUserAsync(string fullName, string email, string password, CancellationToken cancellationToken = default);
        Task AddToRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);
        Task<PasswordCheckResult> CheckPasswordAsync(Guid userId, string password, bool lockoutOnFailure, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
