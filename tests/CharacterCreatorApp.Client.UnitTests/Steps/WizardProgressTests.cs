using Bunit;

using CharacterCreatorApp.Client.Pages.CharacterCreation;
using CharacterCreatorApp.Client.Pages.CharacterCreation.Components;

using FluentAssertions;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

public class WizardProgressTests : BunitContext
{
    [Fact]
    public void Render_InAnEdit_MarksTheStepsNeedingAPickAndGoesToAStepClicked()
    {
        // Arrange - an older character whose scores and spells were not kept
        var steps = WizardSteps.StepsOf(["spells", "ability-scores", "not-a-step"]).ToHashSet();
        var chosen = 0;

        // Act
        var cut = Render<WizardProgress>(parameters => parameters
            .Add(p => p.CurrentStep, 1)
            .Add(p => p.StepsNeedingPick, steps)
            .Add(p => p.OnSelectStep, step => chosen = step));
        cut.FindAll("button").Single(b => b.GetAttribute("title") == "Spells: needs a pick").Click();

        // Assert
        steps.Should().BeEquivalentTo([6, 9]);
        cut.FindAll("button").Where(b => b.GetAttribute("title")!.EndsWith("needs a pick", StringComparison.Ordinal))
            .Select(b => b.GetAttribute("title"))
            .Should().Equal("Ability Scores: needs a pick", "Spells: needs a pick");
        chosen.Should().Be(9);
    }

    [Fact]
    public void Render_ForANewCharacter_HasNoStepToClick()
    {
        // Act
        var cut = Render<WizardProgress>(parameters => parameters.Add(p => p.CurrentStep, 3));

        // Assert
        cut.FindAll("button").Should().BeEmpty();
        cut.Markup.Should().NotContain("Needs a pick");
    }
}
