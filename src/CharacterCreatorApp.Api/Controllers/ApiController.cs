using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Domain.Common.Results;

using Microsoft.AspNetCore.Mvc;

namespace CharacterCreatorApp.Api.Controllers;

/// <summary>
/// Turns what a handler returns into an HTTP result, so no action writes its own IsSuccess
/// branch. Every failure becomes the same <see cref="CustomProblemDetails"/>: the first error's
/// type picks the status, its description is the detail, and every error is listed with its code.
/// </summary>
[ApiController]
public abstract class ApiController : ControllerBase
{
    protected IActionResult OkOrProblem<T>(Response<T> response) =>
        response.IsSuccess ? Ok(response.Value) : Problem(response.Errors);

    protected IActionResult OkOrProblem<T, TResult>(Response<T> response, Func<T, TResult> map) =>
        response.IsSuccess ? Ok(map(response.Value)) : Problem(response.Errors);

    protected IActionResult NoContentOrProblem(Response response) =>
        response.IsSuccess ? NoContent() : Problem(response.Errors);

    protected ObjectResult Problem(IReadOnlyList<Error> errors)
    {
        var statusCode = errors.Count == 0
            ? StatusCodes.Status500InternalServerError
            : GetStatusCode(errors[0].Type);

        // CustomProblemDetailsFactory is the registered ProblemDetailsFactory and always creates
        // CustomProblemDetails; the cast fails loudly if that registration is ever replaced.
        var problem = (CustomProblemDetails)ProblemDetailsFactory.CreateProblemDetails(
            HttpContext,
            statusCode,
            detail: errors.Count == 0 ? "The request failed without saying why." : errors[0].Description,
            instance: HttpContext.Request.Path);

        problem.Errors.AddRange(errors.Select(ProblemDetailsError.From));

        return new ObjectResult(problem) { StatusCode = statusCode };
    }

    private static int GetStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Failure => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status500InternalServerError
    };
}
