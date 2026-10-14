using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

namespace CharacterCreatorApp.Domain.UnitTests;

public class RaceTests
{
    [Fact]
    public void AbilityChoiceWith_ForTheHalfElf_IsTwoOfItsOptions()
    {
        // Act
        var choice = HalfElf().AbilityChoiceWith(subrace: null);

        // Assert
        choice.Should().BeEquivalentTo(new AbilityIncreaseChoice(["STR", "DEX", "CON", "INT", "WIS"], 2, 1));
    }

    [Fact]
    public void AbilityChoiceWith_ForTheVariantHuman_IsTheSubracesChoice()
    {
        // Arrange
        var human = Human();

        // Act
        var choice = human.AbilityChoiceWith(human.Subraces.Single(s => s.Id == "variant_human"));

        // Assert
        choice.Should().BeEquivalentTo(new AbilityIncreaseChoice(["STR", "DEX", "CON", "INT", "WIS", "CHA"], 2, 1));
    }

    [Fact]
    public void AbilityChoiceWith_ForTheStandardHuman_IsNone()
    {
        // Arrange - its +1 to everything is fixed
        var human = Human();

        // Act
        var choice = human.AbilityChoiceWith(human.Subraces.Single(s => s.Id == "standard_human"));

        // Assert
        choice.Should().Be(AbilityIncreaseChoice.None);
    }

    [Fact]
    public void BonusFeatsWith_ForTheVariantHuman_IsOneOfAnyCategory()
    {
        // Arrange
        var human = Human();

        // Act
        var feats = human.BonusFeatsWith(human.Subraces.Single(s => s.Id == "variant_human"));

        // Assert
        feats.Should().Be(new BonusFeatChoice(1, null));
    }

    [Fact]
    public void BonusFeatsWith_ForThe2024Human_IsOneOrigin()
    {
        // Act
        var feats = Human2024().BonusFeatsWith(subrace: null);

        // Assert
        feats.Should().Be(new BonusFeatChoice(1, "Origin"));
    }

    [Fact]
    public void BonusFeatsWith_ForTheStandardHuman_IsNone()
    {
        // Arrange
        var human = Human();

        // Act
        var feats = human.BonusFeatsWith(human.Subraces.Single(s => s.Id == "standard_human"));

        // Assert
        feats.Should().Be(new BonusFeatChoice(0, null));
    }

    [Theory]
    [InlineData(null, "Alert", true)]
    [InlineData(null, null, true)]
    [InlineData("Origin", "Origin", true)]
    [InlineData("Origin", "General", false)]
    [InlineData("Origin", null, false)]
    public void Accepts_TakesAFeatOfTheCategoryOrAnyWithoutOne(string? category, string? featCategory, bool expected)
    {
        // Act
        var accepted = new BonusFeatChoice(1, category).Accepts(featCategory);

        // Assert
        accepted.Should().Be(expected);
    }

    [Fact]
    public void SkillChoiceCountWith_ForTheVariantHuman_AddsTheSubracesSkill()
    {
        // Arrange
        var human = Human();

        // Act
        var count = human.SkillChoiceCountWith(human.Subraces.Single(s => s.Id == "variant_human"));

        // Assert
        count.Should().Be(1);
    }

    [Theory]
    [InlineData("perception", true)]
    [InlineData("stealth", false)]
    public void OffersSkill_ForThe2024Elf_OffersOnlyKeenSensesThree(string skillId, bool expected)
    {
        // Act
        var offered = Elf2024().OffersSkill(skillId, subrace: null);

        // Assert
        offered.Should().Be(expected);
    }

    [Theory]
    [InlineData("stealth", true)]
    [InlineData("thieves-tools", false)]
    public void OffersSkill_ForTheVariantHuman_OffersAnySkill(string skillId, bool expected)
    {
        // Arrange
        var human = Human();

        // Act
        var offered = human.OffersSkill(skillId, human.Subraces.Single(s => s.Id == "variant_human"));

        // Assert
        offered.Should().Be(expected);
    }

    // +2 Charisma and +1 to two others of the player's choice; any two skills
    private static Race HalfElf() => new()
    {
        Id = "half_elf",
        Name = "Half-Elf",
        Edition = RulesEdition.Edition2014,
        Size = "Medium",
        Speed = 30,
        SkillChoiceCount = 2,
        AbilityChoiceCount = 2,
        AbilityChoiceOptions = ["STR", "DEX", "CON", "INT", "WIS"],
        AbilityBonuses = [new RaceAbilityBonus { RaceId = "half_elf", AbilityScoreId = "CHA", Bonus = 2 }],
    };

    // The standard human's +1 to everything, or the variant's +1 to two, a skill and a feat
    private static Race Human() => new()
    {
        Id = "human",
        Name = "Human",
        Edition = RulesEdition.Edition2014,
        Size = "Medium",
        Speed = 30,
        Subraces =
        [
            new Subrace
            {
                Id = "standard_human",
                Name = "Standard Human",
                RaceId = "human",
                AbilityBonuses = [new SubraceAbilityBonus { SubraceId = "standard_human", AbilityScoreId = "STR", Bonus = 1 }],
            },
            new Subrace
            {
                Id = "variant_human",
                Name = "Variant Human",
                RaceId = "human",
                AbilityChoiceCount = 2,
                AbilityChoiceOptions = ["STR", "DEX", "CON", "INT", "WIS", "CHA"],
                SkillChoiceCount = 1,
                FeatChoiceCount = 1,
            },
        ],
    };

    // Skillful and Versatile: any skill and an Origin feat
    private static Race Human2024() => new()
    {
        Id = "human_2024",
        Name = "Human",
        Edition = RulesEdition.Edition2024,
        Size = "Small or Medium",
        Speed = 30,
        SkillChoiceCount = 1,
        FeatChoiceCount = 1,
        FeatChoiceCategory = "Origin",
    };

    // Keen Senses: one of three skills
    private static Race Elf2024() => new()
    {
        Id = "elf_2024",
        Name = "Elf",
        Edition = RulesEdition.Edition2024,
        Size = "Medium",
        Speed = 30,
        SkillChoiceCount = 1,
        SkillChoiceOptions = ["insight", "perception", "survival"],
    };
}
