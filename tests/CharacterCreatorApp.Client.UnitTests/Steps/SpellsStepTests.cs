using AngleSharp.Dom;

using Bunit;

using CharacterCreatorApp.Client.Pages.CharacterCreation.Components;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Requests.CharacterDrafts;
using CharacterCreatorApp.Contracts.Responses.Characters;
using CharacterCreatorApp.Contracts.Responses.Classes;
using CharacterCreatorApp.Contracts.Responses.Spells;

using FluentAssertions;

using static CharacterCreatorApp.Client.UnitTests.Fixtures.TestData;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

public class SpellsStepTests : WizardStepTestBase
{
    private static readonly string Saved = SaveRoute("spells");

    private static readonly List<SpellApiResponse> WizardSpells =
    [
        Spell("fire-bolt-phb", "Fire Bolt", 0),
        Spell("light-phb", "Light", 0, "Evocation"),
        Spell("mage-hand-phb", "Mage Hand", 0, "Conjuration"),
        Spell("magic-missile-phb", "Magic Missile", 1),
        Spell("shield-phb", "Shield", 1, "Abjuration"),
        Spell("sleep-phb", "Sleep", 1, "Enchantment"),
    ];

    private int _movedOn;

    [Fact]
    public void Render_ForACaster_ShowsTheCountsTheServerAllows()
    {
        // Arrange
        ArrangeWizard(new SpellAllowanceApiResponse(true, 3, 6, 1, false, [], WizardSpells));

        // Act
        var cut = RenderStep();

        // Assert
        cut.Markup.Should().Contain("Choose 3 cantrips and 6 spells.");
        Counts(cut).Should().Equal("0 of 3 chosen: choose 3 more", "0 of 6 chosen: choose 6 more");
        cut.Markup.Should().NotContain("Prepared Spells");
    }

    [Fact]
    public void ToggleCantrip_AtTheAllowedCount_DisablesTheOthers()
    {
        // Arrange
        ArrangeWizard(new SpellAllowanceApiResponse(true, 2, 1, 1, false, [], WizardSpells));
        var cut = RenderStep();

        // Act
        SpellButton(cut, "Fire Bolt").Click();
        SpellButton(cut, "Light").Click();

        // Assert
        SpellButton(cut, "Mage Hand").HasAttribute("disabled").Should().BeTrue();
        SpellButton(cut, "Fire Bolt").HasAttribute("disabled").Should().BeFalse();
        Counts(cut)[0].Should().Be("2 of 2 chosen");
    }

    [Fact]
    public void Next_WithExactlyTheAllowedPicks_SavesThemAndMovesOn()
    {
        // Arrange
        ArrangeWizard(new SpellAllowanceApiResponse(true, 1, 2, 1, false, [], WizardSpells));
        Api.Put(DraftPath("spells"));
        var cut = RenderStep();
        SpellButton(cut, "Mage Hand").Click();
        SpellButton(cut, "Shield").Click();
        Button(cut, "Next: Review").HasAttribute("disabled").Should().BeTrue();

        // Act
        SpellButton(cut, "Sleep").Click();
        Button(cut, "Next: Review").Click();

        // Assert
        Api.BodyOf<ChooseSpellsApiRequest>(Saved).SpellIds.Should().Equal("mage-hand-phb", "shield-phb", "sleep-phb");
        _movedOn.Should().Be(1);
    }

    [Fact]
    public void Render_ForA2014PreparedCaster_ExplainsTheCountAndDoesNotOfferTheAlwaysPreparedSpells()
    {
        // Arrange: a level 1 life cleric with Wisdom 16 prepares 3 + 1, and its domain spells on top
        var draft = Draft("cleric-phb", "life-domain-phb");
        Api.Get(DraftPath(), draft);
        Api.Get("/api/classes/cleric-phb", Class("cleric-phb", "Cleric"));
        Api.Get("/api/subclasses/life-domain-phb", new SubclassApiResponse("life-domain-phb", "Life Domain", "cleric-phb", "PHB", null, []));
        Api.Get(DraftPath("spell-allowance"), new SpellAllowanceApiResponse(true, 3, 4, 1, true, ["bless-phb", "cure-wounds-phb"],
        [
            Spell("guidance-phb", "Guidance", 0), Spell("bless-phb", "Bless", 1), Spell("cure-wounds-phb", "Cure Wounds", 1),
            Spell("guiding-bolt-phb", "Guiding Bolt", 1), Spell("healing-word-phb", "Healing Word", 1),
        ]));
        Api.Get(DraftPath("bonus-spells"), new List<BonusSpellChoiceApiResponse>());

        // Act
        var cut = RenderStep();

        // Assert
        var note = Squash(cut.Find("[role=note]").TextContent);
        note.Should().Contain("Cleric prepares 4 spells from the Cleric spell list");
        note.Should().Contain("Your subclass's spells are always prepared on top of these: Bless, Cure Wounds.");
        RowNames(cut).Should().Equal("Guidance", "Guiding Bolt", "Healing Word");
    }

    [Fact]
    public void Next_ForANonCaster_SaysSoAndMovesOnWithoutSavingSpells()
    {
        // Arrange
        Api.Get(DraftPath(), Draft("fighter-phb"));
        Api.Get("/api/classes/fighter-phb", Class("fighter-phb", "Fighter"));
        Api.Get(DraftPath("spell-allowance"), new SpellAllowanceApiResponse(false, 0, 0, 0, false, [], []));
        Api.Get(DraftPath("bonus-spells"), new List<BonusSpellChoiceApiResponse>());
        var cut = RenderStep();
        cut.Markup.Should().Contain("No Spells Available");

        // Act
        Button(cut, "Next: Review").Click();

        // Assert
        Api.Requests.Should().NotContain(Saved);
        _movedOn.Should().Be(1);
    }

    [Fact]
    public void Next_WhenTheServerRefusesTheSpells_ShowsItsMessageAndStays()
    {
        // Arrange
        ArrangeWizard(new SpellAllowanceApiResponse(true, 1, 1, 1, false, [], WizardSpells));
        Api.RefusePut(DraftPath("spells"), "Shield is not on the Wizard spell list");
        var cut = RenderStep();
        SpellButton(cut, "Light").Click();
        SpellButton(cut, "Shield").Click();

        // Act
        Button(cut, "Next: Review").Click();

        // Assert
        cut.Find("[role=alert] p").TextContent.Should().Be("Failed to save spells: Shield is not on the Wizard spell list");
        _movedOn.Should().Be(0);
    }

    [Fact]
    public void ToggleSpell_WithPicksStillOpen_ShowsAnAmberCountOfWhatIsLeft()
    {
        // Arrange
        ArrangeWizard(new SpellAllowanceApiResponse(true, 2, 1, 1, false, [], WizardSpells));
        var cut = RenderStep();

        // Act
        SpellButton(cut, "Fire Bolt").Click();

        // Assert
        var incomplete = CountBox(cut, 0);
        incomplete.ClassList.Should().Contain("border-amber");
        Squash(incomplete.TextContent).Should().Be("1 of 2 chosen: choose 1 more");
        incomplete.QuerySelector("svg").Should().BeNull("only a complete count shows a check");
    }

    [Fact]
    public void ToggleSpell_UntilTheSectionIsFull_ShowsAGreenCountWithACheck()
    {
        // Arrange
        ArrangeWizard(new SpellAllowanceApiResponse(true, 2, 1, 1, false, [], WizardSpells));
        var cut = RenderStep();
        SpellButton(cut, "Fire Bolt").Click();

        // Act
        SpellButton(cut, "Light").Click();

        // Assert
        var complete = CountBox(cut, 0);
        complete.ClassList.Should().Contain("border-green");
        Squash(complete.TextContent).Should().Be("2 of 2 chosen");
        complete.QuerySelector("svg").Should().NotBeNull("a complete count shows its check");
    }

    [Fact]
    public void Render_ForACaster_ShowsTheSpellbookWithThePicksAndTheSlotsStillOpen()
    {
        // Arrange
        ArrangeWizard(new SpellAllowanceApiResponse(true, 2, 2, 1, false, [], WizardSpells));
        var cut = RenderStep();

        // Act
        SpellButton(cut, "Light").Click();
        SpellButton(cut, "Shield").Click();
        SpellButton(cut, "Sleep").Click();

        // Assert
        var sections = cut.FindAll("[data-testid=spellbook] [data-testid=spellbook-section]");
        sections.Select(s => s.QuerySelector("h4")!.TextContent.Trim()).Should().Equal("Cantrips", "Spells");
        sections[0].QuerySelectorAll("li").Select(li => li.TextContent.Trim()).Should().Equal("Light", "Not chosen yet");
        sections[1].QuerySelectorAll("li").Select(li => li.TextContent.Trim()).Should().Equal("Shield", "Sleep");
    }

    [Fact]
    public void Filter_ByASchool_ShowsOnlyThatSchoolsRows()
    {
        // Arrange
        ArrangeWizard(new SpellAllowanceApiResponse(true, 1, 1, 1, false, [], WizardSpells));
        var cut = RenderStep();

        // Act
        cut.FindAll("button.chip").Single(b => b.TextContent.Trim() == "Abjuration").Click();

        // Assert
        RowNames(cut).Should().Equal("Shield");
        cut.FindAll("button.chip").Single(b => b.TextContent.Trim() == "Abjuration").GetAttribute("aria-pressed").Should().Be("true");
    }

    [Fact]
    public void Next_ForARaceCantripCastWithAnAbilityToPick_WaitsForTheAbilityAndSavesIt()
    {
        // Arrange - a 2024 high elf's wizard cantrip, cast with Intelligence, Wisdom or Charisma
        ArrangeWizard(new SpellAllowanceApiResponse(true, 1, 1, 1, false, [], WizardSpells), [HighElfCantrip()]);
        Api.Put(DraftPath("spells"));
        Api.Put(DraftPath("bonus-spells"));
        var cut = RenderStep();
        SpellButton(cut, "Light").Click();
        SpellButton(cut, "Shield").Click();
        SpellButton(cut, "Ray of Frost").Click();
        Button(cut, "Next: Review").HasAttribute("disabled").Should().BeTrue();

        // Act
        cut.Find("select[aria-label='Spellcasting ability']").Change("WIS");
        Button(cut, "Next: Review").Click();

        // Assert
        var saved = Api.BodyOf<ChooseBonusSpellsApiRequest>(SaveRoute("bonus-spells"));
        saved.RaceSpellIds.Should().Equal("ray-of-frost-phb");
        saved.RaceSpellAbilityId.Should().Be("WIS");
        _movedOn.Should().Be(1);
    }

    [Fact]
    public void Render_ForARaceCantripWithItsAbilityPicked_ShowsThePick()
    {
        // Arrange - coming back to the step, or editing a saved character
        var draft = Draft("wizard-phb");
        draft.RaceSpellIds = ["ray-of-frost-phb"];
        draft.RaceSpellAbilityId = "CHA";
        ArrangeWizard(new SpellAllowanceApiResponse(true, 1, 1, 1, false, [], WizardSpells), [HighElfCantrip()], draft);

        // Act
        var cut = RenderStep();

        // Assert
        cut.Find("select[aria-label='Spellcasting ability']").GetAttribute("value").Should().Be("CHA");
    }

    private static BonusSpellChoiceApiResponse HighElfCantrip() => new(
        null, "High Elf", 1, 0, [new BonusSpellListApiResponse("wizard-phb", "Wizard", [Spell("ray-of-frost-phb", "Ray of Frost", 0)])], ["INT", "WIS", "CHA"]);

    private static CharacterDraftDetailsApiResponse Draft(string classId, string? subclassId = null) => new()
    {
        Id = DraftId,
        Level = 1,
        Edition = "2014",
        AllowedSources = ["PHB"],
        ClassId = classId,
        SubclassId = subclassId,
    };

    private static string Squash(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static List<string> Counts(IRenderedComponent<SpellsStep> cut) =>
        cut.FindAll("[data-testid=choice-count]").Select(c => Squash(c.TextContent)).ToList();

    private static IElement CountBox(IRenderedComponent<SpellsStep> cut, int index) =>
        cut.FindAll("[data-testid=choice-count]")[index];

    private static List<string> RowNames(IRenderedComponent<SpellsStep> cut) =>
        cut.FindAll("[data-testid=choice-row] span.font-semibold").Select(s => s.TextContent.Trim()).ToList();
    private static IElement SpellButton(IRenderedComponent<SpellsStep> cut, string name) =>
        cut.FindAll("[data-testid=choice-row]").Single(b => b.QuerySelector("span.font-semibold")?.TextContent.Trim() == name);

    private void ArrangeWizard(
        SpellAllowanceApiResponse allowance, List<BonusSpellChoiceApiResponse>? bonusChoices = null, CharacterDraftDetailsApiResponse? draft = null)
    {
        Api.Get(DraftPath(), draft ?? Draft("wizard-phb"));
        Api.Get("/api/classes/wizard-phb", Class("wizard-phb", "Wizard"));
        Api.Get(DraftPath("spell-allowance"), allowance);
        Api.Get(DraftPath("bonus-spells"), bonusChoices ?? []);
    }

    private IRenderedComponent<SpellsStep> RenderStep()
    {
        var cut = Render<SpellsStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 9)
            .Add(p => p.OnNext, () => _movedOn++));

        cut.WaitForAssertion(() => cut.Markup.Should().NotContain("Loading spell options..."));
        return cut;
    }
}
