using CharacterCreatorApp.Client.Pages.CharacterCreation;
using CharacterCreatorApp.Contracts.Responses.Characters;

using FluentAssertions;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

/// <summary>Which step the wizard opens at and where each button takes the player, without rendering the page.</summary>
public class WizardNavigatorTests
{
    [Theory]
    [InlineData(false, false, false, false, false, 1)]
    [InlineData(true, false, false, false, false, 2)]
    [InlineData(true, true, false, false, false, 3)]
    [InlineData(true, true, true, false, false, 4)]
    [InlineData(true, true, true, true, false, 6)]
    [InlineData(true, true, true, true, true, 7)]
    public void FirstStepToFill_ForANewCharacter_IsTheFirstStepNotSaved(bool nameAndLevel, bool race, bool @class, bool background, bool scores, int expected)
    {
        // Arrange
        var draft = new CharacterDraftDetailsApiResponse
        {
            Name = nameAndLevel ? "Tordek" : null,
            Level = nameAndLevel ? 1 : null,
            RaceId = race ? "dwarf-phb" : null,
            ClassId = @class ? "fighter-phb" : null,
            BackgroundId = background ? "soldier-phb" : null,
            BaseAbilityScores = scores ? new Dictionary<string, int> { ["STR"] = 15, ["DEX"] = 14, ["CON"] = 13, ["INT"] = 12, ["WIS"] = 10, ["CHA"] = 8 } : [],
        };

        // Act
        var step = WizardNavigator.FirstStepToFill(draft);

        // Assert
        step.Should().Be(expected);
    }

    [Fact]
    public void Open_ForAnEditWithAStepNeedingAPick_OpensAtThatStep()
    {
        // Arrange
        var navigator = new WizardNavigator();

        // Act
        navigator.Open(new CharacterDraftDetailsApiResponse { CharacterId = Guid.NewGuid(), Name = "Tordek", Level = 1, ChoicesNeeded = ["languages"] });

        // Assert
        navigator.CurrentStep.Should().Be(5);
        navigator.StepsNeedingPick.Should().Equal(5);
        navigator.IsLevelUp.Should().BeFalse();
    }

    [Fact]
    public void Open_ForALevelUp_GoesThroughHitPointsTheNewStepsAndReview()
    {
        // Arrange
        var navigator = new WizardNavigator();

        // Act
        navigator.Open(new CharacterDraftDetailsApiResponse { CharacterId = Guid.NewGuid(), LevelUpChoices = ["hit-points", "improvements"], ChoicesNeeded = ["hit-points", "improvements"] });

        // Assert
        navigator.IsLevelUp.Should().BeTrue();
        navigator.Flow.Should().Equal(WizardSteps.HitPoints, 8, WizardSteps.Review);
        navigator.CurrentStep.Should().Be(WizardSteps.HitPoints);
        navigator.CanChangeBooks.Should().BeFalse();
    }

    [Fact]
    public void Back_FromTheFlowsFirstStep_StaysAndSaysSo()
    {
        // Arrange
        var navigator = new WizardNavigator();
        navigator.Open(new CharacterDraftDetailsApiResponse { CharacterId = Guid.NewGuid(), LevelUpChoices = ["hit-points"] });

        // Act
        var moved = navigator.Back(WizardSteps.HitPoints);

        // Assert
        moved.Should().BeFalse();
        navigator.CurrentStep.Should().Be(WizardSteps.HitPoints);
    }

    [Fact]
    public void AfterStepOne_SentThereToTickBooksWithNothingCleared_ReturnsToTheStepThatSentThePlayer()
    {
        // Arrange - at the improvements step of a new level-1 character
        var navigator = new WizardNavigator();
        navigator.Resume(new CharacterDraftDetailsApiResponse { Name = "Tordek", Level = 1, RaceId = "dwarf-phb" });
        navigator.Select(8);
        navigator.ChangeBooks();
        navigator.CurrentStep.Should().Be(1);

        // Act
        navigator.AfterStepOne([], keptChoices: true);

        // Assert
        navigator.CurrentStep.Should().Be(8);
    }

    [Fact]
    public void AfterStepOne_WithChoicesCleared_WalksTheStepsAgainFromTheSecond()
    {
        // Arrange
        var navigator = new WizardNavigator();
        navigator.Select(8);
        navigator.ChangeBooks();

        // Act
        navigator.AfterStepOne([], keptChoices: false);

        // Assert
        navigator.CurrentStep.Should().Be(2);
    }

    [Fact]
    public void AfterStepOne_AtALevelAbove1_PutsHitPointsBetweenSpellsAndReview()
    {
        // Arrange
        var navigator = new WizardNavigator();

        // Act
        navigator.AfterStepOne([2, 3], keptChoices: false);
        navigator.Next(9);

        // Assert
        navigator.Flow.Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, WizardSteps.HitPoints, WizardSteps.Review);
        navigator.CurrentStep.Should().Be(WizardSteps.HitPoints);
    }

    [Fact]
    public void Next_FromAStepThatNeededAPick_UnmarksIt()
    {
        // Arrange
        var navigator = new WizardNavigator();
        navigator.Open(new CharacterDraftDetailsApiResponse { CharacterId = Guid.NewGuid(), Name = "Tordek", Level = 1, ChoicesNeeded = ["languages"] });

        // Act
        navigator.Next(5);

        // Assert
        navigator.StepsNeedingPick.Should().BeEmpty();
        navigator.CurrentStep.Should().Be(6);
    }
}
