using System.Net.Http.Json;

using CharacterCreatorApp.Contracts.Requests.ClientError;

using Microsoft.Extensions.Logging;

namespace CharacterCreatorApp.Client.Services.ErrorReporting;

/// <summary>
/// Blazor writes an unhandled error in a component or an event handler to the logger. This
/// sends those to the API (#77), which passes them to the error tracker; the client holds no key.
/// What it sends is the exception's type and stack, the route without its query and the client's
/// version, never the message, which can carry what the user typed. A page load reports at most
/// <see cref="MaxReports"/> errors, so a render loop cannot flood the API.
/// </summary>
public sealed class ClientErrorLoggerProvider(HttpClient http, Func<string> currentPath, string version) : ILoggerProvider
{
    public const int MaxReports = 5;
    public const string ReportPath = "api/client-errors";

    private readonly HttpClient _http = http;
    private readonly Func<string> _currentPath = currentPath;
    private readonly string _version = version;
    private int _reported;

    public ILogger CreateLogger(string categoryName) => new ClientErrorLogger(this);

    public void Dispose()
    {
    }

    private void Report(Exception exception)
    {
        if (Interlocked.Increment(ref _reported) > MaxReports)
        {
            return;
        }

        var request = new ReportClientErrorRequest(
            exception.GetType().FullName ?? exception.GetType().Name,
            exception.StackTrace ?? string.Empty,
            _currentPath(),
            _version);

        // Not awaited: reporting must never slow or break the page. A failed report is dropped
        // (the logger must not log again, which would loop).
        _ = SendAsync(request);
    }

    private async Task SendAsync(ReportClientErrorRequest request)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync(ReportPath, request);
        }
        catch (Exception)
        {
            // shortcut: an error that cannot be reported is lost, when it matters add a retry
        }
    }

    private sealed class ClientErrorLogger(ClientErrorLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (exception is not null && IsEnabled(logLevel))
            {
                provider.Report(exception);
            }
        }
    }
}
