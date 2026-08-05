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
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<IJwtTokenGenerator> _tokenGeneratorMock;
    private readonly Mock<IIdentityTransactionFactory>
        _transactionFactoryMock;

    private readonly AuthenticationService _service;

    public AuthenticationServiceTests()
    {
        _identityServiceMock = new Mock<IIdentityService>();
        _tokenGeneratorMock = new Mock<IJwtTokenGenerator>();
        _transactionFactoryMock =
            new Mock<IIdentityTransactionFactory>();

        _service = new AuthenticationService(
            _identityServiceMock.Object,
            _tokenGeneratorMock.Object,
            _transactionFactoryMock.Object);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        LoginCommand command = new()
        {
            Email = "employee@doctrack.com",
            Password = "Password@123"
        };

        IdentityUserResult user = CreateIdentityUser(command.Email);

        string[] roles = [ApplicationRoles.Employee];

        AuthenticationResult tokenResult =
            CreateAuthenticationResult(user, roles);

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
                generator.Generate(user, roles))
            .Returns(tokenResult);

        AuthenticationResult result =
            await _service.LoginAsync(command);

        result.Should().BeSameAs(tokenResult);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsAuthenticationFailed()
    {
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

        Func<Task> action = () =>
            _service.LoginAsync(command);

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

        _tokenGeneratorMock.Verify(
            generator =>
                generator.Generate(
                    It.IsAny<IdentityUserResult>(),
                    It.IsAny<IReadOnlyCollection<string>>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsAuthenticationFailed()
    {
        LoginCommand command = new()
        {
            Email = "employee@doctrack.com",
            Password = "WrongPassword@123"
        };

        IdentityUserResult user = CreateIdentityUser(command.Email);

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

        Func<Task> action = () =>
            _service.LoginAsync(command);

        await action.Should()
            .ThrowAsync<AuthenticationFailedException>();

        _tokenGeneratorMock.Verify(
            generator =>
                generator.Generate(
                    It.IsAny<IdentityUserResult>(),
                    It.IsAny<IReadOnlyCollection<string>>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_LockedAccount_ThrowsAccountLocked()
    {
        LoginCommand command = new()
        {
            Email = "locked@doctrack.com",
            Password = "Password@123"
        };

        IdentityUserResult user = CreateIdentityUser(command.Email);

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

        Func<Task> action = () =>
            _service.LoginAsync(command);

        await action.Should()
            .ThrowAsync<AccountLockedException>();

        _tokenGeneratorMock.Verify(
            generator =>
                generator.Generate(
                    It.IsAny<IdentityUserResult>(),
                    It.IsAny<IReadOnlyCollection<string>>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ValidCommand_CommitsAndReturnsToken()
    {
        RegisterUserCommand command = new()
        {
            FullName = "Test Employee",
            Email = "employee@doctrack.com",
            Password = "Password@123"
        };

        IdentityUserResult user =
            CreateIdentityUser(command.Email);

        string[] roles = [ApplicationRoles.Employee];

        AuthenticationResult tokenResult =
            CreateAuthenticationResult(user, roles);

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
                generator.Generate(user, roles))
            .Returns(tokenResult);

        AuthenticationResult result =
            await _service.RegisterAsync(command);

        result.Should().BeSameAs(tokenResult);

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
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsConflict()
    {
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

        Func<Task> action = () =>
            _service.RegisterAsync(command);

        await action.Should()
            .ThrowAsync<UserAlreadyExistsException>();

        _transactionFactoryMock.Verify(
            factory =>
                factory.BeginAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_RoleAssignmentFails_RollsBack()
    {
        RegisterUserCommand command = new()
        {
            FullName = "Rollback User",
            Email = "rollback@doctrack.com",
            Password = "Password@123"
        };

        IdentityUserResult user =
            CreateIdentityUser(command.Email);

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

        Func<Task> action = () =>
            _service.RegisterAsync(command);

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

        _tokenGeneratorMock.Verify(
            generator =>
                generator.Generate(
                    It.IsAny<IdentityUserResult>(),
                    It.IsAny<IReadOnlyCollection<string>>()),
            Times.Never);
    }

    private static IdentityUserResult CreateIdentityUser(
        string email)
    {
        return new IdentityUserResult
        {
            UserId = Guid.NewGuid(),
            Email = email,
            FullName = "Test User"
        };
    }

    private static AuthenticationResult
        CreateAuthenticationResult(
            IdentityUserResult user,
            IReadOnlyCollection<string> roles)
    {
        return new AuthenticationResult
        {
            AccessToken = "test-access-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles
        };
    }
}