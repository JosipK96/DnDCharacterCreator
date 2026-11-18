using CharacterCreatorApp.Client.Services.ErrorReporting;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Requests.ClientError;

using FluentAssertions;

using Microsoft.Extensions.Logging;

namespace CharacterCreatorApp.Client.UnitTests.Services;

/// <summary>Blazor logs an unhandled error; the provider sends the safe parts of it to the API (#77).</summary>
public sealed class ClientErrorLoggerProviderTests : IDisposable
{
    private const string Route = "POST /api/client-errors";

    private readonly FakeApi _api = new();

    public ClientErrorLoggerProviderTests() => _api.Post("/api/client-errors");

    [Fact]
    public async Task Log_AnErrorWithAnException_SendsItsTypeStackPathAndVersionButNotItsMessage()
    {
        // Arrange
        using var provider = Provider("1.2.3");
        var logger = provider.CreateLogger("Microsoft.AspNetCore.Components.WebAssembly.Rendering.WebAssemblyRenderer");
        var exception = Thrown("thorin@example.com typed this");

        // Act
        logger.LogError(exception, "Unhandled exception rendering component");
        await WaitForAsync(1);

        // Assert
        var body = _api.BodyOf<ReportClientErrorRequest>(Route);
        body.ExceptionType.Should().Be(typeof(InvalidOperationException).FullName);
        body.StackTrace.Should().Be(exception.StackTrace);
        body.Path.Should().Be("/characters/new");
        body.Version.Should().Be("1.2.3");
        (body.ExceptionType + body.StackTrace + body.Path + body.Version).Should().NotContain("thorin@example.com");
    }

    [Theory]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Information)]
    public void Log_BelowError_SendsNothing(LogLevel level)
    {
        // Arrange
        using var provider = Provider();

        // Act
        provider.CreateLogger("Any").Log(level, Thrown("x"), "message");

        // Assert
        _api.Requests.Should().BeEmpty();
    }

    [Fact]
    public void Log_AnErrorWithoutAnException_SendsNothing()
    {
        // Arrange
        using var provider = Provider();

        // Act
        provider.CreateLogger("Any").LogError("Something failed");

        // Assert
        _api.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Log_MoreErrorsThanTheLimit_SendsOnlyTheFirstOnes()
    {
        // Arrange
        using var provider = Provider();
        var logger = provider.CreateLogger("Any");

        // Act
        for (var i = 0; i < ClientErrorLoggerProvider.MaxReports + 3; i++)
        {
            logger.LogError(Thrown("loop"), "again");
        }

        await WaitForAsync(ClientErrorLoggerProvider.MaxReports);

        // Assert
        _api.Requests.Should().HaveCount(ClientErrorLoggerProvider.MaxReports);
    }

    [Fact]
    public async Task Log_WhenTheApiCannotBeReached_DoesNotThrow()
    {
        // Arrange - no route is set up, so the fake API throws like an unreachable host
        using var provider = new ClientErrorLoggerProvider(new FakeApi().CreateClient(), () => "/", "1.0.0");

        // Act
        var act = () => provider.CreateLogger("Any").LogError(Thrown("x"), "boom");
        act.Should().NotThrow();
        await Task.Delay(50, TestContext.Current.CancellationToken);
    }

    public void Dispose() => _api.Dispose();

    private ClientErrorLoggerProvider Provider(string version = "0.0.0-dev") =>
        new(_api.CreateClient(), () => "/characters/new", version);

    private static InvalidOperationException Thrown(string message)
    {
        try
        {
            throw new InvalidOperationException(message);
        }
        catch (InvalidOperationException exception)
        {
            return exception;
        }
    }

    // The report is sent without being awaited, so give it a moment to arrive
    private async Task WaitForAsync(int requests)
    {
        for (var i = 0; i < 100 && _api.Requests.Count < requests; i++)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }
    }
}
