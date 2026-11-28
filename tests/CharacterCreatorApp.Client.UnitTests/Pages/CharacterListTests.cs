using Bunit;

using CharacterCreatorApp.Client.Pages.Characters;
using CharacterCreatorApp.Client.Services;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Common;
using CharacterCreatorApp.Contracts.Requests.Character;
using CharacterCreatorApp.Contracts.Responses.Characters;
using CharacterCreatorApp.Contracts.Responses.Classes;

using FluentAssertions;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using static CharacterCreatorApp.Client.UnitTests.Fixtures.TestData;

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

    [Fact]
    public void ApplyFilters_SendsTheSearchClassAndSortFromTheFirstPage()
    {
        // Arrange
        Api.Get("/api/classes/get-list", new List<ClassApiResponse> { Class("fighter", "Fighter") });
        Api.Post(ListRoute, Page(totalCount: 1, Bruenor()));
        var cut = Render<CharacterList>();
        cut.Find("#character-search").Input("Brue");
        cut.Find("#character-class").Change("fighter");
        cut.Find("#character-sort").Change("level");
        cut.Find("#character-order").Change("asc");

        // Act
        Button(cut, "Apply filters").Click();

        // Assert
        var sent = Api.BodyOf<FilterRequest<GetCharactersApiRequest>>($"POST {ListRoute}");
        sent.SearchParams.Should().BeEquivalentTo(new GetCharactersApiRequest { Name = "Brue", ClassId = "fighter" });
        sent.Sorting.Should().BeEquivalentTo(new SortingParams { SortColumn = "level", SortDirection = "ASC" });
        sent.Pagination!.PageIndex.Should().Be(0);
        cut.FindAll("#character-class option").Select(o => o.TextContent).Should().Equal("All Classes", "Fighter (2014)");
    }

    [Fact]
    public void Render_WithFiltersThatMatchNothing_OffersToClearThem()
    {
        // Arrange
        Api.Post(ListRoute, Page(totalCount: 0));
        var cut = Render<CharacterList>();
        cut.Find("#character-search").Input("Nobody");

        // Act
        Button(cut, "Apply filters").Click();

        // Assert
        var empty = cut.Find("[data-testid='no-characters']");
        empty.TextContent.Should().Contain("No characters match your filters");
        empty.QuerySelector("button")!.TextContent.Trim().Should().Be("Clear filters");
    }

    [Fact]
    public void ClearFilters_FromTheEmptyState_AsksForEveryCharacterAgain()
    {
        // Arrange
        Api.Post(ListRoute, Page(totalCount: 0));
        var cut = Render<CharacterList>();
        cut.Find("#character-search").Input("Nobody");
        Button(cut, "Apply filters").Click();

        // Act
        cut.Find("[data-testid='no-characters'] button").Click();

        // Assert
        Api.BodyOf<FilterRequest<GetCharactersApiRequest>>($"POST {ListRoute}").SearchParams.Should()
            .BeEquivalentTo(new GetCharactersApiRequest());
    }

    [Theory]
    [InlineData("Next", 1)]
    [InlineData("3", 2)]
    public void Click_APageButton_AsksForThatPage(string button, int pageIndex)
    {
        // Arrange
        Api.Post(ListRoute, Page(totalCount: 30, Bruenor()));
        var cut = Render<CharacterList>();

        // Act
        cut.FindAll("nav[aria-label='Pages'] button").Single(b => b.TextContent.Trim() == button).Click();

        // Assert
        Api.BodyOf<FilterRequest<GetCharactersApiRequest>>($"POST {ListRoute}").Pagination!.PageIndex.Should().Be(pageIndex);
    }

    [Fact]
    public void Render_OnTheMiddleOfManyPages_ShortensThePagerWithGaps()
    {
        // Arrange - page 6 of 10
        var page = Page(totalCount: 120, Bruenor());
        page.PageIndex = 5;
        Api.Post(ListRoute, page);

        // Act
        var cut = Render<CharacterList>();

        // Assert
        cut.Find("nav[aria-label='Pages']").Children.Select(c => c.TextContent.Trim())
            .Should().Equal("Previous", "1", "...", "5", "6", "7", "...", "10", "Next");
        cut.Markup.Should().Contain("Showing 61-72 of 120 characters");
    }

    [Theory]
    [InlineData("Edit", "edit")]
    [InlineData("Level up", "level-up")]
    public void Click_EditOrLevelUp_OpensTheCharacterInTheWizard(string button, string action)
    {
        // Arrange
        var draftId = Guid.Parse("9e8d7c6b-5a49-4382-a1b0-c9d8e7f6a5b4");
        Api.Post(ListRoute, Page(totalCount: 1, Bruenor()));
        Api.Post($"/api/characters/{Bruenor().Id}/{action}", draftId);
        var cut = Render<CharacterList>();

        // Act
        Button(cut, button).Click();

        // Assert
        cut.WaitForAssertion(() => Navigation.Uri.Should().EndWith($"/character/create/{draftId}"));
    }

    [Fact]
    public void Click_EditWhenTheApiRefuses_ShowsItsReason()
    {
        // Arrange
        Api.Post(ListRoute, Page(totalCount: 1, Bruenor()));
        Api.RefusePost($"/api/characters/{Bruenor().Id}/edit", "Character.NotFound", "The character is gone");
        var cut = Render<CharacterList>();

        // Act
        Button(cut, "Edit").Click();

        // Assert
        cut.WaitForAssertion(() => cut.Find("[role=alert]").TextContent.Should().Contain("Failed to open the character for editing: The character is gone"));
    }

    [Fact]
    public void Render_ACharacterAtTheTopLevel_OffersNoLevelUp()
    {
        // Arrange
        var max = Bruenor();
        max.Level = 20;
        Api.Post(ListRoute, Page(totalCount: 1, max));

        // Act
        var cut = Render<CharacterList>();

        // Assert
        Buttons(cut, "Level up").Should().BeEmpty();
    }

    [Fact]
    public void Click_ViewDetails_OpensTheSheet()
    {
        // Arrange
        Api.Post(ListRoute, Page(totalCount: 1, Bruenor()));
        var cut = Render<CharacterList>();

        // Act
        Button(cut, "View details").Click();

        // Assert
        Navigation.Uri.Should().EndWith($"/character/{Bruenor().Id}");
    }

    [Fact]
    public void ConfirmDelete_DeletesTheCharacterAndLoadsTheListAgain()
    {
        // Arrange
        Api.Post(ListRoute, Page(totalCount: 1, Bruenor()));
        Api.Delete($"/api/characters/{Bruenor().Id}");
        var cut = Render<CharacterList>();
        Button(cut, "Delete").Click();

        // Act
        cut.Find("[role='dialog']").QuerySelectorAll("button").Single(b => b.TextContent.Trim() == "Delete").Click();

        // Assert
        cut.WaitForAssertion(() => cut.FindAll("[role='dialog']").Should().BeEmpty());
        Api.Requests.Should().Equal(
            $"POST {ListRoute}", "GET /api/classes/get-list", $"DELETE /api/characters/{Bruenor().Id}", $"POST {ListRoute}");
    }

    [Fact]
    public void Render_WhenTheListFailsToLoad_SaysSoAndOffersToTryAgain()
    {
        // Arrange
        Api.RefusePost(ListRoute, "Characters.Failed", "The list could not be read");

        // Act
        var cut = Render<CharacterList>();

        // Assert
        cut.Find("[role=alert]").TextContent.Should().Contain("The list could not be read");
        Buttons(cut, "Try again").Should().ContainSingle();
    }

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();
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
