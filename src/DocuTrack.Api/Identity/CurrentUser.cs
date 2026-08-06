using System.Security.Claims;
using DocuTrack.Application.Abstractions.Authorization;

namespace DocuTrack.Api.Identity;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor
        _httpContextAccessor;

    public CurrentUser(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor
            ?? throw new ArgumentNullException(
                nameof(httpContextAccessor));
    }

    public bool IsAuthenticated =>
        User.Identity?.IsAuthenticated == true;

    public Guid UserId
    {
        get
        {
            string? value =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(value, out Guid userId))
            {
                throw new InvalidOperationException(
                    "The authenticated user ID is missing or invalid.");
            }

            return userId;
        }
    }

    public string? Email =>
        User.FindFirstValue(ClaimTypes.Email)
        ?? User.FindFirstValue(
            System.IdentityModel.Tokens.Jwt
                .JwtRegisteredClaimNames.Email);

    private ClaimsPrincipal User =>
        _httpContextAccessor.HttpContext?.User
        ?? new ClaimsPrincipal();
}