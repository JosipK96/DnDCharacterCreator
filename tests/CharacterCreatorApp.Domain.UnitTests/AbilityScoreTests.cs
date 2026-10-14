using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

namespace CharacterCreatorApp.Domain.UnitTests;

public class AbilityScoreTests
{
    [Theory]
    [InlineData("CON")]
    [InlineData("con")]
    [InlineData("Con")]
    [InlineData("Constitution")]
    [InlineData("constitution")]
    public void FromId_AnyKnownSpelling_ReturnsTheAbility(string key)
    {
        // Act - the race seed data spells ids in lower case
        var ability = AbilityScore.FromId(key);

        // Assert
        ability.Should().BeSameAs(AbilityScore.CON);
    }

    [Fact]
    public void FromId_UnknownKey_Throws()
    {
        // Act
        var act = () => AbilityScore.FromId("luck");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*luck*");
    }

    [Theory]
    [InlineData("str", "STR")]
    [InlineData("Dexterity", "DEX")]
    [InlineData("WIS", "WIS")]
    public void Normalize_AnyKnownSpelling_ReturnsTheCanonicalId(string key, string expected)
    {
        // Act
        var id = AbilityScore.Normalize(key);

        // Assert
        id.Should().Be(expected);
    }
}
