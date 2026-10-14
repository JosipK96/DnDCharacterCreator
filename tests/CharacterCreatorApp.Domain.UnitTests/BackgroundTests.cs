using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

namespace CharacterCreatorApp.Domain.UnitTests;

public class BackgroundTests
{
    // The 2024 soldier: +2 and +1, or +1 to each, among Strength, Dexterity and Constitution
    private static readonly Background Soldier2024 = new()
    {
        Id = "soldier_2024",
        Name = "Soldier",
        Edition = RulesEdition.Edition2024,
        AbilityScoreOptions = ["STR", "DEX", "CON"],
        FeatId = "savage_attacker_2024"
    };

    private static readonly Background Soldier2014 = new()
    {
        Id = "soldier",
        Name = "Soldier",
        Edition = RulesEdition.Edition2014,
        FeatureId = "soldier_military_rank"
    };

    [Theory]
    [InlineData("STR:2 CON:1", true)]
    [InlineData("CON:1 STR:2", true)]
    [InlineData("STR:1 DEX:1 CON:1", true)]
    [InlineData("strength:2 con:1", true)]
    [InlineData("STR:2 CON:1 DEX:0", true)]
    [InlineData("STR:2", false)]
    [InlineData("STR:1 DEX:1", false)]
    [InlineData("STR:2 CON:2", false)]
    [InlineData("STR:3", false)]
    [InlineData("STR:2 WIS:1", false)]
    [InlineData("STR:2 str:1", false)]
    [InlineData("STR:2 luck:1", false)]
    [InlineData("", false)]
    public void AllowsAbilityIncreases_For2024Background_AcceptsTwoAndOneOrOneToEachOfItsAbilities(string increases, bool expected)
    {
        // Act
        var allowed = Soldier2024.AllowsAbilityIncreases(Increases(increases));

        // Assert
        allowed.Should().Be(expected);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("STR:1", false)]
    public void AllowsAbilityIncreases_For2014Background_AcceptsNone(string increases, bool expected)
    {
        // Act - a 2014 character's race raises its abilities, not its background
        var allowed = Soldier2014.AllowsAbilityIncreases(Increases(increases));

        // Assert
        allowed.Should().Be(expected);
    }

    // "STR:2 CON:1" as {"STR": 2, "CON": 1}; keys that differ only in case stay apart
    private static Dictionary<string, int> Increases(string text) =>
        text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split(':'))
            .ToDictionary(parts => parts[0], parts => int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture), StringComparer.Ordinal);
}
