using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace CharacterCreatorApp.Api.Infrastructure.ExceptionHandling;

/// <summary>
/// The last line: anything a handler did not turn into a Response lands here as a 500 in the
/// same problem shape as every other error.
/// </summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    ProblemDetailsFactory problemDetailsFactory,
    IHostEnvironment environment) : IExceptionHandler
{
    public const string UnexpectedErrorCode = "Server.Unexpected";

    private readonly ILogger<GlobalExceptionHandler> _logger = logger;
    private readonly ProblemDetailsFactory _problemDetailsFactory = problemDetailsFactory;
    private readonly IHostEnvironment _environment = environment;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        // An exception message can name tables, ids or file paths, so only a developer sees it
        var detail = _environment.IsDevelopment()
            ? exception.Message
            : "An unexpected error occurred.";

        var problem = (CustomProblemDetails)_problemDetailsFactory.CreateProblemDetails(
            httpContext,
            StatusCodes.Status500InternalServerError,
            detail: detail,
            instance: httpContext.Request.Path);

        problem.Errors.Add(ProblemDetailsError.Create(UnexpectedErrorCode, detail: detail));

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken);

        return true;
    }
}
