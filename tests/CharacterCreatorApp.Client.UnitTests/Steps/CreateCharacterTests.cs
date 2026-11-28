using Bunit;

using CharacterCreatorApp.Client.Pages.CharacterCreation;
using CharacterCreatorApp.Client.Services;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Requests.Character;
using CharacterCreatorApp.Contracts.Responses.Books;
using CharacterCreatorApp.Contracts.Responses.Characters;
using CharacterCreatorApp.Contracts.Responses.Classes;
using CharacterCreatorApp.Contracts.Responses.Languages;

using FluentAssertions;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using static CharacterCreatorApp.Client.UnitTests.Fixtures.TestData;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

/// <summary>
/// The wizard host's entry flows, each opening on the right step: a new character, a resumed
/// draft (and starting fresh instead), an edit and a level-up; with where cancelling and saving go.
/// Which step a draft opens at is <see cref="WizardNavigatorTests"/>; this is the page around it.
/// </summary>
public class CreateCharacterTests : WizardStepTestBase
{
    private const string IncompletePath = "/api/character-drafts/incomplete";
    private const string DraftsPath = "/api/character-drafts";

    private static readonly Guid CharacterId = Guid.Parse("6a1d6f0e-2b4c-4f3a-8e57-1c9b0d2e3f45");

    public CreateCharacterTests()
    {
        Services.AddSingleton(new CharacterService(Api.CreateClient()));

        // Step 1 lists the edition's books, whichever edition a test's draft has
        Api.GetAnyQuery("/api/books", new List<BookApiResponse> { new("PHB", "Player's Handbook", "2014", true) });
    }

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    [Fact]
    public void Open_WithNoUnfinishedDraft_CreatesOneAndOpensStep1OnIt()
    {
        // Arrange
        Api.NotFound(IncompletePath);
        Api.Post(DraftsPath, DraftId);
        Api.Get(DraftPath(), new CharacterDraftDetailsApiResponse { Id = DraftId, Edition = "2014", AllowedSources = ["PHB"] });

        // Act
        var cut = Render<CreateCharacter>();

        // Assert
        cut.WaitForAssertion(() => cut.Find("h2").TextContent.Should().Be("Step 1: Name & Level"));
        cut.Find("h1").TextContent.Should().Be("Create New Character");
        Api.Requests.Should().ContainSingle(r => r == $"POST {DraftsPath}");
        Navigation.Uri.Should().EndWith($"/character/create/{DraftId}");
    }

    [Fact]
    public void Resume_AnUnfinishedDraftWithoutAClass_OpensTheClassStepOnIt()
    {
        // Arrange - named, of a race, with no class yet
        var draft = new CharacterDraftDetailsApiResponse
        {
            Id = DraftId, Name = "Tordek", Level = 1, Edition = "2014", AllowedSources = ["PHB"], RaceId = "dwarf",
        };
        Api.Get(IncompletePath, draft);
        ArrangeTheStepBehindThePrompt();
        Api.Get(DraftPath(), draft);
        Api.Get("/api/classes/get-list?edition=2014&sources=PHB", new List<ClassApiResponse> { Class("fighter", "Fighter") });
        var cut = Render<CreateCharacter>();

        // Act
        cut.WaitForAssertion(() => Button(cut, "Resume Character").Click());

        // Assert
        cut.WaitForAssertion(() => cut.Find("h2").TextContent.Should().Be("Step 3: Choose Your Class"));
        cut.Markup.Should().NotContain("Resume Character Creation?");
        Navigation.Uri.Should().EndWith($"/character/create/{DraftId}");
        Api.Requests.Should().NotContain(r => r.StartsWith("POST ", StringComparison.Ordinal) || r.StartsWith("DELETE ", StringComparison.Ordinal));
    }

    [Fact]
    public void StartFresh_DeletesTheUnfinishedDraftAndCreatesANewOne()
    {
        // Arrange
        var newDraftId = Guid.Parse("3c2b1a09-8f7e-4d6c-9b5a-0e1f2d3c4b5a");
        Api.Get(IncompletePath, new CharacterDraftDetailsApiResponse { Id = DraftId, Name = "Tordek", Level = 1, Edition = "2014" });
        ArrangeTheStepBehindThePrompt();
        Api.Delete(DraftPath());
        Api.Post(DraftsPath, newDraftId);
        Api.Get($"{DraftsPath}/{newDraftId}", new CharacterDraftDetailsApiResponse { Id = newDraftId, Edition = "2014" });
        var cut = Render<CreateCharacter>();

        // Act
        cut.WaitForAssertion(() => Button(cut, "Start Fresh").Click());

        // Assert - the old draft goes before the new one is made, so the player never has two
        cut.WaitForAssertion(() => Navigation.Uri.Should().EndWith($"/character/create/{newDraftId}"));
        Api.Requests.Where(r => r.StartsWith("POST ", StringComparison.Ordinal) || r.StartsWith("DELETE ", StringComparison.Ordinal))
            .Should().Equal($"DELETE {DraftPath()}", $"POST {DraftsPath}");
        cut.Find("h2").TextContent.Should().Be("Step 1: Name & Level");
    }

    [Fact]
    public void Open_AnEditWithAStepToPick_OpensThatStepUnderTheEditsHeading()
    {
        // Arrange - an older character with no language picked
        ArrangeAnEdit(choicesNeeded: ["languages"]);

        // Act
        var cut = Render<CreateCharacter>(parameters => parameters.Add(p => p.DraftIdParam, DraftId.ToString()));

        // Assert
        cut.WaitForAssertion(() => cut.Find("h2").TextContent.Should().Be("Step 5: Choose Languages"));
        cut.Find("h1").TextContent.Should().Be("Edit Tordek");
        Buttons(cut, "Cancel Editing").Should().ContainSingle();
    }

    [Fact]
    public void Cancel_AnEdit_DeletesTheDraftAndGoesBackToTheSheet()
    {
        // Arrange
        ArrangeAnEdit(choicesNeeded: ["languages"]);
        Api.Delete(DraftPath());
        var cut = Render<CreateCharacter>(parameters => parameters.Add(p => p.DraftIdParam, DraftId.ToString()));
        cut.WaitForAssertion(() => Button(cut, "Cancel Editing").Click());

        // Act
        cut.FindAll("h3").Select(h => h.TextContent).Should().Contain("Cancel Editing?");
        Button(cut, "Yes, Cancel").Click();

        // Assert
        cut.WaitForAssertion(() => Navigation.Uri.Should().EndWith($"/character/{CharacterId}"));
        Api.Requests.Should().Contain($"DELETE {DraftPath()}");
    }

    [Fact]
    public void Open_ALevelUp_OpensItsHitPointsStepUnderTheLevelUpsHeading()
    {
        // Arrange
        ArrangeALevelUp();

        // Act
        var cut = Render<CreateCharacter>(parameters => parameters.Add(p => p.DraftIdParam, DraftId.ToString()));

        // Assert
        cut.WaitForAssertion(() => cut.Find("h2").TextContent.Should().Be("Step 1: Hit Points"));
        cut.Find("h1").TextContent.Should().Be("Level up Tordek to level 5");
        Buttons(cut, "Cancel Level Up").Should().ContainSingle();
    }

    [Fact]
    public void Back_FromALevelUpsFirstStep_AsksBeforeLeaving()
    {
        // Arrange
        ArrangeALevelUp();
        var cut = Render<CreateCharacter>(parameters => parameters.Add(p => p.DraftIdParam, DraftId.ToString()));
        cut.WaitForAssertion(() => cut.Find("h2").TextContent.Should().Be("Step 1: Hit Points"));

        // Act
        Button(cut, "Back").Click();

        // Assert
        cut.FindAll("h3").Select(h => h.TextContent).Should().Contain("Cancel Level Up?");
        Api.Requests.Should().NotContain(r => r.StartsWith("DELETE ", StringComparison.Ordinal));
    }

    [Fact]
    public void ContinueLevellingUp_ClosesTheDialogAndStaysOnTheStep()
    {
        // Arrange
        ArrangeALevelUp();
        var cut = Render<CreateCharacter>(parameters => parameters.Add(p => p.DraftIdParam, DraftId.ToString()));
        cut.WaitForAssertion(() => Button(cut, "Back").Click());

        // Act
        Button(cut, "Continue Levelling Up").Click();

        // Assert
        cut.FindAll("h3").Select(h => h.TextContent).Should().NotContain("Cancel Level Up?");
        cut.Find("h2").TextContent.Should().Be("Step 1: Hit Points");
    }

    [Fact]
    public void Save_ALevelUp_SendsTheDraftAndOpensTheCharactersSheet()
    {
        // Arrange - a level-up whose new level brings only hit points: Hit Points, then Review
        ArrangeALevelUp();
        Api.Put(DraftPath("hit-points"));
        Api.Get(DraftPath("preview"), new CharacterPreviewApiResponse(
            "Tordek", 5, "2014", "Dwarf", null, "Fighter", null, "Soldier", "Medium", [], [], [], [], [],
            new CharacterStatsApiResponse(3, 44, 16, 1, 25, "5d10", 11, [], [], [], [], null, [], []),
            null));
        Api.Post("/api/characters", CharacterId);
        var cut = Render<CreateCharacter>(parameters => parameters.Add(p => p.DraftIdParam, DraftId.ToString()));
        cut.WaitForAssertion(() => Button(cut, "Take 6").Click());
        Button(cut, "Next: Review").Click();

        // Act
        cut.WaitForAssertion(() => Button(cut, "Save Changes").Click());

        // Assert
        cut.WaitForAssertion(() => Navigation.Uri.Should().EndWith($"/character/{CharacterId}"));
        Api.BodyOf<CreateCharacterApiRequest>("POST /api/characters").Should().Be(new CreateCharacterApiRequest(DraftId));
    }

    [Fact]
    public void Open_ADraftThatIsGone_SaysSo()
    {
        // Arrange
        Api.NotFound(DraftPath());

        // Act
        var cut = Render<CreateCharacter>(parameters => parameters.Add(p => p.DraftIdParam, DraftId.ToString()));

        // Assert
        cut.WaitForAssertion(() => cut.Find("[role=alert]").TextContent.Should().Contain("Character draft not found"));
    }

    [Fact]
    public void BackToCharacters_FromADraftThatIsGone_GoesToTheList()
    {
        // Arrange
        Api.NotFound(DraftPath());
        var cut = Render<CreateCharacter>(parameters => parameters.Add(p => p.DraftIdParam, DraftId.ToString()));

        // Act
        cut.WaitForAssertion(() => Button(cut, "Back to Characters").Click());

        // Assert
        Navigation.Uri.Should().EndWith("/characters");
    }

    // Behind the resume prompt the wizard mounts step 1 before a draft is chosen, with no draft id
    private void ArrangeTheStepBehindThePrompt() =>
        Api.Get($"{DraftsPath}/{Guid.Empty}", new CharacterDraftDetailsApiResponse());

    private void ArrangeAnEdit(List<string> choicesNeeded)
    {
        Api.Get(DraftPath(), new CharacterDraftDetailsApiResponse
        {
            Id = DraftId, Name = "Tordek", Level = 1, Edition = "2014", AllowedSources = ["PHB"], RaceId = "dwarf",
            ClassId = "fighter", BackgroundId = "soldier", CharacterId = CharacterId, ChoicesNeeded = choicesNeeded,
        });

        // The languages step it opens at
        Api.Get(DraftPath("choices-owed"), Owed(languages: 1));
        Api.Get("/api/languages/get-list?edition=2014", new List<LanguageApiResponse> { new("giant", "Giant", "Standard") });
    }

    private void ArrangeALevelUp()
    {
        Api.Get(DraftPath(), new CharacterDraftDetailsApiResponse
        {
            Id = DraftId, Name = "Tordek", Level = 5, Edition = "2014", AllowedSources = ["PHB"], ClassId = "fighter-phb",
            CharacterId = CharacterId, LevelUpChoices = ["hit-points"], ChoicesNeeded = ["hit-points"], HitDieLevels = [5],
        });

        // The hit points step it opens at, for a fighter's d10
        Api.Get(DraftPath("choices-owed"), Owed(hitDieSize: 10, hitDieFixedValue: 6));
    }
}
