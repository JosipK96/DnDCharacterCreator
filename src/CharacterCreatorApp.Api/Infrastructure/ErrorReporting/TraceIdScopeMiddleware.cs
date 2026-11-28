using System.Diagnostics;

namespace CharacterCreatorApp.Api.Infrastructure.ErrorReporting;

/// <summary>
/// Tags what Sentry records for a request with the trace id the 500 body carries as
/// <c>traceId</c> (the same value <c>CustomProblemDetailsFactory</c> writes), so a friend's
/// report can be matched to its event. Placed before the exception handler: the framework logs
/// the exception, and so reports it, before the handler runs.
/// </summary>
public sealed class TraceIdScopeMiddleware(RequestDelegate next)
{
    public const string TagName = "trace_id";

    private readonly RequestDelegate _next = next;

    public Task InvokeAsync(HttpContext context, IHub hub)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        hub.ConfigureScope(scope => scope.SetTag(TagName, traceId));

        return _next(context);
    }
}
