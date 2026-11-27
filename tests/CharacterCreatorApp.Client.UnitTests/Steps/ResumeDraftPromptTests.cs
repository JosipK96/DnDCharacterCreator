using Bunit;

using CharacterCreatorApp.Client.Pages.CharacterCreation;
using CharacterCreatorApp.Client.Services;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Responses.Books;
using CharacterCreatorApp.Contracts.Responses.Characters;

using FluentAssertions;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

/// <summary>
/// Opening Create character with an unfinished new-character draft: one with nothing saved opens at
/// step 1 on that draft, one with step 1 saved asks to resume it or start fresh.
/// </summary>
public class ResumeDraftPromptTests : WizardStepTestBase
{
    private const string IncompletePath = "/api/character-drafts/incomplete";
    private const string PromptTitle = "Resume Character Creation?";

    private static readonly List<BookApiResponse> Books2014 = [new("PHB", "Player's Handbook", "2014", true)];

    public ResumeDraftPromptTests()
    {
        Services.AddSingleton(new CharacterService(Api.CreateClient()));
    }

    [Fact]
    public void Open_WithAnEmptyUnfinishedDraft_GoesToStep1OnThatDraftWithoutAsking()
    {
        // Arrange - step 1 not saved: no name or level
        var draft = new CharacterDraftDetailsApiResponse { Id = DraftId, Edition = "2014", AllowedSources = ["PHB"] };
        Api.Get(IncompletePath, draft);
        Api.Get(DraftPath(), draft);
        Api.Get("/api/books?edition=2014", Books2014);

        // Act
        var cut = Render<CreateCharacter>();

        // Assert
        cut.WaitForAssertion(() => cut.Find("h2").TextContent.Should().Be("Step 1: Name & Level"));
        cut.Markup.Should().NotContain(PromptTitle);
        NothingCreatedOrDeleted();
        Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith($"/character/create/{DraftId}");
    }

    [Fact]
    public void Open_WithStep1Saved_AsksToResume()
    {
        // Arrange
        Api.Get(IncompletePath, new CharacterDraftDetailsApiResponse
        {
            Id = DraftId, Name = "Tordek", Level = 3, Edition = "2014", AllowedSources = ["PHB"],
        });

        // The wizard behind the prompt mounts step 1 before a draft is chosen, which loads no draft id
        Api.Get($"/api/character-drafts/{Guid.Empty}", new CharacterDraftDetailsApiResponse());
        Api.Get("/api/books?edition=", new List<BookApiResponse>());

        // Act
        var cut = Render<CreateCharacter>();

        // Assert
        cut.WaitForAssertion(() => cut.FindAll("h3").Select(h => h.TextContent.Trim()).Should().Contain(PromptTitle));
        cut.Markup.Should().Contain("Tordek").And.Contain("Level 3");
        NothingCreatedOrDeleted();
    }

    private void NothingCreatedOrDeleted() =>
        Api.Requests.Should().NotContain(r => r.StartsWith("POST ", StringComparison.Ordinal) || r.StartsWith("DELETE ", StringComparison.Ordinal));
}
