using Bunit;

using CharacterCreatorApp.Client.Pages.Admin;
using CharacterCreatorApp.Client.Pages.Auth;
using CharacterCreatorApp.Client.Services;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Requests.User;

using FluentAssertions;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace CharacterCreatorApp.Client.UnitTests.Pages;

/// <summary>The three password pages: the reset link's, the account's, and the owner's link maker.</summary>
public class PasswordPagesTests : WizardStepTestBase
{
    private const string ResetRoute = "POST /api/users/reset-password";
    private const string ChangeRoute = "POST /api/users/change-password";
    private const string LinkRoute = "POST /api/admin/password-reset-links";

    public PasswordPagesTests()
    {
        Services.AddSingleton(new PasswordService(Api.CreateClient()));
        AddAuthorization().SetAuthorized("player");
    }

    [Fact]
    public void ResetPassword_WithMatchingPasswords_SendsTheLinksEmailAndTokenAndSaysItWorked()
    {
        // Arrange
        Api.Post("/api/users/reset-password");
        var cut = RenderReset("thorin@example.com", "tok+en/1");
        cut.Find("#new-password").Change("Moria4567");
        cut.Find("#confirm-password").Change("Moria4567");

        // Act
        cut.Find("form").Submit();

        // Assert
        Api.BodyOf<ResetPasswordRequest>(ResetRoute).Should().Be(new ResetPasswordRequest("thorin@example.com", "tok+en/1", "Moria4567"));
        cut.Find("[role=status]").TextContent.Should().Contain("Your password is changed");
    }

    [Fact]
    public void ResetPassword_WithPasswordsThatDiffer_SendsNothing()
    {
        // Arrange
        var cut = RenderReset("thorin@example.com", "token");
        cut.Find("#new-password").Change("Moria4567");
        cut.Find("#confirm-password").Change("Moria4568");

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.Find("[role=alert]").TextContent.Should().Contain("not the same");
        Api.Requests.Should().BeEmpty();
    }

    [Fact]
    public void ResetPassword_WithALinkMissingItsToken_SendsNothing()
    {
        // Arrange
        var cut = RenderReset("thorin@example.com", null);
        cut.Find("#new-password").Change("Moria4567");
        cut.Find("#confirm-password").Change("Moria4567");

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.Find("[role=alert]").TextContent.Should().Contain("incomplete");
        Api.Requests.Should().BeEmpty();
    }

    [Fact]
    public void ResetPassword_WhenTheApiRefusesTheLink_ShowsItsMessage()
    {
        // Arrange
        Api.RefusePost("/api/users/reset-password", "User.InvalidResetLink", "This reset link has been used, has expired, or is not valid. Ask for a new one");
        var cut = RenderReset("thorin@example.com", "old");
        cut.Find("#new-password").Change("Moria4567");
        cut.Find("#confirm-password").Change("Moria4567");

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.Find("[role=alert]").TextContent.Should().Contain("has been used");
        cut.FindAll("[role=status]").Should().BeEmpty();
    }

    [Fact]
    public void ChangePassword_WithMatchingNewPasswords_SendsBothAndSaysItWorked()
    {
        // Arrange
        Api.Post("/api/users/change-password");
        var cut = Render<ChangePassword>();
        cut.Find("#current-password").Change("Erebor123");
        cut.Find("#new-password").Change("Moria4567");
        cut.Find("#confirm-password").Change("Moria4567");

        // Act
        cut.Find("form").Submit();

        // Assert
        Api.BodyOf<ChangePasswordRequest>(ChangeRoute).Should().Be(new ChangePasswordRequest("Erebor123", "Moria4567"));
        cut.Find("[role=status]").TextContent.Should().Contain("Your password is changed");
    }

    [Fact]
    public void ChangePassword_WhenTheCurrentPasswordIsWrong_ShowsTheApisMessage()
    {
        // Arrange
        Api.RefusePost("/api/users/change-password", "User.WrongCurrentPassword", "The current password is wrong");
        var cut = Render<ChangePassword>();
        cut.Find("#current-password").Change("nope");
        cut.Find("#new-password").Change("Moria4567");
        cut.Find("#confirm-password").Change("Moria4567");

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.Find("[role=alert]").TextContent.Should().Contain("The current password is wrong");
        cut.FindAll("[role=status]").Should().BeEmpty();
    }

    [Fact]
    public void ChangePassword_WithNewPasswordsThatDiffer_SendsNothing()
    {
        // Arrange
        var cut = Render<ChangePassword>();
        cut.Find("#current-password").Change("Erebor123");
        cut.Find("#new-password").Change("Moria4567");
        cut.Find("#confirm-password").Change("Moria4568");

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.Find("[role=alert]").TextContent.Should().Contain("not the same");
        Api.Requests.Should().BeEmpty();
    }

    [Fact]
    public void ResetLink_ForAKnownEmail_ShowsTheLinkToSend()
    {
        // Arrange
        Api.Post("/api/admin/password-reset-links", new { link = "https://site/reset-password?email=a&token=b" });
        var cut = Render<ResetLink>();
        cut.Find("#player-email").Change(" thorin@example.com ");

        // Act
        cut.Find("form").Submit();

        // Assert
        Api.BodyOf<CreatePasswordResetLinkRequest>(LinkRoute).Should().Be(new CreatePasswordResetLinkRequest("thorin@example.com"));
        cut.Find("#reset-link").GetAttribute("value").Should().Be("https://site/reset-password?email=a&token=b");
    }

    [Fact]
    public void ResetLink_WhenTheApiRefuses_ShowsItsMessageAndNoLink()
    {
        // Arrange
        Api.RefusePost("/api/admin/password-reset-links", "User.NotAnAdmin", "Only the owner can do this");
        var cut = Render<ResetLink>();
        cut.Find("#player-email").Change("thorin@example.com");

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.Find("[role=alert]").TextContent.Should().Contain("Only the owner");
        cut.FindAll("#reset-link").Should().BeEmpty();
    }

    // The page reads the link from the address, as the browser opens it
    private IRenderedComponent<ResetPassword> RenderReset(string? email, string? token)
    {
        var query = new List<string>();

        if (email is not null)
        {
            query.Add($"email={Uri.EscapeDataString(email)}");
        }

        if (token is not null)
        {
            query.Add($"token={Uri.EscapeDataString(token)}");
        }

        Services.GetRequiredService<NavigationManager>().NavigateTo($"/reset-password?{string.Join('&', query)}");

        return Render<ResetPassword>();
    }
}
