using DocuTrack.Application.Abstractions.Authentication;
using DocuTrack.Application.Authentication.Results;
using DocuTrack.Application.Common.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace DocuTrack.Infrastructure.Identity
{
    public sealed class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IdentityUserResult?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ApplicationUser? user =
                await _userManager.FindByEmailAsync(email);

            return user is null
                ? null
                : MapUser(user);
        }

        public async Task<IdentityUserResult> CreateUserAsync(
        string fullName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ApplicationUser user = new()
            {
                Id = Guid.NewGuid(),
                FullName = fullName,
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            IdentityResult result =
                await _userManager.CreateAsync(
                    user,
                    password);

            if (!result.Succeeded)
            {
                throw CreateRegistrationException(
                    result.Errors);
            }

            return MapUser(user);
        }

        public async Task AddToRoleAsync(
            Guid userId,
            string role,
            CancellationToken cancellationToken = default)
        {
            ApplicationUser user =
                await GetUserAsync(
                    userId,
                    cancellationToken);

            IdentityResult result =
                await _userManager.AddToRoleAsync(
                    user,
                    role);

            if (!result.Succeeded)
            {
                throw CreateRegistrationException(
                    result.Errors);
            }
        }

        public async Task<PasswordCheckResult> CheckPasswordAsync(
        Guid userId,
        string password,
        bool lockoutOnFailure,
        CancellationToken cancellationToken = default)
        {
            ApplicationUser user =
                await GetUserAsync(
                    userId,
                    cancellationToken);

            SignInResult result =
                await _signInManager.CheckPasswordSignInAsync(
                    user,
                    password,
                    lockoutOnFailure);

            return new PasswordCheckResult
            {
                Succeeded = result.Succeeded,
                IsLockedOut = result.IsLockedOut,
                IsNotAllowed = result.IsNotAllowed
            };
        }

        public async Task<IReadOnlyCollection<string>> GetRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        {
            ApplicationUser user =
                await GetUserAsync(
                    userId,
                    cancellationToken);

            IList<string> roles =
                await _userManager.GetRolesAsync(user);

            return roles.ToArray();
        }

        private async Task<ApplicationUser> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ApplicationUser? user =
                await _userManager.FindByIdAsync(
                    userId.ToString());

            return user
                ?? throw new AuthenticationFailedException();
        }

        private static IdentityUserResult MapUser(
        ApplicationUser user)
        {
            return new IdentityUserResult
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName
            };
        }

        private static UserRegistrationException
        CreateRegistrationException(
            IEnumerable<IdentityError> errors)
        {
            string details = string.Join(
                "; ",
                errors.Select(error =>
                    error.Description));

            return new UserRegistrationException(
                string.IsNullOrWhiteSpace(details)
                    ? "User registration failed."
                    : details);
        }

    }
}
