using DocuTrack.Api.Contracts.Requests;
using DocuTrack.Api.Contracts.Responses;
using DocuTrack.Api.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DocuTrack.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;

        public AuthController(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthenticationResponse>> Register([FromBody] RegisterApiRequest request, CancellationToken cancellationToken)
        {
            AuthenticationResponse response = await _authenticationService.RegisterAsync(request, cancellationToken);

            return StatusCode(StatusCodes.Status201Created, response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthenticationResponse>> Login([FromBody] LoginApiRequest request, CancellationToken cancellationToken)
        {
            AuthenticationResponse response = await _authenticationService.LoginAsync(request, cancellationToken);

            return Ok(response);
        }
    }
}
