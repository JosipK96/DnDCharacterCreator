using CharacterCreatorApp.Contracts.Requests.ClientError;

namespace CharacterCreatorApp.Api.Infrastructure.ErrorReporting;

/// <summary>
/// Passes an error the client hit on to Sentry, so the client needs no key of its own: the DSN
/// stays on the host, and the one scrubber covers both. Anyone can post here, so every field is
/// cut to a length and none is trusted as more than text.
/// </summary>
public sealed class ClientErrorReporter(IHub hub) : IClientErrorReporter
{
    public const int MaxTypeLength = 200;
    public const int MaxStackLength = 8000;
    public const int MaxPathLength = 300;
    public const int MaxVersionLength = 50;
    public const string Unknown = "unknown";

    private readonly IHub _hub = hub;

    public void Report(ReportClientErrorRequest request) => _hub.CaptureEvent(CreateEvent(request));

    public static SentryEvent CreateEvent(ReportClientErrorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var type = Cut(request.ExceptionType, MaxTypeLength);
        var stack = Cut(request.StackTrace, MaxStackLength);
        var firstFrame = stack.Split('\n', 2)[0].Trim();

        var sentryEvent = new SentryEvent
        {
            Message = $"Client error: {type}",
            Level = SentryLevel.Error,
            Request = new SentryRequest { Url = Cut(request.Path, MaxPathLength), Method = "GET" }
        };
        sentryEvent.SetTag("source", "client");
        sentryEvent.SetTag("client_version", Cut(request.Version, MaxVersionLength));
        sentryEvent.SetExtra("stack_trace", stack);
        sentryEvent.SetFingerprint(["client", type, firstFrame]);

        return sentryEvent;
    }

    private static string Cut(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Unknown;
        }

        return value.Length <= max ? value : value[..max];
    }
}
