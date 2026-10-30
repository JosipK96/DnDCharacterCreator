using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

namespace CharacterCreatorApp.Domain.UnitTests;

public class AbilityIncreaseChoiceTests
{
    // A half-elf's: +1 to two different abilities besides its Charisma
    private static readonly AbilityIncreaseChoice HalfElf = new(["STR", "DEX", "CON", "INT", "WIS"], 2, 1);

    [Theory]
    [InlineData("DEX:1 WIS:1", true)]
    [InlineData("dexterity:1 wis:1", true)]
    [InlineData("DEX:1 WIS:1 STR:0", true)]
    [InlineData("DEX:2", false)]
    [InlineData("DEX:1", false)]
    [InlineData("DEX:1 WIS:1 STR:1", false)]
    [InlineData("CHA:1 DEX:1", false)]
    [InlineData("DEX:1 dex:1", false)]
    [InlineData("DEX:1 luck:1", false)]
    [InlineData("DEX:-1 WIS:1 STR:2", false)]
    [InlineData("", false)]
    public void Allows_ForTheHalfElfsChoice_AcceptsOneToEachOfTwoOptions(string increases, bool expected)
    {
        // Act
        var allowed = HalfElf.Allows(Increases(increases));

        // Assert
        allowed.Should().Be(expected);
    }

    [Theory]
    [InlineData("STR:2", true)]
    [InlineData("strength:1 DEX:1", true)]
    [InlineData("", false)]
    [InlineData("STR:1", false)]
    [InlineData("STR:3", false)]
    [InlineData("STR:1 DEX:1 CON:1", false)]
    [InlineData("STR:1 str:1", false)]
    [InlineData("luck:2", false)]
    public void Allows_ForAnImprovement_IsTwoPointsOverTheSixAbilitiesAtMostTwoOnOne(string increases, bool expected)
    {
        // Act
        var allowed = AbilityIncreaseChoice.Improvement.Allows(Increases(increases));

        // Assert
        allowed.Should().Be(expected);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("STR:0", true)]
    [InlineData("STR:1", false)]
    public void Allows_ForNone_AcceptsOnlyNoIncreases(string increases, bool expected)
    {
        // Act - a dwarf's increases are all fixed
        var allowed = AbilityIncreaseChoice.None.Allows(Increases(increases));

        // Assert
        allowed.Should().Be(expected);
    }

    // "DEX:1 WIS:1" as {"DEX": 1, "WIS": 1}; keys that differ only in case stay apart
    private static Dictionary<string, int> Increases(string text) =>
        text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split(':'))
            .ToDictionary(parts => parts[0], parts => int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture), StringComparer.Ordinal);
}
