using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using CharacterCreatorApp.Api.Infrastructure.Registration;
using CharacterCreatorApp.Api.IntegrationTests.Fixtures;
using CharacterCreatorApp.Contracts.Requests.User;
using CharacterCreatorApp.Contracts.Responses;

using FluentAssertions;

using CharacterCreatorApp.Infrastructure.Identity;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CharacterCreatorApp.Api.IntegrationTests.Controllers;

[Collection(SharedApiHost.Name)]
public class UsersControllerTests : IAsyncLifetime
{
    private const string InviteCode = "speak-friend-and-enter";
    private const string Password = "Test123!@#";
    private const string ClientAddressHeader = "X-Test-Client-Address";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UsersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsOk()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Email = $"test-{Guid.NewGuid()}@example.com",
            UserName = $"testuser{Guid.NewGuid():N}",
            Password = "Test123!@#",
            DisplayName = "Test User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<bool>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var email = $"duplicate-{Guid.NewGuid()}@example.com";
        var request1 = new RegisterUserRequest
        {
            Email = email,
            UserName = $"user1{Guid.NewGuid():N}",
            Password = "Test123!@#",
            DisplayName = "User 1"
        };
        var request2 = new RegisterUserRequest
        {
            Email = email, // Same email
            UserName = $"user2{Guid.NewGuid():N}",
            Password = "Test123!@#",
            DisplayName = "User 2"
        };

        // Act
        await _client.PostAsJsonAsync("/api/users/register", request1, TestContext.Current.CancellationToken);
        var response = await _client.PostAsJsonAsync("/api/users/register", request2, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithAPasswordIdentityRejects_ReturnsBadRequestWithTheReasons()
    {
        // Arrange - Identity is configured to want eight characters, an uppercase letter and a digit
        var request = new RegisterUserRequest
        {
            Email = $"weak-{Guid.NewGuid()}@example.com",
            UserName = $"weak{Guid.NewGuid():N}",
            Password = "abc",
            DisplayName = "Weak Password"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Errors.Select(e => e.Code).Should().Contain(
            ["User.PasswordTooShort", "User.PasswordRequiresUpper", "User.PasswordRequiresDigit"]);
    }

    [Fact]
    public async Task Register_WithASevenCharacterPassword_ReturnsBadRequestForTheLength()
    {
        // Arrange - otherwise a good password
        var request = NewRegistration(null);
        request.Password = "Abcde12";

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Errors.Should().ContainSingle().Which.Code.Should().Be("User.PasswordTooShort");
    }

    [Fact]
    public async Task Register_WithAnEightCharacterPassword_ReturnsOk()
    {
        // Arrange
        var request = NewRegistration(null);
        request.Password = "Abcdef12";

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("speak-friend")]
    public async Task Register_WithAnInviteCodeSetAndAMissingOrWrongOne_ReturnsForbidden(string? inviteCode)
    {
        // Arrange
        await using var host = WithInviteCode(InviteCode);
        using var client = host.CreateClient();
        var request = NewRegistration(inviteCode);

        // Act
        var response = await client.PostAsJsonAsync("/api/users/register", request, TestContext.Current.CancellationToken);

        // Assert
        var problem = await response.ReadProblemAsync();
        problem.Status.Should().Be((int)HttpStatusCode.Forbidden);
        problem.Errors.Should().ContainSingle()
            .Which.Should().Be(new ProblemResponseError("User.InvalidInviteCode", null, "The invite code is missing or wrong"));
    }

    [Fact]
    public async Task Register_WithAnInviteCodeSetAndTheRightOne_ReturnsOk()
    {
        // Arrange
        await using var host = WithInviteCode(InviteCode);
        using var client = host.CreateClient();
        var request = NewRegistration(InviteCode);

        // Act
        var response = await client.PostAsJsonAsync("/api/users/register", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithNoInviteCodeSetAndAnyCodeGiven_ReturnsOk()
    {
        // Arrange - the shared host has no invite code, like local development
        var request = NewRegistration("anything");

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var email = $"login-{Guid.NewGuid()}@example.com";
        var password = "Test123!@#";

        await RegisterUserAsync(email, password);

        var loginRequest = new LoginUserRequest(email, password);

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/login", loginRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginApiResponse>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithAWrongPasswordOrAnUnknownEmail_GivesTheSameAnswer()
    {
        // Arrange
        var email = $"invalid-{Guid.NewGuid()}@example.com";
        await RegisterUserAsync(email, "CorrectPassword123!@#");

        // Act
        var wrongPassword = await LoginAsync(_client, email, "WrongPassword123!@#");
        var unknownEmail = await LoginAsync(_client, $"nobody-{Guid.NewGuid()}@example.com", "Test123!@#");

        // Assert - the client shows Detail, so it must be the message and not the code
        var expected = new ProblemResponseError("User.InvalidCredentials", null, "Wrong email or password");
        foreach (var response in new[] { wrongPassword, unknownEmail })
        {
            var problem = await response.ReadProblemAsync();
            problem.Status.Should().Be((int)HttpStatusCode.Unauthorized);
            problem.Detail.Should().Be("Wrong email or password");
            problem.Errors.Should().ContainSingle().Which.Should().Be(expected);
        }
    }

    [Fact]
    public async Task Login_AfterFiveWrongPasswords_RefusesEvenTheRightPasswordWithTheSameAnswer()
    {
        // Arrange
        var email = $"locked-{Guid.NewGuid()}@example.com";
        await RegisterUserAsync(email, Password);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await LoginAsync(_client, email, "WrongPassword123!@#");
        }

        // Act
        var response = await LoginAsync(_client, email, Password);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.Unauthorized, "User.InvalidCredentials");
    }

    [Fact]
    public async Task Login_BeforeTheLockoutLimit_StillAcceptsTheRightPasswordAndStartsCountingAgain()
    {
        // Arrange - four failures, a success that resets the count, then four more
        var email = $"almost-{Guid.NewGuid()}@example.com";
        await RegisterUserAsync(email, Password);

        for (var attempt = 0; attempt < 4; attempt++)
        {
            await LoginAsync(_client, email, "WrongPassword123!@#");
        }

        var signedIn = await LoginAsync(_client, email, Password);

        for (var attempt = 0; attempt < 4; attempt++)
        {
            await LoginAsync(_client, email, "WrongPassword123!@#");
        }

        // Act
        var response = await LoginAsync(_client, email, Password);

        // Assert
        signedIn.StatusCode.Should().Be(HttpStatusCode.OK);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_OnceTheLockoutHasPassed_AcceptsTheRightPasswordAgain()
    {
        // Arrange
        var email = $"unlock-{Guid.NewGuid()}@example.com";
        await RegisterUserAsync(email, Password);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await LoginAsync(_client, email, "WrongPassword123!@#");
        }

        var whileLocked = await LoginAsync(_client, email, Password);

        // Identity reads the real clock, so the lockout is ended by moving its end into the past
        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync(email);
            await users.SetLockoutEndDateAsync(user!, DateTimeOffset.UtcNow.AddMinutes(-1));
        }

        // Act
        var response = await LoginAsync(_client, email, Password);

        // Assert
        whileLocked.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_PastThePerAddressLimit_ReturnsTooManyRequestsEvenWithTheRightPassword()
    {
        // Arrange
        var email = $"limited-{Guid.NewGuid()}@example.com";
        await RegisterUserAsync(email, Password);
        await using var host = WithRateLimit(permitLimit: 3);
        using var client = host.CreateClient();

        for (var attempt = 0; attempt < 3; attempt++)
        {
            (await LoginAsync(client, $"nobody-{attempt}@example.com", "Test123!@#")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // Act
        var response = await LoginAsync(client, email, Password);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.TooManyRequests, "RateLimit.TooManyRequests");
        response.Headers.RetryAfter.Should().NotBeNull();
    }

    [Fact]
    public async Task Register_PastThePerAddressLimit_ReturnsTooManyRequestsAndWrongInviteCodesCount()
    {
        // Arrange
        await using var host = WithRateLimit(permitLimit: 3, inviteCode: InviteCode);
        using var client = host.CreateClient();

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var guess = await client.PostAsJsonAsync("/api/users/register", NewRegistration($"guess-{attempt}"), TestContext.Current.CancellationToken);
            await guess.ShouldFailWithAsync(HttpStatusCode.Forbidden, "User.InvalidInviteCode");
        }

        // Act - the right code, but the address has used its allowance
        var response = await client.PostAsJsonAsync("/api/users/register", NewRegistration(InviteCode), TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.TooManyRequests, "RateLimit.TooManyRequests");
    }

    [Fact]
    public async Task Login_AndRegister_ShareOneAllowanceForAnAddress()
    {
        // Arrange
        await using var host = WithRateLimit(permitLimit: 2);
        using var client = host.CreateClient();
        await client.PostAsJsonAsync("/api/users/register", NewRegistration(null), TestContext.Current.CancellationToken);
        await LoginAsync(client, "nobody@example.com", "Test123!@#");

        // Act
        var response = await LoginAsync(client, "nobody@example.com", "Test123!@#");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Login_FromTwoAddresses_LimitsEachOnItsOwn()
    {
        // Arrange - the address is the one the forwarded headers middleware leaves on the connection
        // (ASPNETCORE_FORWARDEDHEADERS_ENABLED on a host); the test host has none, so a filter sets it
        await using var host = WithRateLimit(permitLimit: 1, clientAddressHeader: true);
        using var client = host.CreateClient();

        // Act
        var firstOfA = await client.SendAsync(LoginFrom("198.51.100.1"), TestContext.Current.CancellationToken);
        var firstOfB = await client.SendAsync(LoginFrom("198.51.100.2"), TestContext.Current.CancellationToken);
        var secondOfA = await client.SendAsync(LoginFrom("198.51.100.1"), TestContext.Current.CancellationToken);

        // Assert
        firstOfA.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        firstOfB.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        secondOfA.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Act - Try to access protected endpoint without token
        var response = await _client.PostAsync("/api/character-drafts", null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync("/api/character-drafts", null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.token.here");

        // Act
        var response = await _client.PostAsync("/api/character-drafts", null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Helper methods

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/users/login", new LoginUserRequest(email, password), TestContext.Current.CancellationToken);

    private static HttpRequestMessage LoginFrom(string address)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/login")
        {
            Content = JsonContent.Create(new LoginUserRequest("nobody@example.com", Password))
        };
        request.Headers.Add(ClientAddressHeader, address);

        return request;
    }

    // A second host over the same database, with the limits a host gets from RateLimiting__PermitLimit
    // and Registration__InviteCode
    private WebApplicationFactory<Program> WithRateLimit(int permitLimit, string? inviteCode = null, bool clientAddressHeader = false) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = permitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }));
            builder.ConfigureTestServices(services =>
            {
                if (clientAddressHeader)
                {
                    services.AddTransient<IStartupFilter, ClientAddressFromHeaderFilter>();
                }

                if (inviteCode is not null)
                {
                    services.AddSingleton(Options.Create(new RegistrationSettings { InviteCode = inviteCode }));
                }
            });
        });
    // A second host over the same database, with the setting a host gets from Registration__InviteCode
    private WebApplicationFactory<Program> WithInviteCode(string inviteCode) =>
        _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddSingleton(Options.Create(new RegistrationSettings { InviteCode = inviteCode }))));

    private static RegisterUserRequest NewRegistration(string? inviteCode) => new()
    {
        Email = $"invite-{Guid.NewGuid()}@example.com",
        UserName = $"invite{Guid.NewGuid():N}",
        Password = "Test123!@#",
        DisplayName = "Invited User",
        InviteCode = inviteCode
    };

    private async Task RegisterUserAsync(string email, string password)
    {
        var request = new RegisterUserRequest
        {
            Email = email,
            UserName = $"user{Guid.NewGuid():N}",
            Password = password,
            DisplayName = "Test User"
        };

        await _client.PostAsJsonAsync("/api/users/register", request, TestContext.Current.CancellationToken);
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var email = $"auth-{Guid.NewGuid()}@example.com";
        var password = "Test123!@#";

        await RegisterUserAsync(email, password);

        var loginRequest = new LoginUserRequest(email, password);

        var response = await _client.PostAsJsonAsync("/api/users/login", loginRequest, TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<LoginApiResponse>(cancellationToken: TestContext.Current.CancellationToken);

        return result!.Token;
    }

    // Stands in for the forwarded headers middleware, which a test host does not run
    private sealed class ClientAddressFromHeaderFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(ClientAddressHeader, out var address))
                {
                    context.Connection.RemoteIpAddress = IPAddress.Parse(address.ToString());
                }

                return nextMiddleware();
            });
            next(app);
        };
    }
}
