using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using CharacterCreatorApp.Api.Infrastructure.Admin;
using CharacterCreatorApp.Api.Infrastructure.ErrorReporting;
using CharacterCreatorApp.Api.Infrastructure.ExceptionHandling;
using CharacterCreatorApp.Api.IntegrationTests.Fixtures;
using CharacterCreatorApp.Contracts.Common;
using CharacterCreatorApp.Contracts.Requests.ClientError;
using CharacterCreatorApp.Contracts.Requests.User;
using CharacterCreatorApp.Contracts.Responses.Users;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Sentry.AspNetCore;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace CharacterCreatorApp.Api.IntegrationTests.Controllers;

/// <summary>
/// What reaches the error tracker (#77). The host gets a DSN and a transport that keeps what the
/// SDK would send, so these tests read the real events after the real scrubbing.
/// </summary>
[Collection(SharedApiHost.Name)]
public class ErrorReportingTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    private const string Password = "Test123!@#";

    private readonly CustomWebApplicationFactory _factory = factory;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    [Fact]
    public async Task TestError_ByTheOwner_AnswersAServerErrorAndSendsOneScrubbedEventWithVersionPathAndTraceId()
    {
        // Arrange
        var transport = new CapturingTransport();
        var owner = await NewUserAsync();
        await using var host = WithSentry(transport, owner.Email);
        using var client = host.CreateClient();
        await SignInAsync(client, owner);

        // Act
        var response = await client.PostAsync("/api/admin/test-error?note=thorin@example.com", content: null, TestContext.Current.CancellationToken);
        await host.Services.GetRequiredService<IHub>().FlushAsync(TimeSpan.FromSeconds(10));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var traceId = (await response.Content.ReadFromJsonAsync<TraceBody>(TestContext.Current.CancellationToken))!.TraceId;

        var sent = transport.Events.Should().ContainSingle().Subject;
        sent.GetProperty("release").GetString().Should().StartWith(BuildVersion.Unversioned);
        sent.GetProperty("request").GetProperty("url").GetString().Should().EndWith("/api/admin/test-error");
        sent.GetProperty("tags").GetProperty(TraceIdScopeMiddleware.TagName).GetString().Should().Be(traceId);
        sent.GetProperty("exception").GetRawText().Should().Contain("Deliberate test error");
        sent.GetProperty("request").TryGetProperty("query_string", out _).Should().BeFalse();
        sent.GetProperty("request").TryGetProperty("headers", out _).Should().BeFalse();
        sent.GetProperty("request").TryGetProperty("cookies", out _).Should().BeFalse();
        sent.TryGetProperty("user", out var user).Should().BeTrue("the SDK always writes the user, empty here");
        user.EnumerateObject().Should().BeEmpty();
        sent.TryGetProperty("breadcrumbs", out _).Should().BeFalse();
        sent.GetRawText().Should().NotContain(owner.Email).And.NotContain("Bearer ").And.NotContain("thorin@example.com");
    }

    [Fact]
    public async Task TestError_BySomeoneWhoIsNotTheOwner_IsRefusedAndSendsNothing()
    {
        // Arrange
        var transport = new CapturingTransport();
        var owner = await NewUserAsync();
        var player = await NewUserAsync();
        await using var host = WithSentry(transport, owner.Email);
        using var client = host.CreateClient();
        await SignInAsync(client, player);

        // Act
        var response = await client.PostAsync("/api/admin/test-error", content: null, TestContext.Current.CancellationToken);
        await host.Services.GetRequiredService<IHub>().FlushAsync(TimeSpan.FromSeconds(10));

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.Forbidden, "User.NotAnAdmin");
        transport.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task TestError_WithoutADsn_StillAnswersAServerError()
    {
        // Arrange - the shared host sets no Sentry__Dsn, so the SDK is off
        var owner = await NewUserAsync();
        await using var host = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddSingleton(Options.Create(new AdminSettings { Email = owner.Email }))));
        using var client = host.CreateClient();
        await SignInAsync(client, owner);

        // Act
        var response = await client.PostAsync("/api/admin/test-error", content: null, TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.InternalServerError, GlobalExceptionHandler.UnexpectedErrorCode);
    }

    [Fact]
    public async Task ClientErrors_AReport_IsAcceptedWithoutSigningInAndSentAsAScrubbedClientEvent()
    {
        // Arrange
        var transport = new CapturingTransport();
        await using var host = WithSentry(transport, ownerEmail: null);
        using var client = host.CreateClient();
        var report = new ReportClientErrorRequest("System.InvalidOperationException", "at Step.Render() for thorin@example.com", "/characters/new", "1.2.3");

        // Act
        var response = await client.PostAsJsonAsync("/api/client-errors", report, TestContext.Current.CancellationToken);
        await host.Services.GetRequiredService<IHub>().FlushAsync(TimeSpan.FromSeconds(10));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var sent = transport.Events.Should().ContainSingle().Subject;
        var tags = sent.GetProperty("tags");
        tags.GetProperty("source").GetString().Should().Be("client");
        tags.GetProperty("client_version").GetString().Should().Be("1.2.3");
        sent.GetProperty("request").GetProperty("url").GetString().Should().Be("/characters/new");
        sent.GetProperty("release").GetString().Should().StartWith(BuildVersion.Unversioned);
        sent.GetRawText().Should().NotContain("thorin@example.com").And.Contain("[email]");
    }

    [Fact]
    public async Task ClientErrors_PastThePerAddressLimit_ReturnsTooManyRequests()
    {
        // Arrange
        await using var host = _factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimiting:PermitLimit"] = "2" })));
        using var client = host.CreateClient();
        var report = new ReportClientErrorRequest("T", "s", "/", "1");
        await client.PostAsJsonAsync("/api/client-errors", report, TestContext.Current.CancellationToken);
        await client.PostAsJsonAsync("/api/client-errors", report, TestContext.Current.CancellationToken);

        // Act
        var response = await client.PostAsJsonAsync("/api/client-errors", report, TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.TooManyRequests, "RateLimit.TooManyRequests");
    }

    private WebApplicationFactory<Program> WithSentry(CapturingTransport transport, string? ownerEmail) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["Sentry:Dsn"] = "https://key@o0.ingest.sentry.io/1" }));
            builder.ConfigureTestServices(services =>
            {
                services.Configure<SentryAspNetCoreOptions>(options => options.Transport = transport);
                if (ownerEmail is not null)
                {
                    services.AddSingleton(Options.Create(new AdminSettings { Email = ownerEmail }));
                }
            });
        });

    private async Task<(string Email, string Password)> NewUserAsync()
    {
        var email = $"errors-{Guid.NewGuid()}@example.com";
        using var client = _factory.CreateClient();
        var request = new RegisterUserRequest { Email = email, UserName = $"user{Guid.NewGuid():N}", Password = Password, DisplayName = "Test User" };
        (await client.PostAsJsonAsync("/api/users/register", request, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        return (email, Password);
    }

    private static async Task SignInAsync(HttpClient client, (string Email, string Password) user)
    {
        var response = await client.PostAsJsonAsync("/api/users/login", new LoginUserRequest(user.Email, user.Password), TestContext.Current.CancellationToken);
        var login = await response.Content.ReadFromJsonAsync<LoginApiResponse>(TestContext.Current.CancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
    }

    private sealed record TraceBody(string TraceId);

    /// <summary>Stands in for the network: keeps the event JSON of each envelope the SDK would have sent.</summary>
    private sealed class CapturingTransport : ITransport
    {
        private readonly List<JsonElement> _events = [];

        public IReadOnlyList<JsonElement> Events => _events;

        public async Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream();
            await envelope.SerializeAsync(stream, null, cancellationToken);

            var text = Encoding.UTF8.GetString(stream.ToArray());
            foreach (var line in text.Split((char)10).Where(l => l.StartsWith('{') && l.Contains("\"platform\"")))
            {
                _events.Add(JsonDocument.Parse(line).RootElement.Clone());
            }
        }
    }
}
