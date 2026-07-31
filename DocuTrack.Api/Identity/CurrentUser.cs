using System.Security.Claims;
using DocuTrack.Core.Identity;

namespace DocuTrack.Api.Identity
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

        public Guid UserId
        {
            get
            {
                string? userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!Guid.TryParse(userId, out Guid parsedUserId))
                {
                    throw new InvalidOperationException("The authenticated user ID is missing or invalid");
                }

                return parsedUserId;
            }
        }

        public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);
    }
}
