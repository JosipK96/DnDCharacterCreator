using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Api.Infrastructure.RateLimiting;
using CharacterCreatorApp.Application.Features.Authentication.CreatePasswordResetLink;
using CharacterCreatorApp.Contracts.Requests.User;
using CharacterCreatorApp.Contracts.Responses;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CharacterCreatorApp.Api.Controllers;

/// <summary>What only the owner (the account named by Admin__Email) may do; the handler checks who is asking.</summary>
[Authorize]
[Route("api/admin")]
public class AdminController(ISender sender) : ApiController
{
    private readonly ISender _sender = sender;

    [HttpPost("password-reset-links")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    [ProducesResponseType(typeof(PasswordResetLinkApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreatePasswordResetLink([FromBody] CreatePasswordResetLinkRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new CreatePasswordResetLinkCommand(request.Email), cancellationToken);

        // The page opens on this site's own origin, so the link is built from the request's
        var origin = $"{Request.Scheme}://{Request.Host}";

        return OkOrProblem(response, token => new PasswordResetLinkApiResponse(
            $"{origin}/reset-password?email={Uri.EscapeDataString(request.Email)}&token={Uri.EscapeDataString(token)}"));
    }
}
