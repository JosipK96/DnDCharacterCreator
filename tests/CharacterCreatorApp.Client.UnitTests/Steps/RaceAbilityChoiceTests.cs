using CharacterCreatorApp.Client.Pages.CharacterCreation.Components.Races;

using FluentAssertions;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

/// <summary>Picking a race's ability increases, without rendering the race step.</summary>
public class RaceAbilityChoiceTests
{
    // A Monsters of the Multiverse race: 3 points over all six, at most +2 on one
    private static readonly RaceAbilityChoice Mpmm = new(["STR", "DEX", "CON", "INT", "WIS", "CHA"], 3, 1, 2);

    // A half-elf: +1 to two abilities besides Charisma
    private static readonly RaceAbilityChoice HalfElf = new(["STR", "DEX", "CON", "INT", "WIS"], 2, 1, 1);

    [Fact]
    public void Raise_AnAbilityPickedOnceWithRoomForMore_RaisesItAgain()
    {
        // Arrange
        var picks = new Dictionary<string, int> { ["STR"] = 1 };

        // Act
        Mpmm.Raise(picks, "STR");

        // Assert
        picks.Should().Equal(new Dictionary<string, int> { ["STR"] = 2 });
    }

    [Fact]
    public void Raise_AnAbilityAtItsMost_UnpicksIt()
    {
        // Arrange
        var picks = new Dictionary<string, int> { ["STR"] = 2 };

        // Act
        Mpmm.Raise(picks, "STR");

        // Assert
        picks.Should().BeEmpty();
    }

    [Fact]
    public void Raise_WithNoPointsLeft_UnpicksTheAbilityInsteadOfRaisingIt()
    {
        // Arrange - a half-elf's two points are spent
        var picks = new Dictionary<string, int> { ["STR"] = 1, ["DEX"] = 1 };

        // Act
        HalfElf.Raise(picks, "CON");

        // Assert
        picks.Should().Equal(new Dictionary<string, int> { ["STR"] = 1, ["DEX"] = 1 });
    }

    [Theory]
    [InlineData(2, 1, 1, "Raise 2 different abilities by 1.")]
    [InlineData(2, 2, 2, "Raise 1 ability by 2.")]
    [InlineData(3, 1, 2, "Spread 3 points, at most +2 to one ability: pick an ability again to raise it further.")]
    public void Describe_AsASentence_SaysHowThePointsSpread(int points, int min, int max, string expected)
    {
        // Arrange
        var choice = new RaceAbilityChoice(["STR", "DEX"], points, min, max);

        // Act
        var text = choice.Describe(sentence: true);

        // Assert
        text.Should().Be(expected);
    }
}
