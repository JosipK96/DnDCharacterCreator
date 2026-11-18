using CharacterCreatorApp.Contracts.Common;

using Sentry.AspNetCore;
using Sentry.Extensibility;

namespace CharacterCreatorApp.Api.Infrastructure.ErrorReporting;

public static class ErrorReportingExtensions
{
    /// <summary>
    /// Sends unhandled exceptions to Sentry (#77). The SDK reads the <c>Sentry</c> section; with
    /// no <c>Sentry__Dsn</c> it is switched off and sends nothing, as in development and in the
    /// tests. The DSN is a secret of the host and never in the repository.
    /// </summary>
    public static IWebHostBuilder AddErrorReporting(this IWebHostBuilder webHost) =>
        webHost.UseSentry(options =>
        {
            // The SDK refuses to start with no DSN at all; an empty one switches it off
            options.Dsn ??= string.Empty;
            options.Release = BuildVersion.Of(typeof(Program).Assembly);
            options.SendDefaultPii = false;
            options.MaxRequestBodySize = RequestSize.None;
            options.AutoSessionTracking = false;

            // Log lines and HTTP calls could name what a user did; the event keeps only the exception
            options.SetBeforeBreadcrumb((_, _) => null);
            options.SetBeforeSend((sentryEvent, _) => ErrorScrubber.Scrub(sentryEvent));
        });
}
