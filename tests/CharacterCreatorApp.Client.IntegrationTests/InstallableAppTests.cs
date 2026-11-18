using System.Text.Json;

using CharacterCreatorApp.Client.IntegrationTests.Fixtures;

using FluentAssertions;

namespace CharacterCreatorApp.Client.IntegrationTests;

/// <summary>What a phone needs to offer "Install app": the page links a manifest, and the Api serves it.</summary>
[Collection(WizardHostGroup.Name)]
public class InstallableAppTests(WizardHost host) : IDisposable
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri(host.Address) };

    public void Dispose() => _http.Dispose();

    [Fact]
    public async Task IndexHtml_LinksTheManifestAndTheHomeScreenIcon()
    {
        // Act
        var html = await _http.GetStringAsync("/", TestContext.Current.CancellationToken);

        // Assert
        html.Should().Contain("rel=\"manifest\" href=\"manifest.webmanifest\"");
        html.Should().Contain("rel=\"apple-touch-icon\" href=\"apple-touch-icon.png\"");
    }

    [Fact]
    public async Task Manifest_IsServedWithTheNameStartUrlStandaloneDisplayAndIcons()
    {
        // Act
        var response = await _http.GetAsync("/manifest.webmanifest", TestContext.Current.CancellationToken);
        var manifest = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        manifest.GetProperty("name").GetString().Should().Be("D&D Character Creator");
        manifest.GetProperty("start_url").GetString().Should().Be("/");
        manifest.GetProperty("display").GetString().Should().Be("standalone");

        var icons = manifest.GetProperty("icons").EnumerateArray().ToList();
        icons.Select(i => i.GetProperty("sizes").GetString()).Should().Contain(["192x192", "512x512"]);
        icons.Select(i => i.GetProperty("purpose").GetString()).Should().Contain("maskable");

        foreach (var icon in icons)
        {
            var iconResponse = await _http.GetAsync("/" + icon.GetProperty("src").GetString(), TestContext.Current.CancellationToken);
            iconResponse.IsSuccessStatusCode.Should().BeTrue();
        }
    }
}

