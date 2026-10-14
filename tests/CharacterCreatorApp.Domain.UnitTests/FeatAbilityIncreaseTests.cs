using CharacterCreatorApp.Domain.DomainModels.Feats;

using FluentAssertions;

namespace CharacterCreatorApp.Domain.UnitTests;

public class FeatAbilityIncreaseTests
{
    private static readonly FeatAbilityIncrease Actor = new() { Options = ["CHA"] };
    private static readonly FeatAbilityIncrease Athlete = new() { Options = ["STR", "DEX"] };

    // The 2024 Ability Score Improvement feat: +2 to one ability or +1 to two
    private static readonly FeatAbilityIncrease AbilityScoreImprovement = new() { Options = ["STR", "DEX", "CON", "INT", "WIS", "CHA"], Points = 2 };

    [Fact]
    public void Resolve_ForAFixedIncreaseWithoutAPick_RaisesItsAbility()
    {
        // Act
        var increase = Actor.Resolve(new Dictionary<string, int>());

        // Assert
        increase.Should().BeEquivalentTo(Picks("CHA:1"));
    }

    [Fact]
    public void Resolve_WithAPickAmongTheOptions_RaisesItUnderItsId()
    {
        // Act
        var increase = Athlete.Resolve(Picks("dexterity:1"));

        // Assert
        increase.Should().BeEquivalentTo(Picks("DEX:1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("CHA:1")]
    [InlineData("STR:1 DEX:1")]
    [InlineData("STR:2")]
    public void Resolve_WithoutOneOfTheOptionsPicked_ReturnsNull(string picked)
    {
        // Act - Athlete raises Strength or Dexterity by 1, one of them
        var increase = Athlete.Resolve(Picks(picked));

        // Assert
        increase.Should().BeNull();
    }

    [Theory]
    [InlineData("str:2", "STR:2")]
    [InlineData("STR:1 wisdom:1", "STR:1 WIS:1")]
    public void Resolve_ForTheAbilityScoreImprovementFeat_TakesTwoToOneOrOneToTwo(string picked, string expected)
    {
        // Act
        var increase = AbilityScoreImprovement.Resolve(Picks(picked));

        // Assert
        increase.Should().BeEquivalentTo(Picks(expected));
    }

    [Theory]
    [InlineData("STR:1")]
    [InlineData("STR:2 DEX:1")]
    [InlineData("STR:1 DEX:1 CON:1")]
    public void Resolve_ForTheAbilityScoreImprovementFeat_RefusesAnythingButTwoPoints(string picked)
    {
        // Act
        var increase = AbilityScoreImprovement.Resolve(Picks(picked));

        // Assert
        increase.Should().BeNull();
    }

    // "STR:1 WIS:1" as {"STR": 1, "WIS": 1}
    private static Dictionary<string, int> Picks(string text) =>
        text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split(':'))
            .ToDictionary(parts => parts[0], parts => int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture), StringComparer.Ordinal);
}
