using DocuTrack.Api.Contracts.Responses;
using DocuTrack.Application.Authorization;
using DocuTrack.Infrastructure.Identity;
using DocuTrack.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocuTrack.Api.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class AuthApiTests
    : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions =
        CreateJsonOptions();

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthApiTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await ResetDatabaseAsync();
        await SeedRolesAsync();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsCreated()
    {
        const string email =
            "new-user@doctrack.com";

        var request = new
        {
            fullName = "New Test User",
            email,
            password = "Password@123"
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                request);

        string responseBody =
            await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.Created,
            $"response body was: {responseBody}");

        AuthenticationResponse? body =
            await response.Content
                .ReadFromJsonAsync<AuthenticationResponse>(
                    JsonOptions);

        body.Should().NotBeNull();
        body!.AccessToken.Should()
            .NotBeNullOrWhiteSpace();

        body.Email.Should().Be(email);
        body.FullName.Should().Be("New Test User");

        body.Roles.Should().Contain(
            ApplicationRoles.Employee);

        await using AsyncServiceScope scope =
            _factory.Services.CreateAsyncScope();

        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<ApplicationUser>>();

        ApplicationUser? savedUser =
            await userManager.FindByEmailAsync(email);

        savedUser.Should().NotBeNull();

        bool isEmployee =
            await userManager.IsInRoleAsync(
                savedUser!,
                ApplicationRoles.Employee);

        isEmployee.Should().BeTrue();
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        const string email =
            "duplicate@doctrack.com";

        await SeedIdentityUserAsync(
            email,
            "Password@123");

        var request = new
        {
            fullName = "Duplicate User",
            email,
            password = "Password@123"
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                request);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsBadRequest()
    {
        var request = new
        {
            fullName = "Weak Password User",
            email = "weak@doctrack.com",
            password = "weak"
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                request);

        response.StatusCode.Should()
            .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        const string email =
            "login-user@doctrack.com";

        const string password =
            "Password@123";

        await SeedIdentityUserAsync(
            email,
            password);

        var request = new
        {
            email,
            password
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                request);

        string responseBody =
            await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            $"response body was: {responseBody}");

        AuthenticationResponse? body =
            await response.Content
                .ReadFromJsonAsync<AuthenticationResponse>(
                    JsonOptions);

        body.Should().NotBeNull();
        body!.AccessToken.Should()
            .NotBeNullOrWhiteSpace();

        body.Email.Should().Be(email);
        body.AccessTokenExpiresAt.Should()
            .BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        const string email =
            "invalid-password@doctrack.com";

        await SeedIdentityUserAsync(
            email,
            "Password@123");

        var request = new
        {
            email,
            password = "WrongPassword@123"
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                request);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        var request = new
        {
            email = "missing@doctrack.com",
            password = "Password@123"
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                request);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_RepeatedInvalidPasswords_LocksAccount()
    {
        const string email =
            "locked@doctrack.com";

        const string correctPassword =
            "Password@123";

        await SeedIdentityUserAsync(
            email,
            correctPassword);

        for (int attempt = 1;
             attempt <= 4;
             attempt++)
        {
            HttpResponseMessage failedResponse =
                await _client.PostAsJsonAsync(
                    "/api/auth/login",
                    new
                    {
                        email,
                        password = "WrongPassword@123"
                    });

            failedResponse.StatusCode.Should().Be(
                HttpStatusCode.Unauthorized,
                $"attempt {attempt} should not yet report lockout");
        }

        HttpResponseMessage lockedResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    email,
                    password = "WrongPassword@123"
                });

        string responseBody =
            await lockedResponse.Content
                .ReadAsStringAsync();

        lockedResponse.StatusCode.Should().Be(
            (HttpStatusCode)StatusCodes.Status423Locked,
            $"response body was: {responseBody}");

        await using AsyncServiceScope scope =
            _factory.Services.CreateAsyncScope();

        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<ApplicationUser>>();

        ApplicationUser? user =
            await userManager.FindByEmailAsync(email);

        user.Should().NotBeNull();
        user!.LockoutEnd.Should().NotBeNull();
        user.LockoutEnd.Should()
            .BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_LockedUserWithCorrectPassword_ReturnsLocked()
    {
        const string email = "prelocked@doctrack.com";
        const string password = "Password@123";

        await SeedIdentityUserAsync(
            email,
            password);

        await using (AsyncServiceScope scope =
                     _factory.Services.CreateAsyncScope())
        {
            UserManager<ApplicationUser> userManager =
                scope.ServiceProvider
                    .GetRequiredService<
                        UserManager<ApplicationUser>>();

            ApplicationUser? user =
                await userManager.FindByEmailAsync(email);

            user.Should().NotBeNull();

            IdentityResult result =
                await userManager.SetLockoutEndDateAsync(
                    user!,
                    DateTimeOffset.UtcNow.AddMinutes(15));

            result.Succeeded.Should().BeTrue(
                string.Join(
                    "; ",
                    result.Errors.Select(
                        error => error.Description)));
        }

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    email,
                    password
                });

        string responseBody =
            await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            (HttpStatusCode)StatusCodes.Status423Locked,
            $"response body was: {responseBody}");
    }

    [Fact]
    public async Task Login_SuccessAfterFailedAttempt_ResetsFailureCount()
    {
        const string email =
            "reset-failures@doctrack.com";

        const string correctPassword =
            "Password@123";

        await SeedIdentityUserAsync(
            email,
            correctPassword);

        HttpResponseMessage failureResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    email,
                    password = "WrongPassword@123"
                });

        failureResponse.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        HttpResponseMessage successfulResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    email,
                    password = correctPassword
                });

        successfulResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        await using AsyncServiceScope scope =
            _factory.Services.CreateAsyncScope();

        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<ApplicationUser>>();

        ApplicationUser? user =
            await userManager.FindByEmailAsync(email);

        user.Should().NotBeNull();

        int failureCount =
            await userManager
                .GetAccessFailedCountAsync(user!);

        failureCount.Should().Be(0);
    }

    private async Task ResetDatabaseAsync()
    {
        await using AsyncServiceScope scope =
            _factory.Services.CreateAsyncScope();

        DocuTrackDbContext context =
            scope.ServiceProvider
                .GetRequiredService<
                    DocuTrackDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    private async Task SeedRolesAsync()
    {
        await using AsyncServiceScope scope =
            _factory.Services.CreateAsyncScope();

        RoleManager<IdentityRole<Guid>> roleManager =
            scope.ServiceProvider
                .GetRequiredService<
                    RoleManager<IdentityRole<Guid>>>();

        string[] roles =
        [
            ApplicationRoles.Admin,
            ApplicationRoles.Reviewer,
            ApplicationRoles.Employee
        ];

        foreach (string roleName in roles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            IdentityResult result =
                await roleManager.CreateAsync(
                    new IdentityRole<Guid>(roleName));

            result.Succeeded.Should().BeTrue(
                string.Join(
                    "; ",
                    result.Errors.Select(
                        error => error.Description)));
        }
    }

    private async Task<Guid> SeedIdentityUserAsync(
    string email,
    string password)
    {
        await using AsyncServiceScope scope =
            _factory.Services.CreateAsyncScope();

        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<ApplicationUser>>();

        ApplicationUser user = new()
        {
            Id = Guid.NewGuid(),
            FullName = "Integration Test User",
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            LockoutEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        IdentityResult createResult =
            await userManager.CreateAsync(
                user,
                password);

        createResult.Succeeded.Should().BeTrue(
            string.Join(
                "; ",
                createResult.Errors.Select(
                    error => error.Description)));

        IdentityResult roleResult =
            await userManager.AddToRoleAsync(
                user,
                ApplicationRoles.Employee);

        roleResult.Succeeded.Should().BeTrue(
            string.Join(
                "; ",
                roleResult.Errors.Select(
                    error => error.Description)));

        return user.Id;
    }

    private static JsonSerializerOptions
        CreateJsonOptions()
    {
        JsonSerializerOptions options =
            new(JsonSerializerDefaults.Web);

        options.Converters.Add(
            new JsonStringEnumConverter());

        return options;
    }

}