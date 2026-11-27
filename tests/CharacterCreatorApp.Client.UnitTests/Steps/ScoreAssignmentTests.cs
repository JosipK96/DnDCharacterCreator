using CharacterCreatorApp.Client.Pages.CharacterCreation.Components.AbilityScores;

using FluentAssertions;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

/// <summary>Handing the six scores out to the abilities, without rendering the ability scores step.</summary>
public class ScoreAssignmentTests
{
    [Fact]
    public void AssignSelectedTo_WithAScoreClicked_GivesItToTheAbilityAndClearsTheClick()
    {
        // Arrange
        var assignment = new ScoreAssignment();
        assignment.UseScores([15, 14, 13, 12, 10, 8]);
        assignment.Select(1);

        // Act
        assignment.AssignSelectedTo("DEX");

        // Assert
        assignment.ScoreOf("DEX").Should().Be(14);
        assignment.IsAssigned(1).Should().BeTrue();
        assignment.SelectedIndex.Should().BeNull();
    }

    [Fact]
    public void Select_WithTheScoreAlreadyAssigned_IgnoresTheClick()
    {
        // Arrange
        var assignment = new ScoreAssignment();
        assignment.UseScores([15, 14, 13, 12, 10, 8]);
        assignment.Select(0);
        assignment.AssignSelectedTo("STR");

        // Act
        assignment.Select(0);

        // Assert
        assignment.SelectedIndex.Should().BeNull();
    }

    [Fact]
    public void Restore_WithSavedScores_GivesEachAbilityItsOwnInSheetOrder()
    {
        // Arrange
        var assignment = new ScoreAssignment();
        var saved = new Dictionary<string, int> { ["CHA"] = 15, ["STR"] = 8, ["DEX"] = 14, ["CON"] = 13, ["INT"] = 12, ["WIS"] = 10 };

        // Act
        assignment.Restore(saved);

        // Assert
        assignment.AllAssigned.Should().BeTrue();
        assignment.Scores.Should().Equal(8, 14, 13, 12, 10, 15);
        assignment.ToScores().Should().Equal(saved);
    }

    [Fact]
    public void Unassign_WithTheAbilityHoldingAScore_FreesTheScoreForAnother()
    {
        // Arrange
        var assignment = new ScoreAssignment();
        assignment.Restore(new Dictionary<string, int> { ["STR"] = 15, ["DEX"] = 14, ["CON"] = 13, ["INT"] = 12, ["WIS"] = 10, ["CHA"] = 8 });

        // Act
        assignment.Unassign("STR");

        // Assert
        assignment.AllAssigned.Should().BeFalse();
        assignment.ScoreOf("STR").Should().Be(0);
        assignment.IsAssigned(0).Should().BeFalse();
    }
}
