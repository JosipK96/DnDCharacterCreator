using AngleSharp.Dom;

using Bunit;

using CharacterCreatorApp.Client.Pages.CharacterCreation.Components;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Requests.CharacterDrafts;
using CharacterCreatorApp.Contracts.Responses;

using FluentAssertions;

using static CharacterCreatorApp.Client.UnitTests.Fixtures.TestData;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

public class SpellsStepTests : WizardStepTestBase
{
    private const string SaveRoute = "PUT /api/character-drafts/0b6f3a52-7d2e-4c1a-9a51-3f0c2d7e8b14/spells";

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
        ArrangeWizard(new SpellAllowanceApiResponse(true, 3, 6, 1, false, []));

        // Act
        var cut = RenderStep();

        // Assert
        cut.Markup.Should().Contain("Choose 3 cantrips and 6 spells.");
        cut.FindAll("div.text-3xl").Select(d => Squash(d.TextContent)).Should().Equal("0 / 3", "0 / 6");
        cut.Markup.Should().NotContain("Prepared Spells");
    }

    [Fact]
    public void ToggleCantrip_AtTheAllowedCount_DisablesTheOthers()
    {
        // Arrange
        ArrangeWizard(new SpellAllowanceApiResponse(true, 2, 1, 1, false, []));
        var cut = RenderStep();

        // Act
        SpellButton(cut, "Fire Bolt").Click();
        SpellButton(cut, "Light").Click();

        // Assert
        SpellButton(cut, "Mage Hand").HasAttribute("disabled").Should().BeTrue();
        SpellButton(cut, "Fire Bolt").HasAttribute("disabled").Should().BeFalse();
        Squash(cut.FindAll("div.text-3xl")[0].TextContent).Should().Be("2 / 2");
    }

    [Fact]
    public void Next_WithExactlyTheAllowedPicks_SavesThemAndMovesOn()
    {
        // Arrange
        ArrangeWizard(new SpellAllowanceApiResponse(true, 1, 2, 1, false, []));
        Api.Put(DraftPath("spells"));
        var cut = RenderStep();
        SpellButton(cut, "Mage Hand").Click();
        SpellButton(cut, "Shield").Click();
        Button(cut, "Next: Review").HasAttribute("disabled").Should().BeTrue();

        // Act
        SpellButton(cut, "Sleep").Click();
        Button(cut, "Next: Review").Click();

        // Assert
        Api.BodyOf<ChooseSpellsApiRequest>(SaveRoute).SpellIds.Should().Equal("mage-hand-phb", "shield-phb", "sleep-phb");
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
        Api.Get(DraftPath("spell-allowance"), new SpellAllowanceApiResponse(true, 3, 4, 1, true, ["bless-phb", "cure-wounds-phb"]));
        Api.Get("/api/spells?classId=cleric-phb&maxLevel=1&sources=PHB", new List<SpellApiResponse>
        {
            Spell("guidance-phb", "Guidance", 0), Spell("bless-phb", "Bless", 1), Spell("cure-wounds-phb", "Cure Wounds", 1),
            Spell("guiding-bolt-phb", "Guiding Bolt", 1), Spell("healing-word-phb", "Healing Word", 1),
        });
        Api.Get("/api/spells?subclassId=life-domain-phb&maxLevel=1&sources=PHB", new List<SpellApiResponse>
        {
            Spell("bless-phb", "Bless", 1), Spell("cure-wounds-phb", "Cure Wounds", 1),
        });
        Api.Get(DraftPath("bonus-spells"), new List<BonusSpellChoiceApiResponse>());

        // Act
        var cut = RenderStep();

        // Assert
        var note = Squash(cut.Find("div.bg-blue-900").TextContent);
        note.Should().Contain("Cleric prepares 4 spells from the Cleric spell list");
        note.Should().Contain("Your subclass's spells are always prepared on top of these: Bless, Cure Wounds.");
        cut.FindAll("h5").Select(h => h.TextContent.Trim()).Should().Equal("Guidance", "Guiding Bolt", "Healing Word");
    }

    [Fact]
    public void Next_ForANonCaster_SaysSoAndMovesOnWithoutSavingSpells()
    {
        // Arrange
        Api.Get(DraftPath(), Draft("fighter-phb"));
        Api.Get("/api/classes/fighter-phb", Class("fighter-phb", "Fighter"));
        Api.Get(DraftPath("spell-allowance"), new SpellAllowanceApiResponse(false, 0, 0, 0, false, []));
        Api.Get(DraftPath("bonus-spells"), new List<BonusSpellChoiceApiResponse>());
        var cut = RenderStep();
        cut.Markup.Should().Contain("No Spells Available");

        // Act
        Button(cut, "Next: Review").Click();

        // Assert
        Api.Requests.Should().NotContain(SaveRoute);
        _movedOn.Should().Be(1);
    }

    [Fact]
    public void Next_WhenTheServerRefusesTheSpells_ShowsItsMessageAndStays()
    {
        // Arrange
        ArrangeWizard(new SpellAllowanceApiResponse(true, 1, 1, 1, false, []));
        Api.RefusePut(DraftPath("spells"), "Shield is not on the Wizard spell list");
        var cut = RenderStep();
        SpellButton(cut, "Light").Click();
        SpellButton(cut, "Shield").Click();

        // Act
        Button(cut, "Next: Review").Click();

        // Assert
        cut.Find("div.bg-red-900 p").TextContent.Should().Be("Failed to save spells: Shield is not on the Wizard spell list");
        _movedOn.Should().Be(0);
    }

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

    private static IElement SpellButton(IRenderedComponent<SpellsStep> cut, string name) =>
        cut.FindAll("button").Single(b => b.QuerySelector("h5")?.TextContent.Trim() == name);

    private void ArrangeWizard(SpellAllowanceApiResponse allowance)
    {
        Api.Get(DraftPath(), Draft("wizard-phb"));
        Api.Get("/api/classes/wizard-phb", Class("wizard-phb", "Wizard"));
        Api.Get(DraftPath("spell-allowance"), allowance);
        Api.Get("/api/spells?classId=wizard-phb&maxLevel=1&sources=PHB", WizardSpells);
        Api.Get(DraftPath("bonus-spells"), new List<BonusSpellChoiceApiResponse>());
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
