using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using CharacterCreatorApp.Api.Infrastructure.Registration;
using CharacterCreatorApp.Api.IntegrationTests.Fixtures;
using CharacterCreatorApp.Contracts.Requests.User;
using CharacterCreatorApp.Contracts.Responses;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CharacterCreatorApp.Api.IntegrationTests.Controllers;

[Collection(SharedApiHost.Name)]
public class UsersControllerTests : IAsyncLifetime
{
    private const string InviteCode = "speak-friend-and-enter";

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
        // Arrange - Identity is configured to want six characters, an uppercase letter and a digit
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
    public async Task Login_WithNonExistentEmail_ReturnsTheErrorCodeAndMessage()
    {
        // Arrange
        var loginRequest = new LoginUserRequest($"nobody-{Guid.NewGuid()}@example.com", "Test123!@#");

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/login", loginRequest, TestContext.Current.CancellationToken);

        // Assert - the client shows Detail, so it must be the message and not the code
        var problem = await response.ReadProblemAsync();
        problem.Status.Should().Be((int)HttpStatusCode.NotFound);
        problem.Detail.Should().Be("User is not found by email");
        problem.Errors.Should().ContainSingle()
            .Which.Should().Be(new ProblemResponseError("User.NotFoundByEmail", null, "User is not found by email"));
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
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var email = $"invalid-{Guid.NewGuid()}@example.com";
        await RegisterUserAsync(email, "CorrectPassword123!@#");

        var loginRequest = new LoginUserRequest(email, "WrongPassword123!@#");

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/login", loginRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsNotFound()
    {
        // Arrange
        var loginRequest = new LoginUserRequest($"nonexistent-{Guid.NewGuid()}@example.com", "Test123!@#");

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/login", loginRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
}