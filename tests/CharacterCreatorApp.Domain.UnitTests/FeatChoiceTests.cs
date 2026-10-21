using CharacterCreatorApp.Domain.DomainModels.Feats;

using FluentAssertions;

namespace CharacterCreatorApp.Domain.UnitTests;

/// <summary>Which picks fill a feat's skill or tool choice: Keen Mind's one skill, Skilled's three.</summary>
public class FeatChoiceTests
{
    public static TheoryData<string[], bool> KeenMindPicks => new()
    {
        { ["arcana"], true },
        { [], false },
        { ["arcana", "history"], false },

        // Not one of Keen Mind's five
        { ["stealth"], false },
    };

    [Theory]
    [MemberData(nameof(KeenMindPicks))]
    public void Allows_ForASkillsChoice_TakesItsCountOfItsOptions(string[] picks, bool expected)
    {
        // Arrange
        var keenMind = Choice(FeatChoice.Skills, 1, "arcana", "history", "investigation", "nature", "religion");

        // Act
        var allows = keenMind.Allows(picks);

        // Assert
        allows.Should().Be(expected);
    }

    [Fact]
    public void Allows_ForASkillsChoiceListingATool_RefusesTheTool()
    {
        // Arrange - a skills choice is skills only, whatever the data lists
        var choice = Choice(FeatChoice.Skills, 1, "stealth", "lute");

        // Act
        var allows = choice.Allows(["lute"]);

        // Assert
        allows.Should().BeFalse();
    }

    public static TheoryData<string[], bool> SkilledPicks => new()
    {
        { ["stealth", "lute", "thieves-tools"], true },
        { ["stealth", "stealth", "lute"], false },
        { ["stealth", "lute"], false },
    };

    [Theory]
    [MemberData(nameof(SkilledPicks))]
    public void Allows_ForASkillsOrToolsChoice_TakesAnyMixOfDifferentOptions(string[] picks, bool expected)
    {
        // Arrange
        var skilled = Choice(FeatChoice.SkillsTools, 3, "stealth", "arcana", "lute", "thieves-tools");

        // Act
        var allows = skilled.Allows(picks);

        // Assert
        allows.Should().Be(expected);
    }

    [Fact]
    public void Allows_ForResilientsSavingThrow_RefusesAnyPick()
    {
        // Arrange - the save follows the ability the feat raises, so nothing is picked for it
        var resilient = Choice(FeatChoice.SavingThrow, 1, "STR", "DEX");

        // Act
        var allows = resilient.Allows(["STR"]);

        // Assert
        allows.Should().BeFalse();
        resilient.IsProficiencyPick().Should().BeFalse();
    }

    private static FeatChoice Choice(string type, int count, params string[] options) =>
        new() { Type = type, Count = count, Options = [.. options] };
}
