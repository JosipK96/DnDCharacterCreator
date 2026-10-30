using System.Net;
using System.Net.Http.Json;

using CharacterCreatorApp.Api.IntegrationTests.Fixtures;
using CharacterCreatorApp.Contracts.Common;

using FluentAssertions;

namespace CharacterCreatorApp.Api.IntegrationTests.Controllers;

/// <summary>What Render's health check and the owner see at /health.</summary>
[Collection(SharedApiHost.Name)]
public class HealthTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private sealed record HealthBody(string Status, string Version);

    [Fact]
    public async Task Get_ReportsHealthyAndTheVersionThisBuildWasMadeAs()
    {
        // Act
        var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthBody>(TestContext.Current.CancellationToken);
        body!.Status.Should().Be("Healthy");
        body.Version.Should().StartWith(BuildVersion.Unversioned);
    }
}
