using DocuTrack.Api.Contracts.Requests;
using DocuTrack.Api.Contracts.Responses;
using DocuTrack.Core.Enums;
using DocuTrack.Core.Exceptions;
using DocuTrack.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace DocuTrack.Api.Identity
{
    public sealed class AuthenticationService : IAuthenticationService
    {
        private string DefaultRole = UserRole.Employee.ToString();

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public AuthenticationService(UserManager<ApplicationUser> userManager, IJwtTokenGenerator jwtTokenGenerator)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _jwtTokenGenerator = jwtTokenGenerator ?? throw new ArgumentNullException(nameof(jwtTokenGenerator));
        }

        public async Task<AuthenticationResponse> RegisterAsync(RegisterApiRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            string email = request.Email.Trim();
            string fullName = request.FullName.Trim();

            ValidateRegistrationRequest(fullName, email, request.Password);

            ApplicationUser? existingUser = await _userManager.FindByEmailAsync(email);

            if (existingUser is not null)
            {
                throw new UserAlreadyExistsException(email);
            }

            ApplicationUser user = new()
            {
                Id = Guid.NewGuid(),
                FullName = fullName,
                UserName = email,
                Email = email,
                EmailConfirmed = false,
                CreatedAt = DateTimeOffset.UtcNow
            };

            IdentityResult createResult = await _userManager.CreateAsync(user, request.Password);

            if (!createResult.Succeeded)
            {
                throw CreateValidationException(createResult.Errors);
            }

            cancellationToken.ThrowIfCancellationRequested();

            IdentityResult roleResult = await _userManager.AddToRoleAsync(user, DefaultRole);

            if (!roleResult.Succeeded)
            {
                await TryDeleteUserAsync(user);

                throw new InvalidOperationException($"The user could not be assigned to the '{DefaultRole}' role.");
            }

            return await _jwtTokenGenerator.GenerateAsync(user, cancellationToken);
        }

        public async Task<AuthenticationResponse> LoginAsync(LoginApiRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            string email = request.Email.Trim();

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                throw new AuthenticationFailedException();
            }

            ApplicationUser? user = await _userManager.FindByEmailAsync(email);

            // Use the same response for an unknown email and an invalid password.
            // This avoids revealing whether an account exists.
            if (user is null)
            {
                throw new AuthenticationFailedException();
            }

            bool isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);

            if (!isPasswordValid)
            {
                throw new AuthenticationFailedException();
            }

            cancellationToken.ThrowIfCancellationRequested();

            return await _jwtTokenGenerator.GenerateAsync(user, cancellationToken);
        }

        private static void ValidateRegistrationRequest(string fullName, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new DomainValidationException("Full name is required.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new DomainValidationException("Email is required.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new DomainValidationException("Password is required.");
            }
        }

        private static DomainValidationException CreateValidationException(IEnumerable<IdentityError> errors)
        {
            string message = string.Join(
                "; ",
                errors.Select(error => error.Description));

            if (string.IsNullOrWhiteSpace(message))
            {
                message = "The user could not be created.";
            }

            return new DomainValidationException(message);
        }

        private async Task TryDeleteUserAsync(ApplicationUser user)
        {
            try
            {
                await _userManager.DeleteAsync(user);
            }
            catch
            {
                // The original role-assignment failure should remain
                // the primary error. Log this cleanup failure later
                // using ILogger if needed.
            }
        }
    }
}
