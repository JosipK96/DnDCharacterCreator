using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Api.Infrastructure.ErrorReporting;
using CharacterCreatorApp.Api.Infrastructure.RateLimiting;
using CharacterCreatorApp.Contracts.Requests.ClientError;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CharacterCreatorApp.Api.Controllers;

/// <summary>Where the client reports an error it hit (#77); anonymous because the error may be on the sign-in page.</summary>
[AllowAnonymous]
[Route("api/client-errors")]
public class ClientErrorsController(IClientErrorReporter reporter) : ApiController
{
    // Room for the longest stack the reporter keeps, and not for much more
    public const int MaxRequestBytes = 16 * 1024;

    private readonly IClientErrorReporter _reporter = reporter;

    [HttpPost]
    [RequestSizeLimit(MaxRequestBytes)]
    [EnableRateLimiting(RateLimitingExtensions.ClientErrorsPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status429TooManyRequests)]
    public IActionResult Report([FromBody] ReportClientErrorRequest request)
    {
        _reporter.Report(request);

        return NoContent();
    }
}
