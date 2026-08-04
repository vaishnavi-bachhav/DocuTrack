using DocuTrack.Api.Contracts.Requests;
using DocuTrack.Api.Contracts.Responses;
using DocuTrack.Core.Enums;
using DocuTrack.Core.Exceptions;
using DocuTrack.Core.Identity;
using DocuTrack.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace DocuTrack.Api.Identity
{
    public sealed class AuthenticationService : IAuthenticationService
    {
        private string DefaultRole = UserRole.Employee.ToString();

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IIdentityTransactionFactory _transactionFactory;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager,
            IJwtTokenGenerator jwtTokenGenerator,
            IIdentityTransactionFactory transactionFactory,
            ILogger<AuthenticationService> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _jwtTokenGenerator = jwtTokenGenerator ?? throw new ArgumentNullException(nameof(jwtTokenGenerator));
            _transactionFactory = transactionFactory ?? throw new ArgumentNullException(nameof(transactionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AuthenticationResponse> RegisterAsync(RegisterApiRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            string email = request.Email.Trim();
            string fullName = request.FullName.Trim();

            ValidateRegistrationRequest(fullName, email, request.Password);

            ApplicationUser? existingUser = await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                throw new UserAlreadyExistsException(email);
            }

            await using IIdentityTransaction transaction = await _transactionFactory.BeginAsync(cancellationToken);

            try
            {
                ApplicationUser user = new()
                {
                    Id = Guid.NewGuid(),
                    FullName = fullName,
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                IdentityResult createResult = await _userManager.CreateAsync(user, request.Password);
                if (!createResult.Succeeded)
                {
                    // Use the existing CreateValidationException helper
                    throw CreateValidationException(createResult.Errors);
                }

                cancellationToken.ThrowIfCancellationRequested();

                IdentityResult roleResult = await _userManager.AddToRoleAsync(user, DefaultRole);

                if (!roleResult.Succeeded)
                {
                    throw CreateRegistrationException(
                                       $"The user could not be assigned to the '{DefaultRole}' role.",
                                       roleResult.Errors);
                }

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "User {UserId} registered successfully with role {Role}",
                    user.Id,
                    DefaultRole);

                return await _jwtTokenGenerator.GenerateAsync(user, cancellationToken);

            }
            catch (Exception)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
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

            // Return the same error for an unknown account and a bad
            // password to avoid exposing whether the email is registered.
            if (user == null)
            {
                throw new AuthenticationFailedException();
            }

            SignInResult signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (signInResult.IsLockedOut)
            {
                _logger.LogWarning("Login blocked because user {UserId} is locked out", user.Id);

                throw new AccountLockedException();
            }

            if (signInResult.IsNotAllowed)
            {
                _logger.LogInformation("Login not allowed for user {UserId}", user.Id);

                throw new AuthenticationFailedException();
            }

            if (!signInResult.Succeeded)
            {
                _logger.LogInformation("Invalid login attempt for user {UserId}", user.Id);
                throw new AuthenticationFailedException();
            }

            _logger.LogInformation("User {UserId} authenticated successfully", user.Id);

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

        private static UserRegistrationException CreateRegistrationException(string generalMessage, IEnumerable<IdentityError> errors)
        {
            string details = string.Join(
                "; ",
                errors.Select(error => error.Description));

            string message =
                string.IsNullOrWhiteSpace(details)
                    ? generalMessage
                    : $"{generalMessage} {details}";

            return new UserRegistrationException(message);
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
    }
}
