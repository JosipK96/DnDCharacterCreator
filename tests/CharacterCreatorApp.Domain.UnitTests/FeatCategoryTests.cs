using CharacterCreatorApp.Domain.DomainModels.Feats;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

namespace CharacterCreatorApp.Domain.UnitTests;

public class FeatCategoryTests
{
    [Theory]
    [InlineData(RulesEdition.Edition2014, null, true)]
    [InlineData(RulesEdition.Edition2014, FeatCategory.FightingStyle, false)]
    [InlineData(RulesEdition.Edition2024, FeatCategory.FightingStyle, true)]
    [InlineData(RulesEdition.Edition2024, "General", true)]
    public void CanBeTakenForImprovement_KeepsOnlyThe2014FightingStylesOut(string edition, string? category, bool expected)
    {
        // Act
        var allowed = FeatCategory.CanBeTakenForImprovement(edition, category);

        // Assert
        allowed.Should().Be(expected);
    }
}
