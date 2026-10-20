using CharacterCreatorApp.Domain.DomainModels.Classes;
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
        var allowance = wizard.SpellAllowanceAt(3, subclass: null);

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
        var allowance = paladin.SpellAllowanceAt(1, subclass: null);

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
        var allowance = Class("fighter").SpellAllowanceAt(3, eldritchKnight);

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
        var allowance = Class("fighter").SpellAllowanceAt(level, eldritchKnight);

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
        var allowance = Class("fighter-2024").SpellAllowanceAt(3, eldritchKnight);

        // Assert
        allowance!.SchoolLimit.Should().BeNull();
    }

    [Fact]
    public void SpellAllowanceAt_AClassWithoutATableAndNoSubclass_ReturnsNull()
    {
        // Act
        var allowance = Class("fighter").SpellAllowanceAt(3, subclass: null);

        // Assert
        allowance.Should().BeNull();
    }

    [Fact]
    public void UnarmoredArmorClassesWith_AClassWithOne_ReturnsTheClasss()
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
    public void UnarmoredArmorClassesWith_ASubclassWithOne_ReturnsTheSubclasss()
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

    private static Class Class(string id, params ClassSpellProgression[] spellProgression) => new()
    {
        Id = id,
        Name = id,
        Edition = RulesEdition.Edition2014,
        HitDie = "1d8",
        SpellProgression = [.. spellProgression]
    };
}
