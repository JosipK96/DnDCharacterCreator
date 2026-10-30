using System.Net;
using System.Net.Http.Json;

using CharacterCreatorApp.Client.Services.Http;

using FluentAssertions;

namespace CharacterCreatorApp.Client.UnitTests.Services;

public class HttpResponseMessageExtensionsTests
{
    [Fact]
    public async Task GetFailureAsync_ForATooManyRequestsProblem_ShowsTheApisOwnMessage()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = JsonContent.Create(new
            {
                status = 429,
                detail = "Too many attempts. Please wait a minute and try again.",
                errors = new[] { new { code = "RateLimit.TooManyRequests", detail = "Too many attempts. Please wait a minute and try again." } },
            }),
        };

        // Act
        var failure = await response.GetFailureAsync();

        // Assert
        failure.Should().NotBeNull();
        failure!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        failure.Message.Should().Be("Too many attempts. Please wait a minute and try again.");
        failure.HasCode("RateLimit.TooManyRequests").Should().BeTrue();
    }

    [Fact]
    public async Task GetFailureAsync_ForATooManyRequestsWithNoBody_StillSaysToWait()
    {
        // Arrange - a proxy in front of the API may answer without the problem body
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);

        // Act
        var failure = await response.GetFailureAsync();

        // Assert
        failure!.Message.Should().Be("Too many attempts. Please wait a minute and try again.");
    }
}
