using Bunit;

using CharacterCreatorApp.Client.Pages.CharacterCreation;
using CharacterCreatorApp.Client.Pages.CharacterCreation.Components;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Responses.Characters;

using FluentAssertions;

using static CharacterCreatorApp.Client.UnitTests.Fixtures.TestData;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

/// <summary>
/// The review step: the preview as the API builds it (the sheet shows its spellcasting numbers
/// through the same panel), the blocking issue that keeps Create disabled, and a refused preview.
/// </summary>
public class ReviewStepTests : WizardStepTestBase
{
    [Fact]
    public void Render_ACaster_ShowsItsSpellSaveDcAndAttackBonusLabelled()
    {
        // Arrange - a level 1 wizard with Intelligence 16
        Api.Get(DraftPath("preview"), Preview([new("Wizard", "INT", 13, 5)]));

        // Act
        var cut = RenderStep();

        // Assert
        var row = cut.Find("[data-testid='spellcasting']");
        row.TextContent.Should().Contain("Wizard, with Intelligence");
        row.QuerySelector("[data-testid='save-dc']")!.TextContent.Should().Be("13");
        row.QuerySelector("[data-testid='attack-bonus']")!.TextContent.Should().Be("+5");
    }

    [Fact]
    public void Render_ANonCaster_ShowsNoSpellcastingNumbers()
    {
        // Arrange
        Api.Get(DraftPath("preview"), Preview([]));

        // Act
        var cut = RenderStep();

        // Assert
        cut.FindAll("[data-testid='spellcasting']").Should().BeEmpty();
    }

    [Fact]
    public void Render_AFullPreview_ShowsTheCharactersNumbersAndChoices()
    {
        // Arrange
        Api.Get(DraftPath("preview"), Preview([]) with
        {
            SubraceName = "High Elf",
            SubclassName = "School of Evocation",
            Languages = [new("elvish", "Elvish", "Standard"), new("common", "Common", "Standard")],
            Feats = [new CharacterFeatApiResponse(Feat("magic-initiate", "Magic Initiate"), [Spell("light", "Light", 0)], "INT")],
            Spells = [Spell("shield", "Shield", 1), Spell("fire-bolt", "Fire Bolt", 0)],
            Features = [new("arcane-recovery", "Arcane Recovery", "Regain slots.")],
        });

        // Act
        var cut = RenderStep();

        // Assert
        cut.Find("h3.font-display").TextContent.Should().Be("Elminster");
        Squash(cut.Markup).Should().Contain("Level 1 Wizard (School of Evocation)")
            .And.Contain("Human (High Elf) • Medium • Sage • 2014 rules")
            .And.Contain("Common, Elvish")
            .And.Contain("Fire Bolt, Shield")
            .And.Contain("Arcane Recovery")
            .And.Contain("Magic Initiate");
        cut.Markup.Should().Contain("No skill proficiencies.");
    }

    [Fact]
    public void Render_WithABlockingIssue_SaysWhatIsMissingAndKeepsCreateDisabled()
    {
        // Arrange
        Api.Get(DraftPath("preview"), Preview([]) with { BlockingIssue = "Pick 2 more spells." });

        // Act
        var cut = RenderStep();

        // Assert
        cut.Find("[role=status]").TextContent.Should().Contain("Pick 2 more spells.");
        Button(cut, "Create Character").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Click_CreateCharacter_HandsTheDraftToTheWizard()
    {
        // Arrange
        Api.Get(DraftPath("preview"), Preview([]));
        var nexts = 0;
        var cut = RenderStep(onNext: () => nexts++);

        // Act
        Button(cut, "Create Character").Click();

        // Assert
        cut.WaitForAssertion(() => nexts.Should().Be(1));
    }

    [Fact]
    public void Render_AnEdit_OffersToSaveTheChanges()
    {
        // Arrange
        Api.Get(DraftPath("preview"), Preview([]));

        // Act
        var cut = RenderStep(isEdit: true);

        // Assert
        Buttons(cut, "Save Changes").Should().ContainSingle();
        Buttons(cut, "Create Character").Should().BeEmpty();
    }

    [Fact]
    public void Render_WhenTheApiRefusesThePreview_ShowsItsReasonAndKeepsCreateDisabled()
    {
        // Arrange
        Api.NotFound(DraftPath("preview"));

        // Act
        var cut = RenderStep();

        // Assert
        cut.Find("[role=alert]").TextContent.Should().Contain("Failed to load the preview");
        Button(cut, "Create Character").HasAttribute("disabled").Should().BeTrue();
    }

    private static string Squash(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private IRenderedComponent<ReviewStep> RenderStep(bool isEdit = false, Action? onNext = null)
    {
        var cut = Render<ReviewStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, WizardSteps.Review)
            .Add(p => p.IsEdit, isEdit)
            .Add(p => p.OnNext, () => onNext?.Invoke()));

        cut.WaitForAssertion(() => cut.FindAll("div.animate-spin").Should().BeEmpty());
        return cut;
    }

    private static CharacterPreviewApiResponse Preview(List<SpellcastingStatApiResponse> spellcasting) => new(
        "Elminster", 1, "2014", "Human", null, "Wizard", null, "Sage", "Medium", [], [], [], [], [],
        new CharacterStatsApiResponse(2, 8, 12, 2, 30, "1d6", 11, [], [], [], [], null, spellcasting, []),
        null);
}
