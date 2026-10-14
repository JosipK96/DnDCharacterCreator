using System.Diagnostics;

using CharacterCreatorApp.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace CharacterCreatorApp.Application.Common.Behaviours;

/// <summary>
/// Logs each request that comes back with errors, with their codes, and how long every request
/// took at Debug. A failure is Information, not a warning: a wrong password or an incomplete
/// draft is a normal outcome, but a run of them is what to look for when someone reports a
/// problem. Unhandled exceptions are GlobalExceptionHandler's to log, not this behaviour's.
/// </summary>
public class LoggingBehaviour<TRequest, TResponse>(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResponse
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger = logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();

        var response = await next();

        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

        if (response.IsSuccess)
        {
            _logger.LogDebug("{Request} handled in {ElapsedMs:F0} ms", typeof(TRequest).Name, elapsedMs);
        }
        else
        {
            _logger.LogInformation(
                "{Request} failed in {ElapsedMs:F0} ms with {ErrorCodes}",
                typeof(TRequest).Name,
                elapsedMs,
                string.Join(", ", (response.Errors ?? []).Select(e => e.Code)));
        }

        return response;
    }
}
