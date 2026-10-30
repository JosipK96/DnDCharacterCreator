using CharacterCreatorApp.Domain.DomainModels.Classes;
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

    [Fact]
    public void Calculate_WithHitDieResults_AddsEachLevelsResultAndTheFixedValueForTheOthers()
    {
        // Arrange - d10 and CON 14 (+2): 12 at level 1, a 9 rolled at 3 and a 1 at 4, the fixed 6 at 2 and 5
        var input = Input(level: 5, hitDie: "1d10", hitDieResults: new() { [3] = 9, [4] = 1 });

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert
        stats.MaxHitPoints.Should().Be(12 + (6 + 2) + (9 + 2) + (1 + 2) + (6 + 2));
    }

    [Fact]
    public void Calculate_WithARolledOneAndAPunishingConstitution_StillGivesThatLevelOneHitPoint()
    {
        // Arrange - CON 6 (-2): a d8's 1 rolled at level 2 would add -1
        var input = Input(level: 2, hitDie: "1d8", scores: Scores(constitution: 6), hitDieResults: new() { [2] = 1 });

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert - 8 - 2 at level 1, then 1
        stats.MaxHitPoints.Should().Be(7);
    }

    [Fact]
    public void Calculate_WithAHitDieItCannotRead_Throws()
    {
        // Arrange - a typo in the class data must not become a d8
        var input = Input(hitDie: "1dx");

        // Act
        var act = () => CharacterStatsCalculator.Calculate(input);

        // Assert
        act.Should().Throw<FormatException>();
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

    // DEX 14 (+2), CON 16 (+3), WIS 15 (+2), CHA 17 (+3) in every row
    [Theory]
    [InlineData("barbarian and barbarian-2024: Unarmored Defense", 10, "CON", true, 10 + 2 + 3)]
    [InlineData("monk and monk-2024: Unarmored Defense", 10, "WIS", false, 10 + 2 + 2)]
    [InlineData("draconic: Draconic Resilience", 13, null, true, 13 + 2)]
    [InlineData("draconic-2024: Draconic Resilience", 10, "CHA", true, 10 + 2 + 3)]
    [InlineData("dance-2024: Unarmored Defense", 10, "CHA", false, 10 + 2 + 3)]
    public void Calculate_WithAnUnarmoredArmorClass_UsesItsFormula(
        string feature, int @base, string? abilityId, bool allowsShield, int expected)
    {
        // Arrange
        var unarmored = new UnarmoredArmorClass { Base = @base, AbilityId = abilityId, AllowsShield = allowsShield };
        var input = Input(
            scores: Scores(dexterity: 14, constitution: 16, wisdom: 15, charisma: 17),
            unarmoredArmorClasses: [unarmored]);

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert
        stats.ArmorClass.Should().Be(expected, feature);
        stats.Initiative.Should().Be(2);
    }

    [Fact]
    public void Calculate_WithoutAnUnarmoredArmorClass_IsTenPlusDexterity()
    {
        // Arrange - a fighter or a sorcerer of another subclass: DEX 14 (+2), CON 16 (+3)
        var input = Input(scores: Scores(dexterity: 14, constitution: 16), unarmoredArmorClasses: []);

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert
        stats.ArmorClass.Should().Be(12);
    }

    [Fact]
    public void Calculate_WithANegativeAddedModifier_KeepsTenPlusDexterity()
    {
        // Arrange - a barbarian with DEX 14 (+2) and CON 8 (-1): 10 + DEX + CON would be 11, and
        // both rule books let a character with two ways to work out AC pick the better one
        var barbarian = new UnarmoredArmorClass { Base = 10, AbilityId = "CON", AllowsShield = true };
        var input = Input(scores: Scores(dexterity: 14, constitution: 8), unarmoredArmorClasses: [barbarian]);

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert
        stats.ArmorClass.Should().Be(12);
    }

    [Fact]
    public void Calculate_WithANegativeDexterityModifier_SubtractsItFromTheFormula()
    {
        // Arrange - a monk with DEX 8 (-1) and WIS 16 (+3)
        var monk = new UnarmoredArmorClass { Base = 10, AbilityId = "WIS", AllowsShield = false };
        var input = Input(scores: Scores(dexterity: 8, wisdom: 16), unarmoredArmorClasses: [monk]);

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert
        stats.ArmorClass.Should().Be(10 - 1 + 3);
    }

    [Fact]
    public void Calculate_WithTwoUnarmoredArmorClasses_UsesTheHigher()
    {
        // Arrange - DEX 14 (+2), CON 12 (+1), CHA 18 (+4)
        var lower = new UnarmoredArmorClass { Base = 10, AbilityId = "CON", AllowsShield = true };
        var higher = new UnarmoredArmorClass { Base = 10, AbilityId = "CHA", AllowsShield = true };
        var input = Input(
            scores: Scores(dexterity: 14, constitution: 12, charisma: 18),
            unarmoredArmorClasses: [lower, higher]);

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert
        stats.ArmorClass.Should().Be(16);
    }

    [Fact]
    public void Calculate_WithAnAbilityIdInAnotherSpelling_NormalizesIt()
    {
        // Arrange - CON 16 (+3), DEX 12 (+1)
        var barbarian = new UnarmoredArmorClass { Base = 10, AbilityId = "constitution", AllowsShield = true };
        var input = Input(scores: Scores(constitution: 16), unarmoredArmorClasses: [barbarian]);

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert
        stats.ArmorClass.Should().Be(14);
    }

    [Fact]
    public void ArmorClassFor_WithOneAbilityInTwoSpellings_DoesNotThrow()
    {
        // Arrange - DEX +2, and CON as both "CON" and "constitution"
        var barbarian = new UnarmoredArmorClass { Base = 10, AbilityId = "CON", AllowsShield = true };
        var modifiers = new Dictionary<string, int> { ["DEX"] = 2, ["CON"] = 3, ["constitution"] = 3 };

        // Act
        var armorClass = barbarian.ArmorClassFor(modifiers);

        // Assert
        armorClass.Should().Be(15);
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

    [Fact]
    public void Calculate_AFullCaster_ReturnsARowPerSpellLevel()
    {
        // Arrange - a level 5 wizard: four 1st, three 2nd and two 3rd-level slots
        var input = Input(level: 5, spellSlots: new SpellSlots([4, 3, 2], PactMagic: null));

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert
        stats.SpellSlots.Should().Equal(new SpellSlotStat(1, 4), new SpellSlotStat(2, 3), new SpellSlotStat(3, 2));
        stats.PactMagic.Should().BeNull();
    }

    [Fact]
    public void Calculate_AHalfCaster_ReturnsARowPerSpellLevel()
    {
        // Arrange - a level 5 paladin (2014): four 1st and two 2nd-level slots
        var input = Input(level: 5, spellSlots: new SpellSlots([4, 2], PactMagic: null));

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert
        stats.SpellSlots.Should().Equal(new SpellSlotStat(1, 4), new SpellSlotStat(2, 2));
        stats.PactMagic.Should().BeNull();
    }

    [Fact]
    public void Calculate_AThirdCaster_ReturnsARowPerSpellLevel()
    {
        // Arrange - a level 13 Eldritch Knight, whose slots come from its subclass's table
        var input = Input(level: 13, spellSlots: new SpellSlots([4, 3, 2], PactMagic: null));

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert
        stats.SpellSlots.Should().Equal(new SpellSlotStat(1, 4), new SpellSlotStat(2, 3), new SpellSlotStat(3, 2));
        stats.PactMagic.Should().BeNull();
    }

    [Fact]
    public void Calculate_ASpellLevelWithNoSlots_IsLeftOut()
    {
        // Arrange - a table's column the character has not reached yet
        var input = Input(spellSlots: new SpellSlots([2, 0, 0], PactMagic: null));

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert
        stats.SpellSlots.Should().Equal(new SpellSlotStat(1, 2));
    }

    [Fact]
    public void Calculate_AWarlock_ReturnsPactMagicApartFromSpellSlots()
    {
        // Arrange - a level 11 warlock has three Pact Magic slots, all of 5th level
        var input = Input(level: 11, spellSlots: new SpellSlots([], new PactMagicSlots(Slots: 3, SlotLevel: 5)));

        // Act
        var stats = CharacterStatsCalculator.Calculate(input);

        // Assert
        stats.PactMagic.Should().Be(new PactMagicStat(SlotLevel: 5, Slots: 3));
        stats.SpellSlots.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_ANonCaster_HasNoSpellSlots()
    {
        // Act
        var stats = CharacterStatsCalculator.Calculate(Input(spellSlots: SpellSlots.None));

        // Assert
        stats.SpellSlots.Should().BeEmpty();
        stats.PactMagic.Should().BeNull();
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
        string[]? feats = null,
        UnarmoredArmorClass[]? unarmoredArmorClasses = null,
        SpellSlots? spellSlots = null,
        Dictionary<int, int>? hitDieResults = null) =>
        new(level, scores ?? Scores(), proficiencies ?? [], hitDie, hitDieResults ?? [], savingThrows ?? [], speed, raceId, subraceId, subclassId, feats ?? [],
            unarmoredArmorClasses ?? [], spellSlots ?? SpellSlots.None);
}
