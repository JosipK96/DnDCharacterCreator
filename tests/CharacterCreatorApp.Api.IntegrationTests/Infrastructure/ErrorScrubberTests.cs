using CharacterCreatorApp.Api.Infrastructure.ErrorReporting;

using FluentAssertions;

using Sentry;
using Sentry.Protocol;

namespace CharacterCreatorApp.Api.IntegrationTests.Infrastructure;

/// <summary>What may leave the app in an error event (#77): no personal data, no secrets.</summary>
public class ErrorScrubberTests
{
    [Fact]
    public void Scrub_ARequestWithEverythingABrowserSends_KeepsOnlyTheMethodAndThePath()
    {
        // Arrange
        var sentryEvent = new SentryEvent
        {
            User = new SentryUser { Email = "thorin@example.com", Username = "thorin", IpAddress = "203.0.113.7", Id = "5" },
            ServerName = "render-instance-1",
            Request = new SentryRequest
            {
                Method = "POST",
                Url = "https://app.example.com/api/users/login?inviteCode=secret-code#top",
                QueryString = "inviteCode=secret-code",
                Cookies = "session=abc",
                Data = "{\"password\":\"Erebor123\"}"
            }
        };
        sentryEvent.Request.Headers["Authorization"] = "Bearer token";
        sentryEvent.Request.Env["REMOTE_ADDR"] = "203.0.113.7";

        // Act
        var scrubbed = ErrorScrubber.Scrub(sentryEvent);

        // Assert
        scrubbed.Request.Method.Should().Be("POST");
        scrubbed.Request.Url.Should().Be("https://app.example.com/api/users/login");
        scrubbed.Request.QueryString.Should().BeNull();
        scrubbed.Request.Cookies.Should().BeNull();
        scrubbed.Request.Data.Should().BeNull();
        scrubbed.Request.Headers.Should().BeEmpty();
        scrubbed.Request.Env.Should().BeEmpty();
        scrubbed.User.Email.Should().BeNull();
        scrubbed.User.Username.Should().BeNull();
        scrubbed.User.IpAddress.Should().BeNull();
        scrubbed.User.Id.Should().BeNull();
        scrubbed.ServerName.Should().BeNull();
    }

    [Fact]
    public void Scrub_AnEmailInAMessageAnExceptionOrAnExtra_MasksIt()
    {
        // Arrange
        var sentryEvent = new SentryEvent
        {
            Message = new SentryMessage { Message = "Failed for thorin@example.com", Formatted = "Failed for thorin@example.com" },
            SentryExceptions = [new SentryException { Value = "No account for (thorin@example.com)." }]
        };
        sentryEvent.SetExtra("stack_trace", "at A.B() for balin@erebor.example.org");
        sentryEvent.SetExtra("count", 3);
        sentryEvent.SetTag("client_version", "1.0 thorin@example.com");
        sentryEvent.SetFingerprint(["client", "at A.B() for thorin@example.com"]);
        sentryEvent.Request.Url = "/characters/thorin@example.com/sheet";

        // Act
        var scrubbed = ErrorScrubber.Scrub(sentryEvent);

        // Assert
        scrubbed.Message!.Message.Should().Be($"Failed for {ErrorScrubber.MaskedEmail}");
        scrubbed.Message.Formatted.Should().Be($"Failed for {ErrorScrubber.MaskedEmail}");
        scrubbed.SentryExceptions!.Single().Value.Should().Be($"No account for ({ErrorScrubber.MaskedEmail}).");
        scrubbed.Extra["stack_trace"].Should().Be($"at A.B() for {ErrorScrubber.MaskedEmail}");
        scrubbed.Extra["count"].Should().Be(3);
        scrubbed.Tags["client_version"].Should().Be($"1.0 {ErrorScrubber.MaskedEmail}");
        scrubbed.Fingerprint.Should().Equal("client", $"at A.B() for {ErrorScrubber.MaskedEmail}");
        scrubbed.Request.Url.Should().Be($"/characters/{ErrorScrubber.MaskedEmail}/sheet");
    }

    [Fact]
    public void Scrub_AnEventWithoutARequestOrMessage_StaysValid()
    {
        // Arrange
        var sentryEvent = new SentryEvent();

        // Act
        var scrubbed = ErrorScrubber.Scrub(sentryEvent);

        // Assert
        scrubbed.Request.Url.Should().BeNull();
        scrubbed.Message.Should().BeNull();
    }
}
