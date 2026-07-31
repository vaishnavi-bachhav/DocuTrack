using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace DocuTrack.Api.IntegrationTests.Authentication;

public sealed class TestAuthHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "TestScheme";

    public const string DefaultUserId =
        "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

    public const string DefaultEmail =
        "integration-test@doctrack.com";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult>
        HandleAuthenticateAsync()
    {
        if (Request.Headers.TryGetValue(
                "X-Test-Anonymous",
                out var anonymousHeader) &&
            string.Equals(
                anonymousHeader.ToString(),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(
                AuthenticateResult.NoResult());
        }

        List<Claim> claims =
        [
            new Claim(
                ClaimTypes.NameIdentifier,
                DefaultUserId),

            new Claim(
                ClaimTypes.Name,
                DefaultEmail),

            new Claim(
                ClaimTypes.Email,
                DefaultEmail)
        ];

        if (Request.Headers.TryGetValue(
                "X-Test-Role",
                out var roleHeader) &&
            !string.IsNullOrWhiteSpace(roleHeader.ToString()))
        {
            string[] requestedRoles =
                roleHeader
                    .ToString()
                    .Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries);

            foreach (string role in requestedRoles)
            {
                claims.Add(
                    new Claim(
                        ClaimTypes.Role,
                        role));
            }
        }
        else
        {
            // Default integration-test user has every role.
            // This allows business-behavior tests to reach the service.
            claims.Add(
                new Claim(
                    ClaimTypes.Role,
                    "Employee"));

            claims.Add(
                new Claim(
                    ClaimTypes.Role,
                    "Reviewer"));

            claims.Add(
                new Claim(
                    ClaimTypes.Role,
                    "Admin"));
        }

        ClaimsIdentity identity = new(
            claims,
            AuthenticationScheme,
            ClaimTypes.Name,
            ClaimTypes.Role);

        ClaimsPrincipal principal = new(identity);

        AuthenticationTicket ticket = new(
            principal,
            AuthenticationScheme);

        return Task.FromResult(
            AuthenticateResult.Success(ticket));
    }
}