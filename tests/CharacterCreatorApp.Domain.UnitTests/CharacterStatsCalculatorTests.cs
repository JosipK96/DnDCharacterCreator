using CharacterCreatorApp.Domain.Services;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

namespace CharacterCreatorApp.Domain.UnitTests;

public class CharacterStatsCalculatorTests
{
    [Theory]
    [InlineData(1, 2)]
    [InlineData(4, 2)]
    [InlineData(5, 3)]
    [InlineData(8, 3)]
    [InlineData(9, 4)]
    [InlineData(12, 4)]
    [InlineData(13, 5)]
    [InlineData(16, 5)]
    [InlineData(17, 6)]
    [InlineData(20, 6)]
    public void GetProficiencyBonus_FollowsTheLevelTable(int level, int expected)
    {
        CharacterStatsCalculator.GetProficiencyBonus(level).Should().Be(expected);
    }

    [Theory]
    [InlineData(1, -5)]
    [InlineData(3, -4)]
    [InlineData(8, -1)]
    [InlineData(9, -1)]
    [InlineData(10, 0)]
    [InlineData(11, 0)]
    [InlineData(12, 1)]
    [InlineData(15, 2)]
    [InlineData(20, 5)]
    [InlineData(30, 10)]
    public void GetAbilityModifier_RoundsDownIncludingBelowTen(int score, int expected)
    {
        CharacterStatsCalculator.GetAbilityModifier(score).Should().Be(expected);
    }

    [Fact]
    public void Calculate_AtLevelOne_GivesMaxHitDiePlusConstitution()
    {
        // Arrange - d10 hit die, CON 14 (+2)
        var input = Input(level: 1, hitDie: "1d10");

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert
        stats.MaxHitPoints.Should().Be(12);
        stats.HitDice.Should().Be("1d10");
    }

    [Fact]
    public void Calculate_AboveLevelOne_AddsTheDieAverageRoundedUpPerLevel()
    {
        // Arrange - d10 and CON 14 (+2): 12 at level 1, then 6+2 = 8 per level
        var input = Input(level: 5, hitDie: "1d10");

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert
        stats.MaxHitPoints.Should().Be(12 + (4 * 8));
        stats.HitDice.Should().Be("5d10");
    }

    [Theory]
    [InlineData("1d6", 8)]
    [InlineData("d6", 8)]
    [InlineData("6", 8)]
    [InlineData("1d12", 14)]
    public void Calculate_ReadsTheDieSizeWhicheverWayItIsWritten(string hitDie, int expectedHitPoints)
    {
        // Arrange - CON 14 (+2) at level 1
        var input = Input(level: 1, hitDie: hitDie);

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert
        stats.MaxHitPoints.Should().Be(expectedHitPoints);
    }

    [Theory]
    [InlineData("dwarf", "hill_dwarf", null, new string[0], 31)]
    [InlineData("dwarf_2024", null, null, new string[0], 31)]
    [InlineData("human", null, "draconic", new string[0], 31)]
    [InlineData("human_2024", null, "draconic-2024", new string[0], 31)]
    [InlineData("human", null, null, new[] { "tough_2014" }, 34)]
    [InlineData("human_2024", null, null, new[] { "tough_2024" }, 34)]
    [InlineData("dwarf", "hill_dwarf", "draconic", new[] { "tough_2014" }, 40)]
    [InlineData("dwarf_2024", null, "draconic-2024", new[] { "tough_2024" }, 40)]
    [InlineData("dwarf", "mountain_dwarf", "champion", new[] { "alert_2014" }, 28)]
    public void Calculate_WithChoicesThatAddHitPointsEveryLevel_AddsThemForEachLevel(
        string raceId, string? subraceId, string? subclassId, string[] featIds, int expectedHitPoints)
    {
        // Arrange - level 3, d10 and CON 14 (+2) is 28 on its own; Dwarven Toughness (a 2014
        // hill dwarf's, every 2024 dwarf's) and Draconic Resilience add 1 a level, Tough 2
        var input = Input(level: 3, hitDie: "1d10", raceId: raceId, subraceId: subraceId, subclassId: subclassId, feats: featIds);

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert
        stats.MaxHitPoints.Should().Be(expectedHitPoints);
    }

    [Fact]
    public void Calculate_WithAPunishingConstitution_StillGivesAtLeastOneHitPointPerLevel()
    {
        // Arrange - CON 1 is a -5 modifier, worse than a d6's average
        var input = Input(level: 3, hitDie: "1d6", scores: Scores(constitution: 1));

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert - 1 at level 1, then 1 for each level after
        stats.MaxHitPoints.Should().Be(3);
    }

    [Fact]
    public void Calculate_UnarmoredArmorClassAndInitiativeComeFromDexterity()
    {
        // Arrange - DEX 16 (+3)
        var input = Input(level: 1, scores: Scores(dexterity: 16));

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert
        stats.ArmorClass.Should().Be(13);
        stats.Initiative.Should().Be(3);
    }

    [Fact]
    public void Calculate_AddsProficiencyBonusToProficientSavingThrows()
    {
        // Arrange - a level 5 fighter (+3 bonus) proficient in STR and CON saves
        var input = Input(level: 5, savingThrows: ["STR", "CON"]);

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert - STR 15 (+2) proficient, DEX 12 (+1) not
        var strength = stats.SavingThrows.First(s => s.AbilityId == "STR");
        strength.IsProficient.Should().BeTrue();
        strength.Modifier.Should().Be(2 + 3);

        var dexterity = stats.SavingThrows.First(s => s.AbilityId == "DEX");
        dexterity.IsProficient.Should().BeFalse();
        dexterity.Modifier.Should().Be(1);
    }

    [Fact]
    public void Calculate_AddsProficiencyBonusToProficientSkills()
    {
        // Arrange - level 5 (+3 bonus), proficient in athletics only
        var input = Input(level: 5, proficiencies: ["athletics", "light-armor"]);

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert - athletics is STR 15 (+2)
        var athletics = stats.Skills.First(s => s.SkillId == "athletics");
        athletics.IsProficient.Should().BeTrue();
        athletics.Modifier.Should().Be(2 + 3);

        // Stealth is DEX 12 (+1) and not proficient
        var stealth = stats.Skills.First(s => s.SkillId == "stealth");
        stealth.IsProficient.Should().BeFalse();
        stealth.Modifier.Should().Be(1);
    }

    [Fact]
    public void Calculate_ReturnsAllEighteenSkillsAndAllSixSaves()
    {
        // Act
        var stats = CharacterStatsCalculator.Calculate(Input());

        // Assert
        stats.Skills.Should().HaveCount(18);
        stats.SavingThrows.Should().HaveCount(6);
        stats.Abilities.Should().HaveCount(6);
    }

    [Fact]
    public void Calculate_PassivePerceptionIsTenPlusThePerceptionSkill()
    {
        // Arrange - level 5 (+3), WIS 13 (+1), proficient in perception
        var input = Input(level: 5, proficiencies: ["perception"]);

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert
        stats.PassivePerception.Should().Be(10 + 1 + 3);
    }

    [Fact]
    public void Calculate_AcceptsAbilityKeysInAnySpelling()
    {
        // Arrange - the same scores written three different ways
        var input = Input(scores: new Dictionary<string, int>
        {
            ["strength"] = 15,
            ["DEX"] = 12,
            ["Con"] = 14,
            ["intelligence"] = 10,
            ["WIS"] = 13,
            ["cha"] = 8
        });

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert
        stats.Abilities.First(a => a.AbilityId == "STR").Modifier.Should().Be(2);
        stats.Abilities.First(a => a.AbilityId == "CON").Modifier.Should().Be(2);
        stats.Abilities.First(a => a.AbilityId == "CHA").Modifier.Should().Be(-1);
    }

    [Fact]
    public void Calculate_WithAMissingAbility_TreatsItAsTen()
    {
        // Arrange
        var input = Input(scores: new Dictionary<string, int> { ["STR"] = 15 });

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert
        var charisma = stats.Abilities.First(a => a.AbilityId == AbilityScore.CHA.Id);
        charisma.Score.Should().Be(10);
        charisma.Modifier.Should().Be(0);
    }

    private static Dictionary<string, int> Scores(
        int strength = 15,
        int dexterity = 12,
        int constitution = 14,
        int intelligence = 10,
        int wisdom = 13,
        int charisma = 8) => new()
        {
            ["STR"] = strength,
            ["DEX"] = dexterity,
            ["CON"] = constitution,
            ["INT"] = intelligence,
            ["WIS"] = wisdom,
            ["CHA"] = charisma
        };

    private static CharacterStatsInput Input(
        int level = 1,
        string hitDie = "1d10",
        Dictionary<string, int>? scores = null,
        string[]? proficiencies = null,
        string[]? savingThrows = null,
        int speed = 30,
        string raceId = "human",
        string? subraceId = null,
        string? subclassId = null,
        string[]? feats = null) =>
        new(level, scores ?? Scores(), proficiencies ?? [], hitDie, savingThrows ?? [], speed, raceId, subraceId, subclassId, feats ?? []);
}
