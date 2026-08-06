using DocuTrack.Application.Abstractions.Authentication;
using DocuTrack.Application.Authentication;
using DocuTrack.Application.Authentication.Commands;
using DocuTrack.Application.Authentication.Results;
using DocuTrack.Application.Authorization;
using DocuTrack.Application.Common.Exceptions;
using FluentAssertions;
using Moq;

namespace DocuTrack.Application.Tests.Authentication;

public sealed class AuthenticationServiceTests
{
    private static readonly DateTimeOffset AccessTokenExpiresAt =
        new(
            2026,
            8,
            6,
            17,
            0,
            0,
            TimeSpan.Zero);

    private static readonly DateTimeOffset RefreshTokenExpiresAt =
        new(
            2026,
            8,
            13,
            17,
            0,
            0,
            TimeSpan.Zero);

    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<IJwtTokenGenerator> _tokenGeneratorMock;
    private readonly Mock<IIdentityTransactionFactory>
        _transactionFactoryMock;
    private readonly Mock<IRefreshTokenService>
        _refreshTokenServiceMock;

    private readonly AuthenticationService _service;

    public AuthenticationServiceTests()
    {
        _identityServiceMock = new Mock<IIdentityService>();
        _tokenGeneratorMock = new Mock<IJwtTokenGenerator>();
        _transactionFactoryMock =
            new Mock<IIdentityTransactionFactory>();
        _refreshTokenServiceMock =
            new Mock<IRefreshTokenService>();

        // Match the constructor order in AuthenticationService.
        _service = new AuthenticationService(
            _identityServiceMock.Object,
            _tokenGeneratorMock.Object,
            _transactionFactoryMock.Object,
            _refreshTokenServiceMock.Object );
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthenticationResult()
    {
        // Arrange
        LoginCommand command = new()
        {
            Email = "employee@doctrack.com",
            Password = "Password@123"
        };

        IdentityUserResult user =
            CreateIdentityUser(command.Email);

        string[] roles =
        [
            ApplicationRoles.Employee
        ];

        AccessTokenResult accessToken =
            CreateAccessToken();

        IssuedRefreshToken refreshToken =
            CreateRefreshToken();

        _identityServiceMock
            .Setup(service =>
                service.FindByEmailAsync(
                    command.Email,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _identityServiceMock
            .Setup(service =>
                service.CheckPasswordAsync(
                    user.UserId,
                    command.Password,
                    true,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordCheckResult
            {
                Succeeded = true
            });

        _identityServiceMock
            .Setup(service =>
                service.GetRolesAsync(
                    user.UserId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        _tokenGeneratorMock
            .Setup(generator =>
                generator.GenerateAccessToken(
                    user,
                    roles))
            .Returns(accessToken);

        _refreshTokenServiceMock
            .Setup(service =>
                service.IssueAsync(
                    user.UserId,
                    null,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        // Act
        AuthenticationResult result =
            await _service.LoginAsync(command);

        // Assert
        AssertAuthenticationResult(
            result,
            user,
            roles,
            accessToken,
            refreshToken);

        _tokenGeneratorMock.Verify(
            generator =>
                generator.GenerateAccessToken(
                    user,
                    roles),
            Times.Once);

        _refreshTokenServiceMock.Verify(
            service =>
                service.IssueAsync(
                    user.UserId,
                    null,
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsAuthenticationFailed()
    {
        // Arrange
        LoginCommand command = new()
        {
            Email = "missing@doctrack.com",
            Password = "Password@123"
        };

        _identityServiceMock
            .Setup(service =>
                service.FindByEmailAsync(
                    command.Email,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdentityUserResult?)null);

        // Act
        Func<Task> action = () =>
            _service.LoginAsync(command);

        // Assert
        await action.Should()
            .ThrowAsync<AuthenticationFailedException>();

        _identityServiceMock.Verify(
            service =>
                service.CheckPasswordAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);

        VerifyTokensWereNotGenerated();
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsAuthenticationFailed()
    {
        // Arrange
        LoginCommand command = new()
        {
            Email = "employee@doctrack.com",
            Password = "WrongPassword@123"
        };

        IdentityUserResult user =
            CreateIdentityUser(command.Email);

        _identityServiceMock
            .Setup(service =>
                service.FindByEmailAsync(
                    command.Email,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _identityServiceMock
            .Setup(service =>
                service.CheckPasswordAsync(
                    user.UserId,
                    command.Password,
                    true,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordCheckResult
            {
                Succeeded = false
            });

        // Act
        Func<Task> action = () =>
            _service.LoginAsync(command);

        // Assert
        await action.Should()
            .ThrowAsync<AuthenticationFailedException>();

        VerifyTokensWereNotGenerated();
    }

    [Fact]
    public async Task LoginAsync_NotAllowed_ThrowsAuthenticationFailed()
    {
        // Arrange
        LoginCommand command = new()
        {
            Email = "unconfirmed@doctrack.com",
            Password = "Password@123"
        };

        IdentityUserResult user =
            CreateIdentityUser(command.Email);

        _identityServiceMock
            .Setup(service =>
                service.FindByEmailAsync(
                    command.Email,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _identityServiceMock
            .Setup(service =>
                service.CheckPasswordAsync(
                    user.UserId,
                    command.Password,
                    true,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordCheckResult
            {
                IsNotAllowed = true
            });

        // Act
        Func<Task> action = () =>
            _service.LoginAsync(command);

        // Assert
        await action.Should()
            .ThrowAsync<AuthenticationFailedException>();

        VerifyTokensWereNotGenerated();
    }

    [Fact]
    public async Task LoginAsync_LockedAccount_ThrowsAccountLocked()
    {
        // Arrange
        LoginCommand command = new()
        {
            Email = "locked@doctrack.com",
            Password = "Password@123"
        };

        IdentityUserResult user =
            CreateIdentityUser(command.Email);

        _identityServiceMock
            .Setup(service =>
                service.FindByEmailAsync(
                    command.Email,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _identityServiceMock
            .Setup(service =>
                service.CheckPasswordAsync(
                    user.UserId,
                    command.Password,
                    true,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordCheckResult
            {
                IsLockedOut = true
            });

        // Act
        Func<Task> action = () =>
            _service.LoginAsync(command);

        // Assert
        await action.Should()
            .ThrowAsync<AccountLockedException>();

        VerifyTokensWereNotGenerated();
    }

    [Fact]
    public async Task RegisterAsync_ValidCommand_CommitsAndReturnsAuthenticationResult()
    {
        // Arrange
        RegisterUserCommand command = new()
        {
            FullName = "Test Employee",
            Email = "employee@doctrack.com",
            Password = "Password@123"
        };

        IdentityUserResult user =
            CreateIdentityUser(
                command.Email,
                command.FullName);

        string[] roles =
        [
            ApplicationRoles.Employee
        ];

        AccessTokenResult accessToken =
            CreateAccessToken();

        IssuedRefreshToken refreshToken =
            CreateRefreshToken();

        Mock<IIdentityTransaction> transactionMock = new();

        _identityServiceMock
            .Setup(service =>
                service.FindByEmailAsync(
                    command.Email,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdentityUserResult?)null);

        _transactionFactoryMock
            .Setup(factory =>
                factory.BeginAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        _identityServiceMock
            .Setup(service =>
                service.CreateUserAsync(
                    command.FullName,
                    command.Email,
                    command.Password,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _identityServiceMock
            .Setup(service =>
                service.AddToRoleAsync(
                    user.UserId,
                    ApplicationRoles.Employee,
                    It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _identityServiceMock
            .Setup(service =>
                service.GetRolesAsync(
                    user.UserId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        _tokenGeneratorMock
            .Setup(generator =>
                generator.GenerateAccessToken(
                    user,
                    roles))
            .Returns(accessToken);

        // This setup must happen before RegisterAsync is called.
        _refreshTokenServiceMock
            .Setup(service =>
                service.IssueAsync(
                    user.UserId,
                    null,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        // Act
        AuthenticationResult result =
            await _service.RegisterAsync(command);

        // Assert
        AssertAuthenticationResult(
            result,
            user,
            roles,
            accessToken,
            refreshToken);

        transactionMock.Verify(
            transaction =>
                transaction.CommitAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        transactionMock.Verify(
            transaction =>
                transaction.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _identityServiceMock.Verify(
            service =>
                service.AddToRoleAsync(
                    user.UserId,
                    ApplicationRoles.Employee,
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _refreshTokenServiceMock.Verify(
            service =>
                service.IssueAsync(
                    user.UserId,
                    null,
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsConflict()
    {
        // Arrange
        RegisterUserCommand command = new()
        {
            FullName = "Duplicate User",
            Email = "duplicate@doctrack.com",
            Password = "Password@123"
        };

        IdentityUserResult existingUser =
            CreateIdentityUser(command.Email);

        _identityServiceMock
            .Setup(service =>
                service.FindByEmailAsync(
                    command.Email,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        Func<Task> action = () =>
            _service.RegisterAsync(command);

        // Assert
        await action.Should()
            .ThrowAsync<UserAlreadyExistsException>();

        _transactionFactoryMock.Verify(
            factory =>
                factory.BeginAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);

        VerifyTokensWereNotGenerated();
    }

    [Fact]
    public async Task RegisterAsync_RoleAssignmentFails_RollsBack()
    {
        // Arrange
        RegisterUserCommand command = new()
        {
            FullName = "Rollback User",
            Email = "rollback@doctrack.com",
            Password = "Password@123"
        };

        IdentityUserResult user =
            CreateIdentityUser(
                command.Email,
                command.FullName);

        Mock<IIdentityTransaction> transactionMock = new();

        _identityServiceMock
            .Setup(service =>
                service.FindByEmailAsync(
                    command.Email,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdentityUserResult?)null);

        _transactionFactoryMock
            .Setup(factory =>
                factory.BeginAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        _identityServiceMock
            .Setup(service =>
                service.CreateUserAsync(
                    command.FullName,
                    command.Email,
                    command.Password,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _identityServiceMock
            .Setup(service =>
                service.AddToRoleAsync(
                    user.UserId,
                    ApplicationRoles.Employee,
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new UserRegistrationException(
                    "Role assignment failed."));

        // Act
        Func<Task> action = () =>
            _service.RegisterAsync(command);

        // Assert
        await action.Should()
            .ThrowAsync<UserRegistrationException>();

        transactionMock.Verify(
            transaction =>
                transaction.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        transactionMock.Verify(
            transaction =>
                transaction.CommitAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);

        VerifyTokensWereNotGenerated();
    }

    [Fact]
    public async Task RegisterAsync_RefreshTokenCreationFails_RollsBack()
    {
        // Arrange
        RegisterUserCommand command = new()
        {
            FullName = "Refresh Failure User",
            Email = "refresh-failure@doctrack.com",
            Password = "Password@123"
        };

        IdentityUserResult user =
            CreateIdentityUser(
                command.Email,
                command.FullName);

        string[] roles =
        [
            ApplicationRoles.Employee
        ];

        AccessTokenResult accessToken =
            CreateAccessToken();

        Mock<IIdentityTransaction> transactionMock = new();

        _identityServiceMock
            .Setup(service =>
                service.FindByEmailAsync(
                    command.Email,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdentityUserResult?)null);

        _transactionFactoryMock
            .Setup(factory =>
                factory.BeginAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        _identityServiceMock
            .Setup(service =>
                service.CreateUserAsync(
                    command.FullName,
                    command.Email,
                    command.Password,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _identityServiceMock
            .Setup(service =>
                service.AddToRoleAsync(
                    user.UserId,
                    ApplicationRoles.Employee,
                    It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _identityServiceMock
            .Setup(service =>
                service.GetRolesAsync(
                    user.UserId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        _tokenGeneratorMock
            .Setup(generator =>
                generator.GenerateAccessToken(
                    user,
                    roles))
            .Returns(accessToken);

        _refreshTokenServiceMock
            .Setup(service =>
                service.IssueAsync(
                    user.UserId,
                    null,
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Refresh-token storage failed."));

        // Act
        Func<Task> action = () =>
            _service.RegisterAsync(command);

        // Assert
        await action.Should()
            .ThrowAsync<InvalidOperationException>();

        transactionMock.Verify(
            transaction =>
                transaction.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        transactionMock.Verify(
            transaction =>
                transaction.CommitAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void VerifyTokensWereNotGenerated()
    {
        _tokenGeneratorMock.Verify(
            generator =>
                generator.GenerateAccessToken(
                    It.IsAny<IdentityUserResult>(),
                    It.IsAny<IReadOnlyCollection<string>>()),
            Times.Never);

        _refreshTokenServiceMock.Verify(
            service =>
                service.IssueAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static void AssertAuthenticationResult(
        AuthenticationResult result,
        IdentityUserResult user,
        IReadOnlyCollection<string> roles,
        AccessTokenResult accessToken,
        IssuedRefreshToken refreshToken)
    {
        result.AccessToken.Should()
            .Be(accessToken.Token);

        result.AccessTokenExpiresAt.Should()
            .Be(accessToken.ExpiresAt);

        result.RefreshToken.Should()
            .Be(refreshToken.RawToken);

        result.RefreshTokenExpiresAt.Should()
            .Be(refreshToken.ExpiresAt);

        result.UserId.Should()
            .Be(user.UserId);

        result.Email.Should()
            .Be(user.Email);

        result.FullName.Should()
            .Be(user.FullName);

        result.Roles.Should()
            .BeEquivalentTo(roles);
    }

    private static IdentityUserResult CreateIdentityUser(
        string email,
        string fullName = "Test User")
    {
        return new IdentityUserResult
        {
            UserId = Guid.NewGuid(),
            Email = email,
            FullName = fullName
        };
    }

    private static AccessTokenResult CreateAccessToken()
    {
        return new AccessTokenResult(
            Token: "test-access-token",
            ExpiresAt: AccessTokenExpiresAt);
    }

    private static IssuedRefreshToken CreateRefreshToken()
    {
        return new IssuedRefreshToken(
            TokenId: Guid.NewGuid(),
            FamilyId: Guid.NewGuid(),
            RawToken: "test-refresh-token",
            ExpiresAt: RefreshTokenExpiresAt);
    }
}