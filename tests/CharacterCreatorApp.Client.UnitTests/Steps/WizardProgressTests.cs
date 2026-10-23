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
    public void Render_InALevelUp_ShowsOnlyTheHitPointsWhatTheLevelBringsAndTheReview()
    {
        // Arrange - a wizard's level-up to 4: an improvement and spells; the hit points still to pick
        var flow = WizardSteps.LevelUpOf(["hit-points", "spells", "improvements"]);

        // Act
        var cut = Render<WizardProgress>(parameters => parameters
            .Add(p => p.CurrentStep, 8)
            .Add(p => p.Flow, flow)
            .Add(p => p.StepsNeedingPick, WizardSteps.StepsOf(["hit-points"]).ToHashSet())
            .Add(p => p.OnSelectStep, _ => { }));

        // Assert - numbered as shown, in the order picked: the hit points first
        flow.Should().Equal(WizardSteps.HitPoints, 8, 9, 10);
        cut.FindAll("button").Select(b => b.GetAttribute("title"))
            .Should().Equal("Hit Points: needs a pick", "Improvements", "Spells", "Review");
        cut.FindAll("button")[2].TextContent.Trim().Should().Be("3");
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
