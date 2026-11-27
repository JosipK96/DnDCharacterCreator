using Bunit;

using CharacterCreatorApp.Client.Pages.Characters;
using CharacterCreatorApp.Client.Services;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Common;
using CharacterCreatorApp.Contracts.Responses.Characters;
using CharacterCreatorApp.Contracts.Responses.Classes;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace CharacterCreatorApp.Client.UnitTests.Pages;

/// <summary>The list of the player's characters: a room each, the empty state, paging and deleting.</summary>
public class CharacterListTests : WizardStepTestBase
{
    private const string ListRoute = "/api/characters/get-list";

    public CharacterListTests()
    {
        Services.AddSingleton(new CharacterService(Api.CreateClient()));
        Api.Get("/api/classes/get-list", new List<ClassApiResponse>());
    }

    [Fact]
    public void Render_WithCharacters_ShowsEachAsARoomWithItsFactsAndActions()
    {
        // Arrange
        Api.Post(ListRoute, Page(totalCount: 1, Bruenor()));

        // Act
        var cut = Render<CharacterList>();

        // Assert
        var room = cut.Find("[data-testid='character']");
        room.QuerySelector("h2")!.TextContent.Should().Be("Bruenor");
        room.TextContent.Should().Contain("Level 3 · 2014 rules")
            .And.Contain("Dwarf (Hill Dwarf)")
            .And.Contain("Fighter (Champion)")
            .And.Contain("Soldier");
        room.QuerySelectorAll("button").Select(b => b.TextContent.Trim())
            .Should().Equal("View details", "Edit", "Level up", "Delete");
        cut.FindAll("[data-testid='no-characters']").Should().BeEmpty();
    }

    [Fact]
    public void Render_WithNoCharacters_ShowsTheEmptyRoomWithACreateButton()
    {
        // Arrange
        Api.Post(ListRoute, Page(totalCount: 0));

        // Act
        var cut = Render<CharacterList>();

        // Assert
        var empty = cut.Find("[data-testid='no-characters']");
        empty.TextContent.Should().Contain("No characters yet");
        empty.QuerySelector("button")!.TextContent.Trim().Should().Be("Create your first character");
        cut.FindAll("[data-testid='character']").Should().BeEmpty();
    }

    [Fact]
    public void Render_WithMoreThanOnePage_MarksOnlyTheCurrentPage()
    {
        // Arrange
        Api.Post(ListRoute, Page(totalCount: 30, Bruenor()));

        // Act
        var cut = Render<CharacterList>();

        // Assert
        var pages = cut.FindAll("nav[aria-label='Pages'] button")
            .Where(b => int.TryParse(b.TextContent.Trim(), out _))
            .Select(b => (b.TextContent.Trim(), b.GetAttribute("aria-current")))
            .ToList();
        pages.Should().Equal(("1", "page"), ("2", null), ("3", null));
    }

    [Fact]
    public void Delete_AsksInADialogWithADangerButtonAndACancel()
    {
        // Arrange
        Api.Post(ListRoute, Page(totalCount: 1, Bruenor()));
        var cut = Render<CharacterList>();

        // Act
        var buttons = cut.Find("[data-testid='character']").QuerySelectorAll("button");
        buttons[^1].Click();

        // Assert
        var dialog = cut.Find("[role='dialog']");
        dialog.TextContent.Should().Contain("Bruenor");
        dialog.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).Should().Equal("Cancel", "Delete");
    }

    private static CharacterSummaryApiResponse Bruenor() => new()
    {
        Id = Guid.Parse("3f1c7a2e-5b8d-4e6f-9a0b-1c2d3e4f5a6b"),
        Name = "Bruenor",
        Level = 3,
        Edition = "2014",
        RaceName = "Dwarf",
        SubraceName = "Hill Dwarf",
        ClassName = "Fighter",
        SubclassName = "Champion",
        BackgroundName = "Soldier",
    };

    private static FilterResponse<CharacterSummaryApiResponse> Page(int totalCount, params CharacterSummaryApiResponse[] items) => new()
    {
        Items = items,
        TotalCount = totalCount,
        PageIndex = 0,
        PageSize = 12,
    };
}
