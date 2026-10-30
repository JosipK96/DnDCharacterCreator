using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using CharacterCreatorApp.Api.Infrastructure.Admin;
using CharacterCreatorApp.Api.IntegrationTests.Fixtures;
using CharacterCreatorApp.Contracts.Requests.User;
using CharacterCreatorApp.Contracts.Responses;

using FluentAssertions;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CharacterCreatorApp.Api.IntegrationTests.Controllers;

/// <summary>An owner-issued reset link, using it, and a signed-in user changing their password.</summary>
[Collection(SharedApiHost.Name)]
public class PasswordRecoveryTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    private const string Password = "Test123!@#";
    private const string NewPassword = "Brand9New";

    private readonly CustomWebApplicationFactory _factory = factory;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    [Fact]
    public async Task CreateLink_ByTheOwner_ReturnsALinkThatResetsThatPlayersPassword()
    {
        // Arrange
        var owner = await NewUserAsync();
        var player = await NewUserAsync();
        await using var host = WithOwner(owner.Email);
        using var client = host.CreateClient();
        await SignInAsync(client, owner);

        // Act
        var created = await client.PostAsJsonAsync("/api/admin/password-reset-links", new CreatePasswordResetLinkRequest(player.Email), TestContext.Current.CancellationToken);

        // Assert
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var link = new Uri((await created.Content.ReadFromJsonAsync<PasswordResetLinkApiResponse>(TestContext.Current.CancellationToken))!.Link);
        link.AbsolutePath.Should().Be("/reset-password");

        var query = System.Web.HttpUtility.ParseQueryString(link.Query);
        query["email"].Should().Be(player.Email);

        var reset = await ResetAsync(client, query["email"]!, query["token"]!, NewPassword);
        reset.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await LoginAsync(client, player.Email, NewPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await LoginAsync(client, player.Email, Password)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateLink_BySomeoneWhoIsNotTheOwner_ReturnsForbiddenAndNoToken()
    {
        // Arrange
        var owner = await NewUserAsync();
        var other = await NewUserAsync();
        var player = await NewUserAsync();
        await using var host = WithOwner(owner.Email);
        using var client = host.CreateClient();
        await SignInAsync(client, other);

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/password-reset-links", new CreatePasswordResetLinkRequest(player.Email), TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.Forbidden, "User.NotAnAdmin");
    }

    [Fact]
    public async Task CreateLink_WithNoOwnerConfigured_ReturnsForbiddenForEveryone()
    {
        // Arrange - the shared host sets no Admin__Email
        var user = await NewUserAsync();
        using var client = _factory.CreateClient();
        await SignInAsync(client, user);

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/password-reset-links", new CreatePasswordResetLinkRequest(user.Email), TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.Forbidden, "User.NotAnAdmin");
    }

    [Fact]
    public async Task CreateLink_WithoutSigningIn_ReturnsUnauthorized()
    {
        // Arrange
        var player = await NewUserAsync();
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/password-reset-links", new CreatePasswordResetLinkRequest(player.Email), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateLink_ForAnEmailWithNoAccount_ReturnsNotFound()
    {
        // Arrange
        var owner = await NewUserAsync();
        await using var host = WithOwner(owner.Email);
        using var client = host.CreateClient();
        await SignInAsync(client, owner);

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/password-reset-links", new CreatePasswordResetLinkRequest("nobody@example.com"), TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.NotFound, "User.NotFound");
    }

    [Fact]
    public async Task ResetPassword_WithALinkAlreadyUsed_ReturnsInvalidResetLink()
    {
        // Arrange
        var player = await NewUserAsync();
        var token = await TokenForAsync(player.Email);
        using var client = _factory.CreateClient();
        (await ResetAsync(client, player.Email, token, NewPassword)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act
        var response = await ResetAsync(client, player.Email, token, "Another9Pass");

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "User.InvalidResetLink");
        (await LoginAsync(client, player.Email, NewPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_WithAnExpiredLink_ReturnsInvalidResetLink()
    {
        // Arrange - a host whose links are born already past their end
        var player = await NewUserAsync();
        await using var host = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromMinutes(-1))));
        using var client = host.CreateClient();
        var token = await TokenForAsync(player.Email, host);

        // Act
        var response = await ResetAsync(client, player.Email, token, NewPassword);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "User.InvalidResetLink");
        (await LoginAsync(client, player.Email, Password)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_WithATokenThatIsNotTheirs_AndForAnUnknownEmail_GiveTheSameAnswer()
    {
        // Arrange
        var player = await NewUserAsync();
        var other = await NewUserAsync();
        var othersToken = await TokenForAsync(other.Email);
        using var client = _factory.CreateClient();

        // Act
        var wrongToken = await ResetAsync(client, player.Email, othersToken, NewPassword);
        var unknownEmail = await ResetAsync(client, "nobody@example.com", othersToken, NewPassword);

        // Assert
        var expected = new ProblemResponseError("User.InvalidResetLink", null, "This reset link has been used, has expired, or is not valid. Ask for a new one");
        foreach (var response in new[] { wrongToken, unknownEmail })
        {
            var problem = await response.ReadProblemAsync();
            problem.Status.Should().Be((int)HttpStatusCode.BadRequest);
            problem.Errors.Should().ContainSingle().Which.Should().Be(expected);
        }
    }

    [Fact]
    public async Task ResetPassword_WithAPasswordTheRulesRefuse_ReturnsTheReasonAndLeavesThePasswordAlone()
    {
        // Arrange
        var player = await NewUserAsync();
        var token = await TokenForAsync(player.Email);
        using var client = _factory.CreateClient();

        // Act
        var response = await ResetAsync(client, player.Email, token, "abc");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Errors.Select(e => e.Code).Should().Contain(["User.PasswordTooShort", "User.PasswordRequiresUpper", "User.PasswordRequiresDigit"]);
        (await LoginAsync(client, player.Email, Password)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_ForALockedOutPlayer_LetsThemSignInWithTheNewPassword()
    {
        // Arrange
        var player = await NewUserAsync();
        using var client = _factory.CreateClient();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await LoginAsync(client, player.Email, "WrongPassword123!@#");
        }

        var token = await TokenForAsync(player.Email);

        // Act
        await ResetAsync(client, player.Email, token, NewPassword);

        // Assert
        (await LoginAsync(client, player.Email, NewPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WithTheRightCurrentPassword_SetsTheNewOne()
    {
        // Arrange
        var user = await NewUserAsync();
        using var client = _factory.CreateClient();
        await SignInAsync(client, user);

        // Act
        var response = await ChangeAsync(client, Password, NewPassword);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await LoginAsync(client, user.Email, NewPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await LoginAsync(client, user.Email, Password)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WithAWrongCurrentPassword_ReturnsWrongCurrentPasswordAndChangesNothing()
    {
        // Arrange
        var user = await NewUserAsync();
        using var client = _factory.CreateClient();
        await SignInAsync(client, user);

        // Act
        var response = await ChangeAsync(client, "WrongPassword123!@#", NewPassword);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "User.WrongCurrentPassword");
        (await LoginAsync(client, user.Email, Password)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_AfterFiveWrongCurrentPasswords_RefusesEvenTheRightOne()
    {
        // Arrange
        var user = await NewUserAsync();
        using var client = _factory.CreateClient();
        await SignInAsync(client, user);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await ChangeAsync(client, "WrongPassword123!@#", NewPassword);
        }

        // Act
        var response = await ChangeAsync(client, Password, NewPassword);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "User.WrongCurrentPassword");
    }

    [Fact]
    public async Task ChangePassword_WithANewPasswordTheRulesRefuse_ReturnsTheReason()
    {
        // Arrange
        var user = await NewUserAsync();
        using var client = _factory.CreateClient();
        await SignInAsync(client, user);

        // Act
        var response = await ChangeAsync(client, Password, "abc");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Errors.Select(e => e.Code).Should().Contain("User.PasswordTooShort");
    }

    [Fact]
    public async Task ChangePassword_WithoutSigningIn_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await ChangeAsync(client, Password, NewPassword);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResetPassword_PastThePerAddressLimit_ReturnsTooManyRequests()
    {
        // Arrange
        await using var host = _factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimiting:PermitLimit"] = "2" })));
        using var client = host.CreateClient();
        await ResetAsync(client, "nobody@example.com", "token", NewPassword);
        await ResetAsync(client, "nobody@example.com", "token", NewPassword);

        // Act
        var response = await ResetAsync(client, "nobody@example.com", "token", NewPassword);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.TooManyRequests, "RateLimit.TooManyRequests");
    }

    // A second host over the same database, with the setting a host gets from Admin__Email
    private WebApplicationFactory<Program> WithOwner(string email) =>
        _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddSingleton(Options.Create(new AdminSettings { Email = email }))));

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/users/login", new LoginUserRequest(email, password), TestContext.Current.CancellationToken);

    private static Task<HttpResponseMessage> ResetAsync(HttpClient client, string email, string token, string newPassword) =>
        client.PostAsJsonAsync("/api/users/reset-password", new ResetPasswordRequest(email, token, newPassword), TestContext.Current.CancellationToken);

    private static Task<HttpResponseMessage> ChangeAsync(HttpClient client, string current, string next) =>
        client.PostAsJsonAsync("/api/users/change-password", new ChangePasswordRequest(current, next), TestContext.Current.CancellationToken);

    private static async Task SignInAsync(HttpClient client, (string Email, string Password) user)
    {
        var response = await LoginAsync(client, user.Email, user.Password);
        var login = await response.Content.ReadFromJsonAsync<LoginApiResponse>(TestContext.Current.CancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
    }

    private async Task<(string Email, string Password)> NewUserAsync()
    {
        var email = $"recover-{Guid.NewGuid()}@example.com";
        using var client = _factory.CreateClient();
        var request = new RegisterUserRequest { Email = email, UserName = $"user{Guid.NewGuid():N}", Password = Password, DisplayName = "Test User" };
        (await client.PostAsJsonAsync("/api/users/register", request, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        return (email, Password);
    }

    // What the owner's endpoint hands out, taken straight from Identity so a test needs no owner
    private async Task<string> TokenForAsync(string email, WebApplicationFactory<Program>? host = null)
    {
        using var scope = (host ?? _factory).Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<CharacterCreatorApp.Infrastructure.Identity.ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);

        return await users.GeneratePasswordResetTokenAsync(user!);
    }
}
