using DocuTrack.Infrastructure.Identity;
using DocuTrack.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace DocuTrack.Api.IntegrationTests
{
    public class AuthApiTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;
        public AuthApiTests(CustomWebApplicationFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));

            _client = factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            using IServiceScope scope = _factory.Services.CreateScope();

            DocuTrackDbContext context = scope.ServiceProvider.GetRequiredService<DocuTrackDbContext>();

            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        }

        public Task DisposeAsync()
        {
            _client.Dispose();

            return Task.CompletedTask;
        }

        [Fact]
        public async Task Login_RepeatedInvalidPasswords_LocksAccount()
        {
            // Arrange
            const string email = "lockout-test@doctrack.com";

            const string correctPassword = "Correct@123";

            await SeedIdentityUserAsync(email, correctPassword);

            // Act and assert:
            // first four failures should return 401.
            for (int attempt = 1; attempt <= 4; attempt++)
            {
                HttpResponseMessage failedResponse =
                    await _client.PostAsJsonAsync(
                        "/api/auth/login",
                        new
                        {
                            email,
                            password = "Wrong@123"
                        });

                string responseBody = await failedResponse.Content.ReadAsStringAsync();

                failedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                    $"failed attempt {attempt} returned: {responseBody}");
            }

            // Fifth invalid password reaches the configured limit.
            HttpResponseMessage lockoutResponse =
                await _client.PostAsJsonAsync(
                    "/api/auth/login",
                    new
                    {
                        email,
                        password = "Wrong@123"
                    });

            string lockoutBody = await lockoutResponse.Content.ReadAsStringAsync();

            lockoutResponse.StatusCode.Should().Be(
                (HttpStatusCode)StatusCodes.Status423Locked,
                $"response body was: {lockoutBody}");

            // Confirm Identity actually persisted the lockout.
            using IServiceScope scope = _factory.Services.CreateScope();

            UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            ApplicationUser? lockedUser = await userManager.FindByEmailAsync(email);

            lockedUser.Should().NotBeNull();
            lockedUser!.LockoutEnd.Should().NotBeNull();
            lockedUser.LockoutEnd.Should().BeAfter(DateTimeOffset.UtcNow);
        }

        [Fact]
        public async Task Register_ValidRequest_CreatesEmployeeUser()
        {
            // You must first seed the Employee role because
            // production seeders are skipped in Testing.
            await SeedRoleAsync("Employee");

            const string email = "new-employee@doctrack.com";

            HttpResponseMessage response =
                await _client.PostAsJsonAsync(
                    "/api/auth/register",
                    new
                    {
                        fullName = "New Employee",
                        email,
                        password = "Password@123"
                    });

            string responseBody = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(
                HttpStatusCode.Created,
                $"response body was: {responseBody}");

            using IServiceScope scope = _factory.Services.CreateScope();

            UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            ApplicationUser? user = await userManager.FindByEmailAsync(email);

            user.Should().NotBeNull();

            bool isEmployee = await userManager.IsInRoleAsync(user!, "Employee");

            isEmployee.Should().BeTrue();
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOk()
        {
            // Arrange
            const string email = "successful-login@doctrack.com";

            const string password = "Correct@123";

            await SeedIdentityUserAsync(email, password);

            // Act
            HttpResponseMessage response =
                await _client.PostAsJsonAsync(
                    "/api/auth/login",
                    new
                    {
                        email,
                        password
                    });

            string responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(
                HttpStatusCode.OK,
                $"response body was: {responseBody}");
        }

        [Fact]
        public async Task Login_SuccessAfterFailedAttempt_ResetsFailureCount()
        {
            // Arrange
            const string email = "reset-failures@doctrack.com";

            const string correctPassword = "Correct@123";

            await SeedIdentityUserAsync(email, correctPassword);

            await _client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    email,
                    password = "Wrong@123"
                });

            // Act
            HttpResponseMessage successResponse =
                await _client.PostAsJsonAsync(
                    "/api/auth/login",
                    new
                    {
                        email,
                        password = correctPassword
                    });

            // Assert
            successResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            using IServiceScope scope = _factory.Services.CreateScope();

            UserManager<ApplicationUser> userManager =
                scope.ServiceProvider
                    .GetRequiredService<
                        UserManager<ApplicationUser>>();

            ApplicationUser? user = await userManager.FindByEmailAsync(email);

            user.Should().NotBeNull();

            int failedCount = await userManager.GetAccessFailedCountAsync(user!);

            failedCount.Should().Be(0);
        }

        private async Task SeedRoleAsync(string roleName)
        {
            using IServiceScope scope = _factory.Services.CreateScope();

            RoleManager<IdentityRole<Guid>> roleManager =
                scope.ServiceProvider
                    .GetRequiredService<
                        RoleManager<IdentityRole<Guid>>>();

            if (await roleManager.RoleExistsAsync(roleName))
            {
                return;
            }

            IdentityResult result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));

            result.Succeeded.Should().BeTrue(
                string.Join(
                    "; ",
                    result.Errors.Select(
                        error => error.Description)));
        }

        private async Task<ApplicationUser> SeedIdentityUserAsync(
            string email,
            string password)
        {
            using IServiceScope scope =
                _factory.Services.CreateScope();

            UserManager<ApplicationUser> userManager =
                scope.ServiceProvider
                    .GetRequiredService<
                        UserManager<ApplicationUser>>();

            ApplicationUser? existingUser =
                await userManager.FindByEmailAsync(email);

            if (existingUser is not null)
            {
                return existingUser;
            }

            ApplicationUser user = new()
            {
                Id = Guid.NewGuid(),
                FullName = "Integration Test User",
                UserName = email,
                Email = email,
                EmailConfirmed = true,

                // Required for failed attempts to trigger lockout.
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

            return user;
        }
    }
}
