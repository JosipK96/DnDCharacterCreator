using Bunit;

using CharacterCreatorApp.Client.Pages.CharacterCreation;
using CharacterCreatorApp.Client.Pages.CharacterCreation.Components;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Responses.Characters;

using FluentAssertions;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

/// <summary>The review step's spellcasting numbers, as the preview sends them (the sheet shows them through the same panel).</summary>
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

    private IRenderedComponent<ReviewStep> RenderStep()
    {
        var cut = Render<ReviewStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, WizardSteps.Review));

        cut.WaitForAssertion(() => cut.FindAll("div.animate-spin").Should().BeEmpty());
        return cut;
    }

    private static CharacterPreviewApiResponse Preview(List<SpellcastingStatApiResponse> spellcasting) => new(
        "Elminster", 1, "2014", "Human", null, "Wizard", null, "Sage", "Medium", [], [], [], [], [],
        new CharacterStatsApiResponse(2, 8, 12, 2, 30, "1d6", 11, [], [], [], [], null, spellcasting, []),
        null);
}
