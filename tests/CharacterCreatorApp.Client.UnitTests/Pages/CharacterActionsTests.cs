using Bunit;

using CharacterCreatorApp.Client.Pages.Characters;
using CharacterCreatorApp.Client.Services;
using CharacterCreatorApp.Client.UnitTests.Fixtures;

using FluentAssertions;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace CharacterCreatorApp.Client.UnitTests.Pages;

/// <summary>Edit, Level up and Delete, which the list and the sheet both show (#88).</summary>
public class CharacterActionsTests : WizardStepTestBase
{
    private static readonly Guid CharacterId = Guid.Parse("3f1c7a2e-5b8d-4e6f-9a0b-1c2d3e4f5a6b");
    private static readonly Guid NewDraftId = Guid.Parse("9e8d7c6b-5a49-4382-a1b0-c9d8e7f6a5b4");

    private bool _deleted;

    public CharacterActionsTests()
    {
        Services.AddSingleton(new CharacterService(Api.CreateClient()));
    }

    [Theory]
    [InlineData("Edit character", "Opening...", "edit")]
    [InlineData("Level up", "Level up", "level-up")]
    public void Click_AgainWhileTheEditOrLevelUpStarts_DoesNothing(string button, string buttonWhileStarting, string action)
    {
        // Arrange - the API has not answered the first click yet
        var route = $"/api/characters/{CharacterId}/{action}";
        Api.Post(route, NewDraftId);
        var answer = Api.Hold(route, "POST");
        var cut = RenderActions();
        Button(cut, button).Click();

        // Act
        Button(cut, buttonWhileStarting).Click();
        answer.SetResult();

        // Assert
        cut.WaitForAssertion(() => Navigation.Uri.Should().EndWith($"/character/create/{NewDraftId}"));
        Button(cut, buttonWhileStarting).HasAttribute("disabled").Should().BeTrue();
        Api.Requests.Should().ContainSingle(r => r == $"POST {route}");
    }

    [Fact]
    public void Click_EditWhenTheApiRefuses_ShowsItsReasonAndCanBeTriedAgain()
    {
        // Arrange
        Api.RefusePost($"/api/characters/{CharacterId}/edit", "Character.NotFound", "The character is gone");
        var cut = RenderActions();

        // Act
        Button(cut, "Edit character").Click();

        // Assert
        cut.WaitForAssertion(() => cut.Find("[role=alert]").TextContent.Should()
            .Contain("Failed to open the character for editing: The character is gone"));
        cut.FindAll("button").Should().OnlyContain(b => !b.HasAttribute("disabled"));
    }

    [Fact]
    public void Click_LevelUpWhenTheServerCannotBeReached_SaysSoWithoutTheExceptionsOwnMessage()
    {
        // Arrange
        Api.Unreachable("POST", $"/api/characters/{CharacterId}/level-up");
        var cut = RenderActions();

        // Act
        Button(cut, "Level up").Click();

        // Assert
        cut.WaitForAssertion(() => cut.Find("[role=alert]").TextContent.Trim().Should()
            .Be("Failed to level the character up: the server could not be reached."));
    }

    [Fact]
    public void ConfirmDelete_DeletesTheCharacterAndTellsThePage()
    {
        // Arrange
        Api.Delete($"/api/characters/{CharacterId}");
        var cut = RenderActions();
        Button(cut, "Delete character").Click();

        // Act
        DialogButton(cut, "Delete").Click();

        // Assert
        cut.WaitForAssertion(() => _deleted.Should().BeTrue());
        cut.FindAll("[role='dialog']").Should().BeEmpty();
        Api.Requests.Should().Equal($"DELETE /api/characters/{CharacterId}");
    }

    [Fact]
    public void ConfirmDelete_WhenTheServerCannotBeReached_ClosesTheDialogAndSaysSo()
    {
        // Arrange
        Api.Unreachable("DELETE", $"/api/characters/{CharacterId}");
        var cut = RenderActions();
        Button(cut, "Delete character").Click();

        // Act
        DialogButton(cut, "Delete").Click();

        // Assert
        cut.WaitForAssertion(() => cut.Find("[role=alert]").TextContent.Trim().Should()
            .Be("Failed to delete the character: the server could not be reached."));
        cut.FindAll("[role='dialog']").Should().BeEmpty();
        _deleted.Should().BeFalse();
    }

    [Fact]
    public void Cancel_ClosesTheDialogWithoutDeleting()
    {
        // Arrange
        var cut = RenderActions();
        Button(cut, "Delete character").Click();

        // Act
        DialogButton(cut, "Cancel").Click();

        // Assert
        cut.FindAll("[role='dialog']").Should().BeEmpty();
        Api.Requests.Should().BeEmpty();
    }

    [Fact]
    public void Render_Compact_UsesTheListsShortLabels()
    {
        // Act
        var cut = RenderActions(compact: true);

        // Assert
        cut.FindAll("button").Select(b => b.TextContent.Trim()).Should().Equal("Edit", "Level up", "Delete");
    }

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    private IRenderedComponent<CharacterActions> RenderActions(bool compact = false) =>
        Render<CharacterActions>(parameters => parameters
            .Add(p => p.CharacterId, CharacterId)
            .Add(p => p.Name, "Bruenor")
            .Add(p => p.Level, 3)
            .Add(p => p.Compact, compact)
            .Add(p => p.OnDeleted, () => _deleted = true));

    private static AngleSharp.Dom.IElement DialogButton(IRenderedComponent<CharacterActions> cut, string text) =>
        cut.Find("[role='dialog']").QuerySelectorAll("button").Single(b => b.TextContent.Trim() == text);
}
