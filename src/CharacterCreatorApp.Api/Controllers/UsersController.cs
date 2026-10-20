using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Application.Features.Authentication.Login;
using CharacterCreatorApp.Application.Features.Authentication.Register;
using CharacterCreatorApp.Contracts.Requests.User;
using CharacterCreatorApp.Contracts.Responses;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace CharacterCreatorApp.Api.Controllers
{
    [Route("api/users")]
    public class UsersController(ISender sender) : ApiController
    {
        private readonly ISender _sender = sender;

        [HttpPost("register")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                new RegisterUserCommand(request.Email, request.UserName, request.Password, request.DisplayName, request.InviteCode),
                cancellationToken);

            return OkOrProblem(response);
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginUserRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new LoginUserCommand(request.Email, request.Password), cancellationToken);

            return OkOrProblem(response, login => new LoginApiResponse(login.Token));
        }
    }
}
