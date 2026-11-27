using AngleSharp.Dom;

using Bunit;

using CharacterCreatorApp.Client.Pages.CharacterCreation;
using CharacterCreatorApp.Client.Pages.CharacterCreation.Components;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Requests.CharacterDrafts;
using CharacterCreatorApp.Contracts.Responses.Characters;

using FluentAssertions;

using static CharacterCreatorApp.Client.UnitTests.Fixtures.TestData;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

/// <summary>
/// The hit die for each level after the first: its fixed value, a roll here, or a roll with real
/// dice. A level-up's first step asks for its new level; a new character above level 1 asks for
/// every level from 2, before Review.
/// </summary>
public class HitPointsStepTests : WizardStepTestBase
{
    private static readonly string HitPointsRoute = SaveRoute("hit-points");

    // A fighter levelling up to 5, which brings an improvement after the hit points
    private static readonly IReadOnlyList<int> LevelUpFlow = WizardSteps.LevelUpOf(["hit-points", "improvements"]);

    // A new fighter created at level 3
    private static readonly IReadOnlyList<int> CreationFlow = WizardSteps.CreationOf([2, 3]);

    private int _movedOn;

    [Fact]
    public void Render_ForAFighter_OffersTheD10sFixedValueAndHeadsTheLevelUpsFirstStep()
    {
        // Arrange
        ArrangeLoad(LevelUp());

        // Act
        var cut = RenderStep(LevelUpFlow);

        // Assert
        cut.Find("h2").TextContent.Should().Be("Step 1: Hit Points");
        Button(cut, "Take 6").Should().NotBeNull();
        Button(cut, "Roll a d10").Should().NotBeNull();
        cut.FindAll("[data-testid^='hit-die-level-']").Should().ContainSingle();
        Button(cut, "Next: Improvements").HasAttribute("disabled").Should().BeTrue("nothing is picked yet");
    }

    [Fact]
    public void Next_AfterTakingTheFixedValue_SavesItAndMovesOn()
    {
        // Arrange
        ArrangeLoad(LevelUp());
        Api.Put(DraftPath("hit-points"));
        var cut = RenderStep(LevelUpFlow);

        // Act
        Button(cut, "Take 6").Click();
        Button(cut, "Next: Improvements").Click();

        // Assert
        Api.BodyOf<SetHitPointsApiRequest>(HitPointsRoute).Results.Should().Equal(new Dictionary<int, int> { [5] = 6 });
        _movedOn.Should().Be(1);
    }

    [Fact]
    public void Roll_RollsTheDieOnceAndSavesWhatCameUp()
    {
        // Arrange
        ArrangeLoad(LevelUp());
        Api.Put(DraftPath("hit-points"));
        var cut = RenderStep(LevelUpFlow);

        // Act
        Button(cut, "Roll a d10").Click();
        var rollButton = Button(cut, "Rolled once");
        Button(cut, "Next: Improvements").Click();

        // Assert - the room's name says what came up ("Rolled 7")
        var rolled = RolledOn(rollButton);
        rolled.Should().BeInRange(1, 10);
        rollButton.HasAttribute("disabled").Should().BeTrue("the die is rolled once");
        Api.BodyOf<SetHitPointsApiRequest>(HitPointsRoute).Results.Should().Equal(new Dictionary<int, int> { [5] = rolled });
    }

    [Fact]
    public void Next_WithARollEnteredFromRealDice_SavesIt()
    {
        // Arrange
        ArrangeLoad(LevelUp());
        Api.Put(DraftPath("hit-points"));
        var cut = RenderStep(LevelUpFlow);

        // Act
        cut.Find("#hit-die-roll-5").Change("9");
        Button(cut, "Next: Improvements").Click();

        // Assert
        Api.BodyOf<SetHitPointsApiRequest>(HitPointsRoute).Results.Should().Equal(new Dictionary<int, int> { [5] = 9 });
    }

    [Fact]
    public void Enter_ARollTheDieCannotMake_SaysSoAndKeepsNextDisabled()
    {
        // Arrange
        ArrangeLoad(LevelUp());
        var cut = RenderStep(LevelUpFlow);

        // Act
        cut.Find("#hit-die-roll-5").Change("11");

        // Assert
        cut.Markup.Should().Contain("A d10 rolls from 1 to 10.");
        Button(cut, "Next: Improvements").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Render_WithAResultSaved_ShowsItAndSavesItUnchanged()
    {
        // Arrange - coming back to the step after a 3 was entered
        var draft = LevelUp();
        draft.HitDieResults = new Dictionary<int, int> { [5] = 3 };
        ArrangeLoad(draft);
        Api.Put(DraftPath("hit-points"));
        var cut = RenderStep(LevelUpFlow);

        // Act
        Button(cut, "Next: Improvements").Click();

        // Assert
        cut.Find("#hit-die-roll-5").GetAttribute("value").Should().Be("3");
        Api.BodyOf<SetHitPointsApiRequest>(HitPointsRoute).Results.Should().Equal(new Dictionary<int, int> { [5] = 3 });
    }

    [Fact]
    public void Render_ForANewCharacterAtLevelThree_SaysLevelOneIsTheMaximumAndOffersLevelsTwoAndThree()
    {
        // Arrange
        ArrangeLoad(NewCharacter());

        // Act
        var cut = RenderStep(CreationFlow);

        // Assert
        cut.Find("h2").TextContent.Should().Be("Step 10: Hit Points");
        cut.Find("div.text-pencil").TextContent.Should().Contain("Level 1 takes the d10's maximum, 10");
        cut.FindAll("[data-testid^='hit-die-level-']").Select(level => level.GetAttribute("data-testid"))
            .Should().Equal("hit-die-level-2", "hit-die-level-3");
        Button(cut, "Next: Review").HasAttribute("disabled").Should().BeFalse("a level left alone takes the fixed value");
    }

    [Fact]
    public void Next_ForANewCharacter_SavesThePickedLevelsAndLeavesTheRestToTheFixedValue()
    {
        // Arrange
        ArrangeLoad(NewCharacter());
        Api.Put(DraftPath("hit-points"));
        var cut = RenderStep(CreationFlow);

        // Act - level 2 rolled with real dice, level 3 left alone
        cut.Find("#hit-die-roll-2").Change("2");
        Button(cut, "Next: Review").Click();

        // Assert
        cut.Find("[data-testid='hit-die-level-3']").TextContent.Should().Contain("Left alone, level 3 takes the fixed value, 6.");
        Api.BodyOf<SetHitPointsApiRequest>(HitPointsRoute).Results.Should().Equal(new Dictionary<int, int> { [2] = 2 });
        _movedOn.Should().Be(1);
    }

    [Fact]
    public void Roll_ForANewCharacter_RollsOnlyTheLevelItIsFor()
    {
        // Arrange
        ArrangeLoad(NewCharacter());
        Api.Put(DraftPath("hit-points"));
        var cut = RenderStep(CreationFlow);

        // Act
        Button(LevelRoom(cut, 3), "Roll a d10").Click();
        Button(LevelRoom(cut, 2), "Take 6").Click();
        Button(cut, "Next: Review").Click();

        // Assert
        var rolled = RolledOn(Button(LevelRoom(cut, 3), "Rolled once"));
        Button(LevelRoom(cut, 2), "Roll a d10").HasAttribute("disabled").Should().BeFalse("level 2 was not rolled");
        Api.BodyOf<SetHitPointsApiRequest>(HitPointsRoute).Results.Should().Equal(new Dictionary<int, int> { [2] = 6, [3] = rolled });
    }

    [Fact]
    public void Enter_ForANewCharacterARollTheDieCannotMake_KeepsNextDisabled()
    {
        // Arrange - leaving the level alone is fine, but a wrong roll is not taken as the fixed value
        ArrangeLoad(NewCharacter());
        var cut = RenderStep(CreationFlow);

        // Act
        cut.Find("#hit-die-roll-3").Change("12");

        // Assert
        cut.Markup.Should().Contain("A d10 rolls from 1 to 10.");
        Button(cut, "Next: Review").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Render_ForANewCharacterWithResultsSaved_ShowsEachOnItsLevel()
    {
        // Arrange
        var draft = NewCharacter();
        draft.HitDieResults = new Dictionary<int, int> { [2] = 6, [3] = 4 };
        ArrangeLoad(draft);

        // Act
        var cut = RenderStep(CreationFlow);

        // Assert
        Button(LevelRoom(cut, 2), "Take 6").GetAttribute("aria-pressed").Should().Be("true");
        cut.Find("#hit-die-roll-3").GetAttribute("value").Should().Be("4");
    }

    private static CharacterDraftDetailsApiResponse LevelUp() => new()
    {
        Id = DraftId,
        Level = 5,
        Edition = "2014",
        ClassId = "fighter-phb",
        LevelUpChoices = ["hit-points", "improvements"],
        HitDieLevels = [5],
    };

    private static CharacterDraftDetailsApiResponse NewCharacter() => new()
    {
        Id = DraftId,
        Level = 3,
        Edition = "2014",
        ClassId = "fighter-phb",
        HitDieLevels = [2, 3],
    };

    private static int RolledOn(IElement rollButton) =>
        int.Parse(rollButton.QuerySelector("span.font-display")!.TextContent["Rolled ".Length..], System.Globalization.CultureInfo.InvariantCulture);

    private static IElement LevelRoom(IRenderedComponent<HitPointsStep> cut, int level) =>
        cut.Find($"[data-testid='hit-die-level-{level}']");

    private static IElement Button(IElement room, string text) =>
        room.QuerySelectorAll("button").First(button => button.TextContent.Contains(text, StringComparison.Ordinal));

    private void ArrangeLoad(CharacterDraftDetailsApiResponse draft)
    {
        Api.Get(DraftPath(), draft);
        Api.Get("/api/classes/fighter-phb", Class("fighter-phb", "Fighter", 4, 6, 8));
    }

    private IRenderedComponent<HitPointsStep> RenderStep(IReadOnlyList<int> flow)
    {
        var cut = Render<HitPointsStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, WizardSteps.HitPoints)
            .Add(p => p.Flow, flow)
            .Add(p => p.OnNext, () => _movedOn++));

        cut.WaitForAssertion(() => cut.FindAll("div.animate-spin").Should().BeEmpty());
        return cut;
    }
}
