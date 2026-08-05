using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace DocuTrack.Api.IntegrationTests.Authentication;

public sealed class TestAuthHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "DocuTrackTestScheme";

    public const string DefaultUserId =
        "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

    public const string DefaultEmail =
        "integration-test@doctrack.com";

    public const string EmployeeRole = "Employee";
    public const string ReviewerRole = "Reviewer";
    public const string AdminRole = "Admin";

    public const string RoleHeader = "X-Test-Role";
    public const string UserIdHeader = "X-Test-UserId";
    public const string EmailHeader = "X-Test-Email";
    public const string AnonymousHeader = "X-Test-Anonymous";

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
                AnonymousHeader,
                out var anonymousValue) &&
            string.Equals(
                anonymousValue.ToString(),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(
                AuthenticateResult.NoResult());
        }

        string userId =
            GetHeaderValue(
                UserIdHeader,
                DefaultUserId);

        string email =
            GetHeaderValue(
                EmailHeader,
                DefaultEmail);

        string rolesValue =
            GetHeaderValue(
                RoleHeader,
                EmployeeRole);

        List<Claim> claims =
        [
            new(
                ClaimTypes.NameIdentifier,
                userId),

            new(
                ClaimTypes.Name,
                email),

            new(
                ClaimTypes.Email,
                email)
        ];

        string[] roles = rolesValue.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);

        foreach (string role in roles)
        {
            claims.Add(
                new Claim(
                    ClaimTypes.Role,
                    role));
        }

        ClaimsIdentity identity = new(
            claims,
            SchemeName,
            ClaimTypes.Name,
            ClaimTypes.Role);

        ClaimsPrincipal principal = new(identity);

        AuthenticationTicket ticket = new(
            principal,
            SchemeName);

        return Task.FromResult(
            AuthenticateResult.Success(ticket));
    }

    private string GetHeaderValue(
        string headerName,
        string defaultValue)
    {
        if (Request.Headers.TryGetValue(
                headerName,
                out var value) &&
            !string.IsNullOrWhiteSpace(value.ToString()))
        {
            return value.ToString();
        }

        return defaultValue;
    }
}