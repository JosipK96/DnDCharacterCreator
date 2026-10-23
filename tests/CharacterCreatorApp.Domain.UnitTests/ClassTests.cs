using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Spells;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

namespace CharacterCreatorApp.Domain.UnitTests;

public class ClassTests
{
    [Fact]
    public void SpellAllowanceAt_ALevelOnTheClassTable_ReturnsThatRow()
    {
        // Arrange - the 2014 wizard at level 3: three cantrips, ten spellbook spells, up to level 2
        var wizard = Class("wizard", new ClassSpellProgression
        {
            ClassId = "wizard", CharacterLevel = 3, CantripsKnown = 3, SpellsKnown = 10, MaxSpellLevel = 2
        });

        // Act
        var allowance = wizard.SpellAllowanceAt(3, subclass: null, NoScores);

        // Assert
        allowance.Should().Be(new SpellAllowance(Cantrips: 3, Spells: 10, MaxSpellLevel: 2));
    }

    [Fact]
    public void SpellAllowanceAt_ALevelBeforeTheClassTableStarts_ReturnsNull()
    {
        // Arrange - the 2014 paladin casts from level 2
        var paladin = Class("paladin", new ClassSpellProgression
        {
            ClassId = "paladin", CharacterLevel = 2, CantripsKnown = 0, SpellsKnown = 0, MaxSpellLevel = 1
        });

        // Act
        var allowance = paladin.SpellAllowanceAt(1, subclass: null, NoScores);

        // Assert
        allowance.Should().BeNull();
    }

    [Fact]
    public void SpellAllowanceAt_AClassWithoutATableAndASubclassWithOne_ReturnsTheSubclassRow()
    {
        // Arrange - the fighter casts nothing; its Eldritch Knight has a table
        var eldritchKnight = new Subclass
        {
            Id = "eldritch-knight",
            Name = "Eldritch Knight",
            ClassId = "fighter",
            SpellProgression =
            [
                new SubclassSpellProgression
                {
                    SubclassId = "eldritch-knight", CharacterLevel = 3, CantripsKnown = 2, SpellsKnown = 3, MaxSpellLevel = 1, SpellcastingAbility = "INT"
                }
            ]
        };

        // Act
        var allowance = Class("fighter").SpellAllowanceAt(3, eldritchKnight, NoScores);

        // Assert
        allowance.Should().Be(new SpellAllowance(Cantrips: 2, Spells: 3, MaxSpellLevel: 1));
    }

    [Theory]
    [InlineData(3, 1)]
    [InlineData(7, 1)]
    [InlineData(8, 2)]
    [InlineData(14, 3)]
    [InlineData(20, 4)]
    public void SpellAllowanceAt_ASubclassThatLimitsSchools_GivesOneAnySchoolSpellPerLevelReached(int level, int anySchoolSpells)
    {
        // Arrange - the 2014 Eldritch Knight: abjuration and evocation, but one spell from any
        // school at each of levels 3, 8, 14 and 20
        var eldritchKnight = new Subclass
        {
            Id = "eldritch-knight",
            Name = "Eldritch Knight",
            ClassId = "fighter",
            SpellSchools = ["Abjuration", "Evocation"],
            AnySchoolSpellLevels = [3, 8, 14, 20],
            SpellProgression =
            [
                new SubclassSpellProgression
                {
                    SubclassId = "eldritch-knight", CharacterLevel = level, CantripsKnown = 2, SpellsKnown = 3, MaxSpellLevel = 1, SpellcastingAbility = "INT"
                }
            ]
        };

        // Act
        var allowance = Class("fighter").SpellAllowanceAt(level, eldritchKnight, NoScores);

        // Assert
        allowance!.SchoolLimit.Should().NotBeNull();
        allowance.SchoolLimit!.Schools.Should().Equal("Abjuration", "Evocation");
        allowance.SchoolLimit.AnySchoolSpells.Should().Be(anySchoolSpells);
    }

    [Fact]
    public void SpellAllowanceAt_ASubclassWithoutASchoolLimit_HasNone()
    {
        // Arrange - the 2024 Eldritch Knight picks from any school
        var eldritchKnight = new Subclass
        {
            Id = "eldritch-knight-2024",
            Name = "Eldritch Knight",
            ClassId = "fighter-2024",
            SpellProgression =
            [
                new SubclassSpellProgression
                {
                    SubclassId = "eldritch-knight-2024", CharacterLevel = 3, CantripsKnown = 2, SpellsKnown = 3, MaxSpellLevel = 1, SpellcastingAbility = "INT"
                }
            ]
        };

        // Act
        var allowance = Class("fighter-2024").SpellAllowanceAt(3, eldritchKnight, NoScores);

        // Assert
        allowance!.SchoolLimit.Should().BeNull();
    }

    [Fact]
    public void SpellAllowanceAt_AClassWithoutATableAndNoSubclass_ReturnsNull()
    {
        // Act
        var allowance = Class("fighter").SpellAllowanceAt(3, subclass: null, NoScores);

        // Assert
        allowance.Should().BeNull();
    }

    [Theory]
    [InlineData("cleric", "Wisdom", 1, 16, 4)]
    [InlineData("cleric", "Wisdom", 5, 16, 8)]
    [InlineData("cleric", "Wisdom", 1, 18, 5)]
    [InlineData("druid", "Wisdom", 3, 14, 5)]
    [InlineData("paladin", "Charisma", 2, 16, 4)]
    [InlineData("paladin", "Charisma", 5, 16, 5)]
    [InlineData("artificer", "Intelligence", 1, 16, 3)]
    [InlineData("artificer", "Intelligence", 3, 17, 4)]
    public void SpellAllowanceAt_A2014PreparedCaster_PreparesItsModifierPlusItsLevelOrHalfOfIt(
        string classId, string ability, int level, int score, int prepared)
    {
        // Arrange - a cleric or druid adds its whole level, a paladin or artificer half of it,
        // rounded down; the table gives no count and the ability comes from the class's data
        var divisor = classId is "paladin" or "artificer" ? 2 : 1;
        var @class = PreparedCaster(classId, ability, divisor, level);

        // Act
        var allowance = @class.SpellAllowanceAt(level, subclass: null, Scores(AbilityScore.Normalize(ability), score));

        // Assert
        allowance.Should().Be(new SpellAllowance(Cantrips: 2, Spells: prepared, MaxSpellLevel: 1));
    }

    [Theory]
    [InlineData("cleric", "Wisdom", 1, 1)]
    [InlineData("druid", "Wisdom", 1, 1)]
    [InlineData("paladin", "Charisma", 2, 2)]
    [InlineData("artificer", "Intelligence", 2, 1)]
    public void SpellAllowanceAt_APreparedCasterWithALowScore_PreparesAtLeastOne(string classId, string ability, int divisor, int level)
    {
        // Arrange - a modifier of -1 would leave nothing: the book says at least one
        var @class = PreparedCaster(classId, ability, divisor, level);

        // Act
        var allowance = @class.SpellAllowanceAt(level, subclass: null, Scores(AbilityScore.Normalize(ability), 8));

        // Assert
        allowance!.Spells.Should().Be(1);
    }

    [Fact]
    public void SpellAllowanceAt_ScoresKeyedInAnotherSpelling_ReadsTheSpellcastingAbility()
    {
        // Arrange - base scores arrive as "wisdom" or "wis" as well as "WIS"
        var cleric = PreparedCaster("cleric", "Wisdom", 1, 1);

        // Act
        var allowance = cleric.SpellAllowanceAt(1, subclass: null, new Dictionary<string, int> { ["wisdom"] = 16, ["STR"] = 18 });

        // Assert
        allowance!.Spells.Should().Be(4);
    }

    [Fact]
    public void SpellAllowanceAt_AWizard_KeepsItsTableCount()
    {
        // Arrange - the 2014 wizard prepares its modifier plus its level too, but from its
        // spellbook: the table's six spells are what it picks
        var wizard = PreparedCaster("wizard", "Intelligence", 1, 1, spellsKnown: 6);

        // Act
        var allowance = wizard.SpellAllowanceAt(1, subclass: null, Scores("INT", 16));

        // Assert
        allowance!.Spells.Should().Be(6);
        wizard.PreparesSpellsFromScoresAt(1).Should().BeFalse();
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void PreparesSpellsFromScoresAt_APaladin_OnlyFromItsFirstSpellLevel(int level, bool prepares)
    {
        // Arrange - the 2014 paladin's table starts at level 2
        var paladin = PreparedCaster("paladin", "Charisma", 2, 2);

        // Act
        var result = paladin.PreparesSpellsFromScoresAt(level);

        // Assert
        result.Should().Be(prepares);
    }

    [Fact]
    public void PreparesSpellsFromScoresAt_AClassWithoutTheRule_IsFalse()
    {
        // Arrange - a 2024 cleric's table gives its prepared spells
        var cleric = Class("cleric-2024", new ClassSpellProgression
        {
            ClassId = "cleric-2024", CharacterLevel = 1, CantripsKnown = 3, SpellsKnown = 4, MaxSpellLevel = 1
        });
        cleric.Spellcasting = new Spellcasting { ClassId = "cleric-2024", SpellcastingAbility = "Wisdom", IsPreparedCaster = true };

        // Act
        var result = cleric.PreparesSpellsFromScoresAt(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void UnarmoredArmorClassesWith_AClassWithOne_ReturnsTheClassOne()
    {
        // Arrange - a barbarian, whose subclass has none
        var unarmoredDefense = new UnarmoredArmorClass { Base = 10, AbilityId = "CON", AllowsShield = true };
        var barbarian = Class("barbarian");
        barbarian.UnarmoredArmorClass = unarmoredDefense;
        var berserker = new Subclass { Id = "berserker", Name = "Berserker", ClassId = "barbarian" };

        // Act
        var armorClasses = barbarian.UnarmoredArmorClassesWith(berserker);

        // Assert
        armorClasses.Should().Equal(unarmoredDefense);
    }

    [Fact]
    public void UnarmoredArmorClassesWith_ASubclassWithOne_ReturnsTheSubclassOne()
    {
        // Arrange - a draconic sorcerer
        var draconicResilience = new UnarmoredArmorClass { Base = 13, AbilityId = null, AllowsShield = true };
        var draconic = new Subclass { Id = "draconic", Name = "Draconic Bloodline", ClassId = "sorcerer", UnarmoredArmorClass = draconicResilience };

        // Act
        var armorClasses = Class("sorcerer").UnarmoredArmorClassesWith(draconic);

        // Assert
        armorClasses.Should().Equal(draconicResilience);
    }

    [Fact]
    public void UnarmoredArmorClassesWith_NeitherHasOne_ReturnsNone()
    {
        // Act - a sorcerer who has not picked a subclass yet
        var armorClasses = Class("sorcerer").UnarmoredArmorClassesWith(subclass: null);

        // Assert
        armorClasses.Should().BeEmpty();
    }

    [Fact]
    public void SpellSlotsAt_AClassWithARowAtTheLevel_ReturnsItsSlots()
    {
        // Arrange - a level 5 wizard
        var wizard = Class("wizard", new ClassSpellProgression
        {
            ClassId = "wizard", CharacterLevel = 5, CantripsKnown = 4, SpellsKnown = 14, MaxSpellLevel = 3, SpellSlots = [4, 3, 2]
        });

        // Act
        var slots = wizard.SpellSlotsAt(5, subclass: null);

        // Assert
        slots.PerSpellLevel.Should().Equal(4, 3, 2);
        slots.PactMagic.Should().BeNull();
    }

    [Fact]
    public void SpellSlotsAt_AWarlock_ReturnsItsPactMagicAtItsHighestSpellLevel()
    {
        // Arrange - a level 11 warlock: three slots of 5th level
        var warlock = Class("warlock", new ClassSpellProgression
        {
            ClassId = "warlock", CharacterLevel = 11, CantripsKnown = 4, SpellsKnown = 11, MaxSpellLevel = 5, PactMagicSlots = 3
        });

        // Act
        var slots = warlock.SpellSlotsAt(11, subclass: null);

        // Assert
        slots.PerSpellLevel.Should().BeEmpty();
        slots.PactMagic.Should().Be(new PactMagicSlots(Slots: 3, SlotLevel: 5));
    }

    [Fact]
    public void SpellSlotsAt_AClassWithoutATable_ReturnsTheSubclassSlots()
    {
        // Arrange - a level 7 Eldritch Knight, whose fighter casts nothing
        var eldritchKnight = new Subclass
        {
            Id = "eldritch-knight",
            Name = "Eldritch Knight",
            ClassId = "fighter",
            SpellProgression =
            [
                new SubclassSpellProgression
                {
                    SubclassId = "eldritch-knight", CharacterLevel = 7, CantripsKnown = 2, SpellsKnown = 5, MaxSpellLevel = 2,
                    SpellSlots = [4, 2], SpellcastingAbility = "Intelligence"
                }
            ]
        };

        // Act
        var slots = Class("fighter").SpellSlotsAt(7, eldritchKnight);

        // Assert
        slots.PerSpellLevel.Should().Equal(4, 2);
        slots.PactMagic.Should().BeNull();
    }

    [Fact]
    public void SpellSlotsAt_ALevelWithoutARow_ReturnsNone()
    {
        // Act - a fighter before its subclass, or a 2014 paladin at level 1
        var slots = Class("fighter").SpellSlotsAt(1, subclass: null);

        // Assert
        slots.Should().Be(SpellSlots.None);
    }

    [Fact]
    public void ChoicesGainedAt_TheSubclassLevel_AsksForTheSubclassAndTheSpellsOneOfThemCasts()
    {
        // Arrange - a fighter picks at 3; a Champion casts nothing, an Eldritch Knight starts its table
        var fighter = Fighter();
        fighter.Subclasses = [Champion(), EldritchKnight()];

        // Act
        var choices = fighter.ChoicesGainedAt(3, subclass: null, NoScores);

        // Assert
        choices.Should().Equal(DraftChoice.Subclass, DraftChoice.Spells);
    }

    [Fact]
    public void ChoicesGainedAt_AnImprovementLevel_AsksForTheImprovement()
    {
        // Act
        var choices = Fighter().ChoicesGainedAt(4, Champion(), NoScores);

        // Assert
        choices.Should().Equal(DraftChoice.Improvements);
    }

    [Fact]
    public void ChoicesGainedAt_ALevelThatBringsNothingToPick_AsksForNothing()
    {
        // Act
        var choices = Fighter().ChoicesGainedAt(5, Champion(), NoScores);

        // Assert
        choices.Should().BeEmpty();
    }

    [Fact]
    public void ChoicesGainedAt_ACasterGainingASpellLevel_AsksForSpells()
    {
        // Arrange - a wizard's third level opens 2nd-level spells and adds two to its spellbook
        var wizard = Class(
            "wizard",
            new ClassSpellProgression { ClassId = "wizard", CharacterLevel = 2, CantripsKnown = 3, SpellsKnown = 8, MaxSpellLevel = 1 },
            new ClassSpellProgression { ClassId = "wizard", CharacterLevel = 3, CantripsKnown = 3, SpellsKnown = 10, MaxSpellLevel = 2 });
        wizard.SubclassLevel = 2;

        // Act
        var choices = wizard.ChoicesGainedAt(3, new Subclass { Id = "evocation", Name = "Evocation", ClassId = "wizard" }, NoScores);

        // Assert
        choices.Should().Equal(DraftChoice.Spells);
    }

    [Fact]
    public void ChoicesGainedAt_AFightingStyleFeature_AsksForTheImprovementsStep()
    {
        // Arrange - a paladin's Fighting Style and first spells both come at 2
        var paladin = Class("paladin", new ClassSpellProgression { ClassId = "paladin", CharacterLevel = 2, SpellsKnown = 2, MaxSpellLevel = 1 });
        paladin.SubclassLevel = 3;
        paladin.Features = [Feature("paladin_fighting_style", "Fighting Style", 2)];

        // Act
        var choices = paladin.ChoicesGainedAt(2, subclass: null, NoScores);

        // Assert
        choices.Should().Equal(DraftChoice.Improvements, DraftChoice.Spells);
    }

    [Fact]
    public void ChoicesGainedAt_ASubclassFightingStyle_AsksForTheImprovementsStep()
    {
        // Arrange - a 2014 Champion's Additional Fighting Style at 10
        var champion = Champion();
        champion.Features = [Feature("champion_additional_fighting_style", "Additional Fighting Style", 10, champion.Id)];

        // Act
        var choices = Fighter().ChoicesGainedAt(10, champion, NoScores);

        // Assert
        choices.Should().Equal(DraftChoice.Improvements);
    }

    [Fact]
    public void ChoicesGainedAt_ASubclassWithAlwaysPreparedSpells_AsksForTheSpellsAPickMayRepeat()
    {
        // Arrange - a 2014 paladin's count is the same at 2 and 3, but Devotion's Bless may be a pick already
        var paladin = PreparedCaster("paladin", "CHA", divisor: 2, level: 2);
        paladin.SpellProgression.Add(new ClassSpellProgression { ClassId = "paladin", CharacterLevel = 3, CantripsKnown = 2, MaxSpellLevel = 1 });
        paladin.SubclassLevel = 3;
        var devotion = new Subclass { Id = "devotion", Name = "Devotion", ClassId = "paladin" };
        devotion.Spells = [new SpellSubclass { SpellId = "bless", SubclassId = devotion.Id, Level = 3, IsAlwaysPrepared = true }];
        paladin.Subclasses = [devotion];

        // Act
        var choices = paladin.ChoicesGainedAt(3, subclass: null, Scores("CHA", 14));

        // Assert
        choices.Should().Equal(DraftChoice.Subclass, DraftChoice.Spells);
    }

    [Fact]
    public void ChoicesGainedAt_AnImprovementForAPreparedCasterWhoseLevelCountStays_StillAsksForSpells()
    {
        // Arrange - a 2014 artificer prepares INT + half its level: 9 at both 18 and 19, but 19's
        // improvement may raise INT
        var artificer = PreparedCaster("artificer", "INT", divisor: 2, level: 18);
        artificer.SpellProgression.Add(new ClassSpellProgression { ClassId = "artificer", CharacterLevel = 19, CantripsKnown = 2, MaxSpellLevel = 1 });
        artificer.AsiLevels = [19];
        var subclass = new Subclass { Id = "alchemist", Name = "Alchemist", ClassId = "artificer" };

        // Act
        var choices = artificer.ChoicesGainedAt(19, subclass, Scores("INT", 16));

        // Assert
        choices.Should().Equal(DraftChoice.Improvements, DraftChoice.Spells);
    }

    [Fact]
    public void ChoicesGainedAt_TheFirstLevel_Throws()
    {
        // Act - level 1 is never reached by a level-up
        var act = () => Fighter().ChoicesGainedAt(1, subclass: null, NoScores);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // A 2014 fighter: a subclass at 3, improvements at 4, 6 and 8, its style at 1, no spells of its own
    private static Class Fighter()
    {
        var fighter = Class("fighter");
        fighter.SubclassLevel = 3;
        fighter.AsiLevels = [4, 6, 8];
        fighter.Features = [Feature("fighter_fighting_style", "Fighting Style", 1)];
        return fighter;
    }

    private static Subclass Champion() => new() { Id = "champion", Name = "Champion", ClassId = "fighter" };

    private static Subclass EldritchKnight() => new()
    {
        Id = "eldritch_knight",
        Name = "Eldritch Knight",
        ClassId = "fighter",
        SpellProgression =
        [
            new SubclassSpellProgression
            {
                SubclassId = "eldritch_knight", CharacterLevel = 3, CantripsKnown = 2, SpellsKnown = 3, MaxSpellLevel = 1, SpellcastingAbility = "INT"
            },
        ],
    };

    private static Feature Feature(string id, string name, int level, string? subclassId = null) =>
        new() { Id = id, Name = name, Description = name, Level = level, SubclassId = subclassId, Type = "class" };

    private static readonly Dictionary<string, int> NoScores = [];

    private static Dictionary<string, int> Scores(string abilityId, int score) => new() { [abilityId] = score };

    // A 2014 class with a table row at this level of two cantrips and no spell count, up to level 1
    private static Class PreparedCaster(string id, string ability, int divisor, int level, int spellsKnown = 0)
    {
        var @class = Class(id, new ClassSpellProgression
        {
            ClassId = id, CharacterLevel = level, CantripsKnown = 2, SpellsKnown = spellsKnown, MaxSpellLevel = 1
        });
        @class.Spellcasting = new Spellcasting
        {
            ClassId = id, SpellcastingAbility = ability, IsPreparedCaster = true, PreparedSpellsLevelDivisor = divisor
        };

        return @class;
    }

    private static Class Class(string id, params ClassSpellProgression[] spellProgression) => new()
    {
        Id = id,
        Name = id,
        Edition = RulesEdition.Edition2014,
        HitDie = "1d8",
        SpellProgression = [.. spellProgression]
    };
}
