using Bunit;

using CharacterCreatorApp.Client.Pages.CharacterCreation;
using CharacterCreatorApp.Client.Pages.CharacterCreation.Components;

using FluentAssertions;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

public class WizardProgressTests : BunitContext
{
    private const string MapRooms = "[data-testid=progress-map] [data-state]";
    private const string CompactSquares = "[data-testid=progress-compact] [data-state]";

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
        cut.FindAll("[data-testid=progress-map] button").Single(b => b.GetAttribute("title") == "Spells: needs a pick").Click();

        // Assert
        steps.Should().BeEquivalentTo([6, 9]);
        cut.FindAll("[data-testid=progress-map] button[data-state=needs-pick]")
            .Select(b => b.GetAttribute("title"))
            .Should().Equal("Ability Scores: needs a pick", "Spells: needs a pick");
        chosen.Should().Be(9);
    }

    [Fact]
    public void Render_InAnEdit_GoesToAStepClickedInTheCompactForm()
    {
        // Arrange
        var chosen = 0;

        // Act
        var cut = Render<WizardProgress>(parameters => parameters
            .Add(p => p.CurrentStep, 4)
            .Add(p => p.OnSelectStep, step => chosen = step));
        cut.FindAll("[data-testid=progress-compact] button").Single(b => b.GetAttribute("aria-label") == "Languages").Click();

        // Assert
        chosen.Should().Be(5);
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
        var rooms = cut.FindAll("[data-testid=progress-map] button");
        rooms.Select(b => b.GetAttribute("title"))
            .Should().Equal("Hit Points: needs a pick", "Improvements", "Spells", "Review");
        rooms[2].TextContent.Trim().Should().Be("3");
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

    [Fact]
    public void Render_DrawsEachRoomInItsState()
    {
        // Act - on Class, with Spells still needing a pick
        var cut = Render<WizardProgress>(parameters => parameters
            .Add(p => p.CurrentStep, 3)
            .Add(p => p.StepsNeedingPick, new HashSet<int> { 9 }));

        // Assert - the rooms walked are done, Class is where the player is, Spells needs a pick
        var rooms = cut.FindAll(MapRooms);
        rooms.Select(r => r.GetAttribute("data-state")).Should().Equal(
            "done", "done", "current", "ahead", "ahead", "ahead", "ahead", "ahead", "needs-pick", "ahead");
        rooms[0].ClassList.Should().Contain("hatch");
        rooms[0].TextContent.Trim().Should().Be("1");
        rooms[2].ClassList.Should().Contain(["bg-plum-tint", "border-plum", "border-[5px]"]);
        rooms[2].GetAttribute("aria-current").Should().Be("step");
        rooms[3].ClassList.Should().Contain("border-dashed");
        rooms[8].ClassList.Should().Contain(["bg-amber-tint", "border-amber"]);
        rooms[8].TextContent.Trim().Should().Be("!");
        rooms.Where(r => r.GetAttribute("aria-current") is not null).Should().ContainSingle();
    }

    [Fact]
    public void Render_TheCompactForm_NamesTheRoomTheNextOneAndHasASquarePerStep()
    {
        // Act
        var cut = Render<WizardProgress>(parameters => parameters
            .Add(p => p.CurrentStep, 3)
            .Add(p => p.StepsNeedingPick, new HashSet<int> { 9 }));

        // Assert
        var compact = cut.Find("[data-testid=progress-compact]");
        compact.QuerySelector("p")!.TextContent.Should().Be("Room 3 of 10: Class");
        compact.QuerySelectorAll("p")[1].TextContent.Should().Be("Next: Background");
        cut.FindAll(CompactSquares).Select(s => s.GetAttribute("data-state")).Should().Equal(
            "done", "done", "current", "ahead", "ahead", "ahead", "ahead", "ahead", "needs-pick", "ahead");
    }

    [Fact]
    public void Render_TheCompactFormOnTheLastStep_HasNoNextRoom()
    {
        // Arrange
        var flow = WizardSteps.LevelUpOf(["hit-points"]);

        // Act
        var cut = Render<WizardProgress>(parameters => parameters
            .Add(p => p.CurrentStep, WizardSteps.Review)
            .Add(p => p.Flow, flow));

        // Assert
        var compact = cut.Find("[data-testid=progress-compact]");
        compact.QuerySelector("p")!.TextContent.Should().Be("Room 2 of 2: Review");
        compact.QuerySelectorAll("p").Should().ContainSingle();
    }
}
