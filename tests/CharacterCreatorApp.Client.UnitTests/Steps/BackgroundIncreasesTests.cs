using CharacterCreatorApp.Client.Pages.CharacterCreation.Components.Backgrounds;

using FluentAssertions;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

/// <summary>A 2024 background's ability increases as picked, without rendering the background step.</summary>
public class BackgroundIncreasesTests
{
    private static readonly List<string> SoldierAbilities = ["STR", "DEX", "CON"];

    [Fact]
    public void For_WithPlusTwoAndPlusOnePicked_GivesBoth()
    {
        // Arrange
        var increases = new BackgroundIncreases { PlusTwo = "STR", PlusOne = "CON" };

        // Act
        var picked = increases.For(SoldierAbilities);

        // Assert
        picked.Should().Equal(new Dictionary<string, int> { ["STR"] = 2, ["CON"] = 1 });
    }

    [Fact]
    public void For_WithOneToEach_GivesOneToEveryAbility()
    {
        // Arrange
        var increases = new BackgroundIncreases { OneToEach = true };

        // Act
        var picked = increases.For(SoldierAbilities);

        // Assert
        picked.Should().Equal(new Dictionary<string, int> { ["STR"] = 1, ["DEX"] = 1, ["CON"] = 1 });
    }

    [Theory]
    [InlineData("STR", null)]
    [InlineData("STR", "STR")]
    public void For_WhileUnfinishedOrTheSameAbilityTwice_IsNull(string? plusTwo, string? plusOne)
    {
        // Arrange
        var increases = new BackgroundIncreases { PlusTwo = plusTwo, PlusOne = plusOne };

        // Act
        var picked = increases.For(SoldierAbilities);

        // Assert
        picked.Should().BeNull();
    }

    [Fact]
    public void For_ABackgroundWithNoAbilitiesToRaise_GivesNothingToSave()
    {
        // Arrange - a 2014 background
        var increases = new BackgroundIncreases();

        // Act
        var picked = increases.For([]);

        // Assert
        picked.Should().BeEmpty();
    }

    [Fact]
    public void Restore_WithOneToEachSaved_PicksOneToEachAgain()
    {
        // Arrange
        var increases = new BackgroundIncreases();

        // Act
        increases.Restore(new Dictionary<string, int> { ["STR"] = 1, ["DEX"] = 1, ["CON"] = 1 });

        // Assert
        increases.OneToEach.Should().BeTrue();
        increases.PlusTwo.Should().BeNull();
        increases.PlusOne.Should().BeNull();
    }
}
