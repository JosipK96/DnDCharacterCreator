using CharacterCreatorApp.Api.Infrastructure.ErrorReporting;
using CharacterCreatorApp.Contracts.Requests.ClientError;

using FluentAssertions;

namespace CharacterCreatorApp.Api.IntegrationTests.Infrastructure;

/// <summary>The event made from what a client posts; anyone can post, so every field is bounded.</summary>
public class ClientErrorReporterTests
{
    [Fact]
    public void CreateEvent_AClientReport_CarriesTheTypeStackPathAndVersion()
    {
        // Arrange
        var request = new ReportClientErrorRequest("System.InvalidOperationException", "at Step.Render()\nat Page.Run()", "/characters/new", "1.2.3");

        // Act
        var sentryEvent = ClientErrorReporter.CreateEvent(request);

        // Assert
        sentryEvent.Message!.Message.Should().Be("Client error: System.InvalidOperationException");
        sentryEvent.Level.Should().Be(SentryLevel.Error);
        sentryEvent.Request.Url.Should().Be("/characters/new");
        sentryEvent.Tags["source"].Should().Be("client");
        sentryEvent.Tags["client_version"].Should().Be("1.2.3");
        sentryEvent.Extra["stack_trace"].Should().Be("at Step.Render()\nat Page.Run()");
        sentryEvent.Fingerprint.Should().Equal("client", "System.InvalidOperationException", "at Step.Render()");
    }

    [Fact]
    public void CreateEvent_FieldsLongerThanTheLimits_AreCut()
    {
        // Arrange
        var request = new ReportClientErrorRequest(
            new string('t', 1000),
            new string('s', 50_000),
            new string('p', 1000),
            new string('v', 1000));

        // Act
        var sentryEvent = ClientErrorReporter.CreateEvent(request);

        // Assert
        ((string)sentryEvent.Extra["stack_trace"]!).Length.Should().Be(ClientErrorReporter.MaxStackLength);
        sentryEvent.Request.Url!.Length.Should().Be(ClientErrorReporter.MaxPathLength);
        sentryEvent.Tags["client_version"].Length.Should().Be(ClientErrorReporter.MaxVersionLength);
        sentryEvent.Message!.Message.Should().Be($"Client error: {new string('t', ClientErrorReporter.MaxTypeLength)}");
    }

    [Fact]
    public void CreateEvent_BlankFields_BecomeUnknown()
    {
        // Arrange
        var request = new ReportClientErrorRequest(" ", string.Empty, null!, "\t");

        // Act
        var sentryEvent = ClientErrorReporter.CreateEvent(request);

        // Assert
        sentryEvent.Message!.Message.Should().Be($"Client error: {ClientErrorReporter.Unknown}");
        sentryEvent.Request.Url.Should().Be(ClientErrorReporter.Unknown);
        sentryEvent.Tags["client_version"].Should().Be(ClientErrorReporter.Unknown);
    }
}
