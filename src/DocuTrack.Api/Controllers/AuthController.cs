using DocuTrack.Api.Contracts.Requests;
using DocuTrack.Api.Contracts.Responses;
using DocuTrack.Api.DependencyInjection;
using DocuTrack.Api.Mappings;
using DocuTrack.Application.Abstractions.Authentication;
using DocuTrack.Application.Authentication.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DocuTrack.Api.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;

        public AuthController(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService
                ?? throw new ArgumentNullException(
                    nameof(authenticationService));
        }

        [EnableRateLimiting(RateLimitingExtensions.RegistrationPolicy)]
        [HttpPost("register")]
        public async Task<ActionResult<AuthenticationResponse>> Register([FromBody] RegisterApiRequest request, CancellationToken cancellationToken)
        {
            AuthenticationResult result = await _authenticationService.RegisterAsync(request.ToCommand(), cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result.ToResponse());
        }

        [EnableRateLimiting(RateLimitingExtensions.LoginPolicy)]
        [HttpPost("login")]
        public async Task<ActionResult<AuthenticationResponse>> Login([FromBody] LoginApiRequest request, CancellationToken cancellationToken)
        {
            AuthenticationResult result = await _authenticationService.LoginAsync(request.ToCommand(), cancellationToken);
            return Ok(result.ToResponse());
        }

        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingExtensions.RefreshPolicy)]
        [HttpPost("refresh")]
        public async Task<ActionResult<AuthenticationResponse>> Refresh([FromBody] RefreshTokenApiRequest request, CancellationToken cancellationToken)
        {
            AuthenticationResult result = await _authenticationService.RefreshAsync(request.ToCommand(), cancellationToken);
            return Ok(result.ToResponse());
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RevokeRefreshTokenApiRequest request, CancellationToken cancellationToken)
        {
            await _authenticationService.RevokeAsync(request.ToCommand(), cancellationToken);
            return NoContent();
        }
    }
}
