using Bunit;

using CharacterCreatorApp.Client.Pages.Auth;
using CharacterCreatorApp.Client.Services;
using CharacterCreatorApp.Client.Services.Auth;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Requests.User;
using CharacterCreatorApp.Contracts.Responses.Users;

using FluentAssertions;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace CharacterCreatorApp.Client.UnitTests.Pages;

/// <summary>
/// Signing in and registering: what each page sends, where it goes after, and the API's reasons
/// for refusing shown to the player. Register's invite code is sent as typed.
/// </summary>
public class AuthPagesTests : WizardStepTestBase
{
    private const string LoginRoute = "POST /api/users/login";
    private const string RegisterRoute = "POST /api/users/register";

    public AuthPagesTests()
    {
        var authenticationState = new JwtAuthenticationStateProvider(new TokenStore(JSInterop.JSRuntime), TimeProvider.System);
        Services.AddSingleton(new AuthService(Api.CreateClient(), authenticationState));
    }

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    [Fact]
    public void Login_WithTheRightPassword_SendsItKeepsTheTokenAndGoesToTheCharacters()
    {
        // Arrange
        Api.Post("/api/users/login", new LoginApiResponse("the-token"));
        var keep = JSInterop.SetupVoid("localStorage.setItem", "authToken", "the-token");
        keep.SetVoidResult();
        var cut = Render<Login>();
        FillLogin(cut, "thorin@example.com", "Erebor123");

        // Act
        cut.Find("form").Submit();

        // Assert
        Api.BodyOf<LoginUserRequest>(LoginRoute).Should().Be(new LoginUserRequest("thorin@example.com", "Erebor123"));
        keep.VerifyInvoke("localStorage.setItem");
        Navigation.Uri.Should().Be("http://localhost/characters");
    }

    [Theory]
    [InlineData("/character/create", "http://localhost/character/create")]
    [InlineData("https://elsewhere.example/steal", "http://localhost/characters")]
    public void Login_WithAReturnUrl_FollowsOnlyARelativeOne(string returnUrl, string expected)
    {
        // Arrange - RedirectToLogin sends a player who was not signed in here with where they were going
        Api.Post("/api/users/login", new LoginApiResponse("the-token"));
        JSInterop.SetupVoid("localStorage.setItem", "authToken", "the-token").SetVoidResult();
        Navigation.NavigateTo($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        var cut = Render<Login>();
        FillLogin(cut, "thorin@example.com", "Erebor123");

        // Act
        cut.Find("form").Submit();

        // Assert
        Navigation.Uri.Should().Be(expected);
    }

    [Fact]
    public void Login_WhenTheApiRefuses_ShowsItsReasonAndStays()
    {
        // Arrange
        Api.RefusePost("/api/users/login", "User.InvalidCredentials", "The email or password is wrong");
        var cut = Render<Login>();
        FillLogin(cut, "thorin@example.com", "wrong");

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.Find("[role=alert]").TextContent.Should().Contain("The email or password is wrong");
        Navigation.Uri.Should().Be("http://localhost/");
    }

    [Fact]
    public void Login_WithNoEmail_SendsNothing()
    {
        // Arrange
        var cut = Render<Login>();
        FillLogin(cut, string.Empty, "Erebor123");

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.Markup.Should().Contain("Email is required");
        Api.Requests.Should().BeEmpty();
    }

    [Fact]
    public void Login_AfterRegistering_SaysTheAccountIsMade()
    {
        // Arrange
        Navigation.NavigateTo("/login?registered=true");

        // Act
        var cut = Render<Login>();

        // Assert
        cut.Find("[role=status]").TextContent.Should().Contain("Account created successfully!");
    }

    [Fact]
    public void Register_WithEveryField_SendsThemWithTheInviteCodeAndGoesToSignIn()
    {
        // Arrange
        Api.Post("/api/users/register");
        var cut = Render<Register>();
        FillRegister(cut, password: "Erebor123", confirm: "Erebor123");
        cut.Find("#displayName").Change("Thorin");
        cut.Find("#inviteCode").Change("friends-only");

        // Act
        cut.Find("form").Submit();

        // Assert
        var sent = Api.BodyOf<RegisterUserRequest>(RegisterRoute);
        sent.Should().BeEquivalentTo(new RegisterUserRequest
        {
            UserName = "thorin",
            Email = "thorin@example.com",
            Password = "Erebor123",
            DisplayName = "Thorin",
            InviteCode = "friends-only",
        });
        Navigation.Uri.Should().Be("http://localhost/login?registered=true");
    }

    [Fact]
    public void Register_WhenTheApiRefusesTheInviteCode_ShowsItsReason()
    {
        // Arrange
        Api.RefusePost("/api/users/register", "User.InvalidInviteCode", "The invite code is not right");
        var cut = Render<Register>();
        FillRegister(cut, password: "Erebor123", confirm: "Erebor123");
        cut.Find("#inviteCode").Change("guess");

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.Find("[role=alert]").TextContent.Should().Contain("The invite code is not right");
        cut.FindAll("[role=alert] li").Should().BeEmpty();
        Navigation.Uri.Should().Be("http://localhost/");
    }

    [Fact]
    public void Register_WhenTheApiGivesSeveralReasons_ListsEachOne()
    {
        // Arrange
        Api.RefusePost("/api/users/register", ("User.DuplicateUserName", "That username is taken"), ("User.DuplicateEmail", "That email is taken"));
        var cut = Render<Register>();
        FillRegister(cut, password: "Erebor123", confirm: "Erebor123");

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.FindAll("[role=alert] li").Select(li => li.TextContent).Should().Equal("That username is taken", "That email is taken");
    }

    [Fact]
    public void Register_WithPasswordsThatDiffer_SendsNothing()
    {
        // Arrange
        var cut = Render<Register>();
        FillRegister(cut, password: "Erebor123", confirm: "Erebor124");

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.Markup.Should().Contain("Passwords do not match");
        Api.Requests.Should().BeEmpty();
    }

    private static void FillLogin(IRenderedComponent<Login> cut, string email, string password)
    {
        cut.Find("#email").Change(email);
        cut.Find("#password").Change(password);
    }

    private static void FillRegister(IRenderedComponent<Register> cut, string password, string confirm)
    {
        cut.Find("#username").Change("thorin");
        cut.Find("#email").Change("thorin@example.com");
        cut.Find("#password").Change(password);
        cut.Find("#confirmPassword").Change(confirm);
    }
}
