using DocuTrack.Application.Abstractions.Authentication;
using DocuTrack.Application.Authentication.Commands;
using DocuTrack.Application.Authentication.Results;
using DocuTrack.Application.Authorization;
using DocuTrack.Application.Common.Exceptions;

namespace DocuTrack.Application.Authentication
{
    public sealed class AuthenticationService : IAuthenticationService
    {
        private readonly IIdentityService _identityService;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IIdentityTransactionFactory _transactionFactory;

        public AuthenticationService(IIdentityService identityService,
            IJwtTokenGenerator jwtTokenGenerator,
            IIdentityTransactionFactory transactionFactory)
        {
            _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
            _jwtTokenGenerator = jwtTokenGenerator ?? throw new ArgumentNullException(nameof(jwtTokenGenerator));
            _transactionFactory = transactionFactory ?? throw new ArgumentNullException(nameof(transactionFactory));
        }

        public async Task<AuthenticationResult> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);
            cancellationToken.ThrowIfCancellationRequested();

            string email = command.Email.Trim();
            string fullName = command.FullName.Trim();

            ValidateRegistrationCommand(fullName, email, command.Password);

            IdentityUserResult? existingUser = await _identityService.FindByEmailAsync(email);

            if (existingUser is not null)
            {
                throw new UserAlreadyExistsException(email);
            }

            await using IIdentityTransaction transaction = await _transactionFactory.BeginAsync(cancellationToken);

            try
            {
                IdentityUserResult user = await _identityService.CreateUserAsync(fullName, email, command.Password, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                await _identityService.AddToRoleAsync(user.UserId, ApplicationRoles.Employee, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                IReadOnlyCollection<string> roles = await _identityService.GetRolesAsync(user.UserId, cancellationToken);

                return _jwtTokenGenerator.Generate(user, roles);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }

        public async Task<AuthenticationResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);
            cancellationToken.ThrowIfCancellationRequested();

            string email = command.Email.Trim();

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(command.Password))
            {
                throw new AuthenticationFailedException();
            }

            IdentityUserResult? user = await _identityService.FindByEmailAsync(email, cancellationToken);

            if (user is null)
            {
                throw new AuthenticationFailedException();
            }

            PasswordCheckResult passwordCheckResult = await _identityService.CheckPasswordAsync(user.UserId, command.Password, lockoutOnFailure: true);

            if (passwordCheckResult.IsLockedOut)
            {
                throw new AccountLockedException();
            }

            if (passwordCheckResult.IsNotAllowed)
            {
                throw new AuthenticationFailedException();
            }

            if (!passwordCheckResult.Succeeded)
            {
                throw new AuthenticationFailedException();
            }

            IReadOnlyCollection<string> roles = await _identityService.GetRolesAsync(user.UserId, cancellationToken);

            return _jwtTokenGenerator.Generate(user, roles);
        }

        private static void ValidateRegistrationCommand(string fullName, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new UseCaseValidationException("Full name is required.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new UseCaseValidationException("Email is required.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new UseCaseValidationException("Password is required.");
            }
        }
    }
}