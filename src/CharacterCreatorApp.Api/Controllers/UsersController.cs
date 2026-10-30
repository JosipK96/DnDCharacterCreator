using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Application.Features.Authentication.ChangePassword;
using CharacterCreatorApp.Application.Features.Authentication.Login;
using CharacterCreatorApp.Application.Features.Authentication.Register;
using CharacterCreatorApp.Application.Features.Authentication.ResetPassword;
using CharacterCreatorApp.Contracts.Requests.User;
using CharacterCreatorApp.Contracts.Responses;

using CharacterCreatorApp.Api.Infrastructure.RateLimiting;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

using Microsoft.AspNetCore.Mvc;

namespace CharacterCreatorApp.Api.Controllers
{
    [Route("api/users")]
    public class UsersController(ISender sender) : ApiController
    {
        private readonly ISender _sender = sender;

        [HttpPost("register")]
        [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                new RegisterUserCommand(request.Email, request.UserName, request.Password, request.DisplayName, request.InviteCode),
                cancellationToken);

            return OkOrProblem(response);
        }

        [HttpPost("login")]
        [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
        [ProducesResponseType(typeof(LoginApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login([FromBody] LoginUserRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new LoginUserCommand(request.Email, request.Password), cancellationToken);

            return OkOrProblem(response, login => new LoginApiResponse(login.Token));
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new ResetPasswordCommand(request.Email, request.Token, request.NewPassword), cancellationToken);

            return NoContentOrProblem(response);
        }

        [Authorize]
        [HttpPost("change-password")]
        [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), cancellationToken);

            return NoContentOrProblem(response);
        }
    }
}
