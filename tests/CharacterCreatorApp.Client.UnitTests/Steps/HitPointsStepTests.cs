using Bunit;

using CharacterCreatorApp.Client.Pages.CharacterCreation;
using CharacterCreatorApp.Client.Pages.CharacterCreation.Components;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Requests.CharacterDrafts;
using CharacterCreatorApp.Contracts.Responses;

using FluentAssertions;

using static CharacterCreatorApp.Client.UnitTests.Fixtures.TestData;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

/// <summary>A level-up's first step: the hit die's fixed value, a roll here, or a roll with real dice.</summary>
public class HitPointsStepTests : WizardStepTestBase
{
    private static readonly string HitPointsRoute = SaveRoute("hit-points");

    // A fighter levelling up to 5, which brings an improvement after the hit points
    private static readonly IReadOnlyList<int> Flow = WizardSteps.LevelUpOf(["hit-points", "improvements"]);

    private int _movedOn;

    [Fact]
    public void Render_ForAFighter_OffersTheD10sFixedValueAndHeadsTheLevelUpsFirstStep()
    {
        // Arrange
        ArrangeLoad(Draft());

        // Act
        var cut = RenderStep();

        // Assert
        cut.Find("h2").TextContent.Should().Be("Step 1: Hit Points");
        Button(cut, "Take 6").Should().NotBeNull();
        Button(cut, "Roll a d10").Should().NotBeNull();
        Button(cut, "Next: Improvements").HasAttribute("disabled").Should().BeTrue("nothing is picked yet");
    }

    [Fact]
    public void Next_AfterTakingTheFixedValue_SavesItAndMovesOn()
    {
        // Arrange
        ArrangeLoad(Draft());
        Api.Put(DraftPath("hit-points"));
        var cut = RenderStep();

        // Act
        Button(cut, "Take 6").Click();
        Button(cut, "Next: Improvements").Click();

        // Assert
        Api.BodyOf<SetHitPointsApiRequest>(HitPointsRoute).Result.Should().Be(6);
        _movedOn.Should().Be(1);
    }

    [Fact]
    public void Roll_RollsTheDieOnceAndSavesWhatCameUp()
    {
        // Arrange
        ArrangeLoad(Draft());
        Api.Put(DraftPath("hit-points"));
        var cut = RenderStep();

        // Act
        Button(cut, "Roll a d10").Click();
        var rollButton = Button(cut, "Rolled once");
        Button(cut, "Next: Improvements").Click();

        // Assert - the button's first line says what came up ("Rolled 7")
        var rolled = int.Parse(rollButton.QuerySelector("span")!.TextContent["Rolled ".Length..], System.Globalization.CultureInfo.InvariantCulture);
        rolled.Should().BeInRange(1, 10);
        rollButton.HasAttribute("disabled").Should().BeTrue("the die is rolled once");
        Api.BodyOf<SetHitPointsApiRequest>(HitPointsRoute).Result.Should().Be(rolled);
    }

    [Fact]
    public void Next_WithARollEnteredFromRealDice_SavesIt()
    {
        // Arrange
        ArrangeLoad(Draft());
        Api.Put(DraftPath("hit-points"));
        var cut = RenderStep();

        // Act
        cut.Find("#hit-die-roll").Change("9");
        Button(cut, "Next: Improvements").Click();

        // Assert
        Api.BodyOf<SetHitPointsApiRequest>(HitPointsRoute).Result.Should().Be(9);
    }

    [Fact]
    public void Enter_ARollTheDieCannotMake_SaysSoAndKeepsNextDisabled()
    {
        // Arrange
        ArrangeLoad(Draft());
        var cut = RenderStep();

        // Act
        cut.Find("#hit-die-roll").Change("11");

        // Assert
        cut.Markup.Should().Contain("A d10 rolls from 1 to 10.");
        Button(cut, "Next: Improvements").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Render_WithAResultSaved_ShowsItAndSavesItUnchanged()
    {
        // Arrange - coming back to the step after a 3 was entered
        var draft = Draft();
        draft.HitDieResults = new Dictionary<int, int> { [5] = 3 };
        ArrangeLoad(draft);
        Api.Put(DraftPath("hit-points"));
        var cut = RenderStep();

        // Act
        Button(cut, "Next: Improvements").Click();

        // Assert
        cut.Find("#hit-die-roll").GetAttribute("value").Should().Be("3");
        Api.BodyOf<SetHitPointsApiRequest>(HitPointsRoute).Result.Should().Be(3);
    }

    private static CharacterDraftDetailsApiResponse Draft() => new()
    {
        Id = DraftId,
        Level = 5,
        Edition = "2014",
        ClassId = "fighter-phb",
        LevelUpChoices = ["hit-points", "improvements"],
    };

    private void ArrangeLoad(CharacterDraftDetailsApiResponse draft)
    {
        Api.Get(DraftPath(), draft);
        Api.Get("/api/classes/fighter-phb", Class("fighter-phb", "Fighter", 4, 6, 8));
    }

    private IRenderedComponent<HitPointsStep> RenderStep()
    {
        var cut = Render<HitPointsStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, WizardSteps.HitPoints)
            .Add(p => p.Flow, Flow)
            .Add(p => p.OnNext, () => _movedOn++));

        cut.WaitForAssertion(() => cut.FindAll("div.animate-spin").Should().BeEmpty());
        return cut;
    }
}
