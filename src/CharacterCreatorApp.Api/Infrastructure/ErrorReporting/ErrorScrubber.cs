using System.Text.RegularExpressions;

namespace CharacterCreatorApp.Api.Infrastructure.ErrorReporting;

/// <summary>
/// Strips an event down to what is needed to fix the bug before it leaves the app (#77): the
/// exception, its stack, the request's method and path, the release and the trace id. Nothing a
/// user typed or a browser sent with the request goes to Sentry.
/// </summary>
public static partial class ErrorScrubber
{
    public const string MaskedEmail = "[email]";

    public static SentryEvent Scrub(SentryEvent sentryEvent)
    {
        ArgumentNullException.ThrowIfNull(sentryEvent);

        sentryEvent.User = new SentryUser();
        sentryEvent.ServerName = null;

        var request = sentryEvent.Request;
        request.Cookies = null;
        request.Data = null;
        request.QueryString = null;
        request.Headers.Clear();
        request.Env.Clear();
        request.Url = MaskEmails(WithoutQuery(request.Url));

        if (sentryEvent.Message is { } message)
        {
            message.Message = MaskEmails(message.Message);
            message.Formatted = MaskEmails(message.Formatted);
        }

        if (sentryEvent.SentryExceptions is { } exceptions)
        {
            foreach (var exception in exceptions)
            {
                exception.Value = MaskEmails(exception.Value);
            }
        }

        foreach (var key in sentryEvent.Extra.Keys.ToList())
        {
            if (sentryEvent.Extra[key] is string text)
            {
                sentryEvent.SetExtra(key, MaskEmails(text));
            }
        }

        foreach (var tag in sentryEvent.Tags.ToList())
        {
            sentryEvent.SetTag(tag.Key, MaskEmails(tag.Value)!);
        }

        // A fingerprint can be built from text a client sent, so it is masked like the rest
        sentryEvent.SetFingerprint(sentryEvent.Fingerprint.Select(part => MaskEmails(part)!).ToList());

        return sentryEvent;
    }

    private static string? WithoutQuery(string? url)
    {
        if (url is null)
        {
            return null;
        }

        var end = url.IndexOfAny(['?', '#']);
        return end < 0 ? url : url[..end];
    }

    private static string? MaskEmails(string? text) =>
        text is null ? null : EmailPattern().Replace(text, MaskedEmail);

    [GeneratedRegex(@"[^\s@<>""'(),;:/]+@[^\s@<>""'(),;:/]+\.[^\s@<>""'(),;:/]+")]
    private static partial Regex EmailPattern();
}
