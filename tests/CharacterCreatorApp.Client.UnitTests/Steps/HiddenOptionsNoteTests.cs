using AngleSharp.Dom;

using Bunit;

using CharacterCreatorApp.Client.Pages.CharacterCreation;
using CharacterCreatorApp.Client.Pages.CharacterCreation.Components;
using CharacterCreatorApp.Client.Services;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Requests.CharacterDrafts;
using CharacterCreatorApp.Contracts.Responses;

using FluentAssertions;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using static CharacterCreatorApp.Client.UnitTests.Fixtures.TestData;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

/// <summary>
/// The note under each list the draft's books filter: what the books not ticked in step 1 hold, as
/// the API counts it, with a way back to step 1 that returns to the step.
/// </summary>
public class HiddenOptionsNoteTests : WizardStepTestBase
{
    private static readonly HiddenOptionsApiResponse InXanathars =
        new(3, [new("XGE", "Xanathar's Guide to Everything", 3)]);

    private static readonly HiddenOptionsApiResponse InOneBook =
        new(1, [new("TCE", "Tasha's Cauldron of Everything", 1)]);

    private int _changedBooks;

    [Fact]
    public void RaceStep_WithRacesInUntickedBooks_SaysHowManyAndWhereAndOffersStep1()
    {
        // Arrange
        Api.Get(DraftPath(), new CharacterDraftDetailsApiResponse { Id = DraftId });
        Api.Get("/api/races/get-list?edition=2014&sources=PHB", new List<RaceApiResponse> { Race("elf", "Elf") });
        Api.Get(Hidden("races"), InXanathars);

        // Act
        var cut = Render<RaceStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 2)
            .Add(p => p.Edition, "2014")
            .Add(p => p.Sources, ["PHB"])
            .Add(p => p.OnChangeBooks, () => _changedBooks++));
        Button(NoteOf(cut, "races"), "Tick books in step 1").Click();

        // Assert
        Squash(NoteOf(cut, "races").TextContent).Should().StartWith(
            "3 more races and subraces are in books you haven't ticked: Xanathar's Guide to Everything (3).");
        _changedBooks.Should().Be(1);
    }

    [Fact]
    public void RaceStep_WithNothingInUntickedBooks_ShowsNoNote()
    {
        // Arrange - the base's answer for every count is nothing hidden
        Api.Get(DraftPath(), new CharacterDraftDetailsApiResponse { Id = DraftId });
        Api.Get("/api/races/get-list?edition=2014&sources=PHB", new List<RaceApiResponse> { Race("elf", "Elf") });

        // Act
        var cut = Render<RaceStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 2)
            .Add(p => p.Edition, "2014")
            .Add(p => p.Sources, ["PHB"])
            .Add(p => p.OnChangeBooks, () => _changedBooks++));

        // Assert
        cut.WaitForAssertion(() => Api.Requests.Should().Contain($"GET {Hidden("races")}"));
        cut.FindAll("[data-testid=hidden-options]").Should().BeEmpty();
    }

    [Fact]
    public void ClassStep_WithAClassAndItsSubclassesInUntickedBooks_NotesBothLists()
    {
        // Arrange - a level 3 fighter: the classes, and the subclasses of the class clicked
        Api.Get(DraftPath(), new CharacterDraftDetailsApiResponse { Id = DraftId });
        Api.Get("/api/classes/get-list?edition=2014&sources=PHB", new List<ClassApiResponse> { Class("fighter", "Fighter") });
        Api.Get("/api/subclasses?classId=fighter&sources=PHB", new List<SubclassApiResponse> { new("champion", "Champion", "fighter", "PHB", null, []) });
        Api.Get(Hidden("classes"), InOneBook);
        Api.Get(Hidden("subclasses", "classId=fighter"), InXanathars);

        var cut = Render<ClassStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 3)
            .Add(p => p.CharacterLevel, 3)
            .Add(p => p.Edition, "2014")
            .Add(p => p.Sources, ["PHB"])
            .Add(p => p.OnChangeBooks, () => _changedBooks++));

        // Act
        cut.FindAll("[data-testid=option-room]").Single(r => r.TextContent.Contains("Fighter", StringComparison.Ordinal)).Click();

        // Assert
        Squash(NoteOf(cut, "classes").TextContent).Should().StartWith(
            "1 more class is in books you haven't ticked: Tasha's Cauldron of Everything (1).");
        Squash(NoteOf(cut, "subclasses").TextContent).Should().StartWith(
            "3 more subclasses are in books you haven't ticked: Xanathar's Guide to Everything (3).");
    }

    [Fact]
    public void ClassStep_InALevelUp_NotesTheSubclassesWithoutAWayToStep1()
    {
        // Arrange - a level-up keeps the class, so only the subclasses are noted, and it has no step 1
        Api.Get(DraftPath(), new CharacterDraftDetailsApiResponse { Id = DraftId, ClassId = "fighter" });
        Api.Get("/api/classes/get-list?edition=2014&sources=PHB", new List<ClassApiResponse> { Class("fighter", "Fighter") });
        Api.Get("/api/subclasses?classId=fighter&sources=PHB", new List<SubclassApiResponse> { new("champion", "Champion", "fighter", "PHB", null, []) });
        Api.Get(Hidden("subclasses", "classId=fighter"), InXanathars);

        // Act
        var cut = Render<ClassStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 3)
            .Add(p => p.Flow, WizardSteps.LevelUpOf(["hit-points", "subclass"]))
            .Add(p => p.CharacterLevel, 3)
            .Add(p => p.Edition, "2014")
            .Add(p => p.Sources, ["PHB"])
            .Add(p => p.ClassLocked, true));

        // Assert
        cut.WaitForAssertion(() => cut.FindAll("[data-testid=hidden-options]").Select(n => n.GetAttribute("data-list")).Should().Equal("subclasses"));
        Buttons(cut, "Tick books in step 1").Should().BeEmpty();
    }

    [Fact]
    public void BackgroundStep_WithBackgroundsAndDarkGiftsInUntickedBooks_NotesBoth()
    {
        // Arrange - a Mist Wanderer picks a Dark Gift, counted on its own
        var wanderer = Background("mist_wanderer", "Mist Wanderer") with { FeatChoiceCategory = "Dark Gift" };
        Api.Get(DraftPath(), new CharacterDraftDetailsApiResponse { Id = DraftId, BackgroundId = "mist_wanderer" });
        Api.Get("/api/backgrounds?edition=2024&sources=XPHB", new List<BackgroundApiResponse> { wanderer });
        Api.Get("/api/feats?edition=2024&sources=XPHB", new List<FeatApiResponse>());
        Api.Get(Hidden("backgrounds", edition: "2024", sources: "XPHB"), InXanathars);
        Api.Get(Hidden("feats", "category=Dark%20Gift", "2024", "XPHB"), InOneBook);

        // Act
        var cut = Render<BackgroundStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 4)
            .Add(p => p.Edition, "2024")
            .Add(p => p.Sources, ["XPHB"])
            .Add(p => p.OnChangeBooks, () => _changedBooks++));

        // Assert
        Squash(NoteOf(cut, "backgrounds").TextContent).Should().StartWith("3 more backgrounds are in books you haven't ticked");
        Squash(NoteOf(cut, "feats").TextContent).Should().StartWith(
            "1 more feat is in books you haven't ticked: Tasha's Cauldron of Everything (1).");
    }

    [Fact]
    public void ImprovementsStep_WithAnImprovementTakingAFeat_NotesTheFeatsOfUntickedBooks()
    {
        // Arrange - a level 4 fighter's improvement
        var fighter = Class("fighter-phb", "Fighter", 4);
        Api.Get(DraftPath(), new CharacterDraftDetailsApiResponse
        {
            Id = DraftId, Level = 4, Edition = "2014", AllowedSources = ["PHB"], ClassId = fighter.Id,
        });
        Api.Get($"/api/classes/{fighter.Id}", fighter);
        Api.Get("/api/feats?edition=2014&sources=PHB", new List<FeatApiResponse> { Feat("alert-phb", "Alert") });
        Api.Get("/api/proficiencies", new List<ProficiencyApiResponse>());
        Api.Get(DraftPath("fighting-styles"), new FightingStylesApiResponse(0, []));
        Api.Get(Hidden("feats"), InXanathars);

        var cut = Render<ImprovementsStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 8)
            .Add(p => p.CharacterLevel, 4)
            .Add(p => p.OnChangeBooks, () => _changedBooks++));
        cut.WaitForAssertion(() => cut.FindAll("div.animate-spin").Should().BeEmpty());
        cut.FindAll("[data-testid=hidden-options]").Should().BeEmpty("no feat list shows while the improvement raises scores");

        // Act
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Feat").Click();

        // Assert
        Squash(NoteOf(cut, "feats").TextContent).Should().StartWith(
            "3 more feats are in books you haven't ticked: Xanathar's Guide to Everything (3).");
    }

    [Fact]
    public void SpellsStep_ForACasterWithASubclass_CountsItsListsUpToTheLevelItPicksFrom()
    {
        // Arrange - a level 1 wizard of a subclass, picking up to level 1
        Api.Get(DraftPath(), new CharacterDraftDetailsApiResponse
        {
            Id = DraftId, Level = 1, Edition = "2014", AllowedSources = ["PHB"], ClassId = "wizard-phb", SubclassId = "evoker",
        });
        Api.Get("/api/classes/wizard-phb", Class("wizard-phb", "Wizard"));
        Api.Get("/api/subclasses/evoker", new SubclassApiResponse("evoker", "Evoker", "wizard-phb", "PHB", null, []));
        Api.Get(DraftPath("spell-allowance"), new SpellAllowanceApiResponse(true, 3, 6, 1, false, []));
        Api.Get("/api/spells?classId=wizard-phb&maxLevel=1&sources=PHB", new List<SpellApiResponse> { Spell("light-phb", "Light", 0) });
        Api.Get("/api/spells?subclassId=evoker&maxLevel=1&sources=PHB", new List<SpellApiResponse>());
        Api.Get(DraftPath("bonus-spells"), new List<BonusSpellChoiceApiResponse>());
        Api.Get(Hidden("spells", "classId=wizard-phb&subclassId=evoker&maxLevel=1"), InXanathars);

        // Act
        var cut = Render<SpellsStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 9)
            .Add(p => p.OnChangeBooks, () => _changedBooks++));

        // Assert
        Squash(NoteOf(cut, "spells").TextContent).Should().StartWith(
            "3 more spells are in books you haven't ticked: Xanathar's Guide to Everything (3).");
    }

    [Fact]
    public void Wizard_BackFromStep1WithABookTicked_ReturnsToTheStepThatSentThePlayerThere()
    {
        // Arrange - a draft on the class step, with a class in Tasha's
        RenderWizardOnTheClassStep(allowedSources: ["PHB"]);
        Api.Get("/api/classes/get-list?edition=2014&sources=PHB&sources=TCE", new List<ClassApiResponse> { Class("fighter", "Fighter") });
        var cut = Render<CreateCharacter>(parameters => parameters.Add(p => p.DraftIdParam, DraftId.ToString()));
        Button(NoteOf(cut, "classes"), "Tick books in step 1").Click();
        cut.WaitForAssertion(() => cut.Find("h2").TextContent.Should().Be("Step 1: Name & Level"));
        cut.WaitForAssertion(() => BookBox(cut, "Tasha's Cauldron of Everything").Change(true));

        // Act
        Button(cut, "Next: Race").Click();

        // Assert
        cut.WaitForAssertion(() => cut.Find("h2").TextContent.Should().Be("Step 3: Choose Your Class"));
        Api.BodyOf<SetAllowedBooksApiRequest>(SaveRoute("books")).Books.Should().Equal("TCE");
    }

    [Fact]
    public void Wizard_BackFromStep1WithABookUnticked_GoesOnToStep2()
    {
        // Arrange - unticking a book clears what came from it, so the steps are walked again
        RenderWizardOnTheClassStep(allowedSources: ["PHB", "XGE"]);
        Api.Get("/api/races/get-list?edition=2014&sources=PHB", new List<RaceApiResponse> { Race("elf", "Elf") });
        var cut = Render<CreateCharacter>(parameters => parameters.Add(p => p.DraftIdParam, DraftId.ToString()));
        Button(NoteOf(cut, "classes"), "Tick books in step 1").Click();
        cut.WaitForAssertion(() => BookBox(cut, "Xanathar's Guide to Everything").Change(false));

        // Act
        Button(cut, "Next: Race").Click();

        // Assert
        cut.WaitForAssertion(() => cut.Find("h2").TextContent.Should().Be("Step 2: Choose Your Race"));
    }

    private static string Hidden(string list, string? more = null, string edition = "2014", string sources = "PHB") =>
        $"{HiddenOptionsPath}?edition={edition}&list={list}{(more is null ? string.Empty : "&" + more)}&sources={sources}";

    private static string Squash(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static IElement NoteOf<TComponent>(IRenderedComponent<TComponent> cut, string list)
        where TComponent : IComponent
    {
        IElement? note = null;
        cut.WaitForAssertion(() => note = cut.FindAll("[data-testid=hidden-options]").Single(n => n.GetAttribute("data-list") == list));
        return note!;
    }

    private static List<IElement> Buttons(IElement scope, string text) =>
        scope.QuerySelectorAll("button").Where(b => b.TextContent.Contains(text, StringComparison.Ordinal)).ToList();

    private static IElement Button(IElement scope, string text) => Buttons(scope, text).First();

    private static IElement BookBox(IRenderedComponent<CreateCharacter> cut, string name) =>
        cut.FindAll("label").Single(l => l.QuerySelector("span")?.TextContent == name).QuerySelector("input[type=checkbox]")!;

    // A draft named and of a race, with no class yet: the wizard opens on the class step
    private void RenderWizardOnTheClassStep(List<string> allowedSources)
    {
        Services.AddSingleton(new CharacterService(Api.CreateClient()));

        var draft = new CharacterDraftDetailsApiResponse
        {
            Id = DraftId, Name = "Tordek", Level = 1, Edition = "2014", AllowedSources = allowedSources, RaceId = "elf",
        };
        var sources = string.Join('&', allowedSources.Select(s => $"sources={s}"));

        Api.Get(DraftPath(), draft);
        Api.Get($"/api/classes/get-list?edition=2014&{sources}", new List<ClassApiResponse> { Class("fighter", "Fighter") });
        Api.Get($"{HiddenOptionsPath}?edition=2014&list=classes&{sources}", InOneBook);
        Api.Get("/api/books?edition=2014", new List<BookApiResponse>
        {
            new("PHB", "Player's Handbook (2014)", "2014", true),
            new("TCE", "Tasha's Cauldron of Everything", "2014", false),
            new("XGE", "Xanathar's Guide to Everything", "2014", false),
        });
        Api.Put(DraftPath("name-and-level"));
        Api.Put(DraftPath("books"));
    }
}
