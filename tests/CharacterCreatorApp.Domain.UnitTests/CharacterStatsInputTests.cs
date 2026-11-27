using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.Services;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

namespace CharacterCreatorApp.Domain.UnitTests;

public class CharacterStatsInputTests
{
    [Fact]
    public void For_JoinsTheClassSavesWithTheFeatsAndDropsRepeats()
    {
        // Arrange - Resilient for Strength, which the fighter already saves with, and for Wisdom
        var input = Build(featSavingThrows: ["STR", "WIS"]);

        // Assert
        input.SavingThrowAbilityIds.Should().Equal("STR", "CON", "WIS");
    }

    [Fact]
    public void For_TakesHitDieFromTheClassAndSpeedFromTheSubraceWhereItHasItsOwn()
    {
        // Arrange
        var input = Build(subrace: new Subrace { Id = "wood_elf", Name = "Wood Elf", Source = "PHB", RaceId = "elf", Speed = 35 });

        // Assert
        input.HitDie.Should().Be("1d10");
        input.Speed.Should().Be(35);
        input.RaceId.Should().Be("elf");
        input.SubraceId.Should().Be("wood_elf");
    }

    [Fact]
    public void For_WithoutASubraceOrSubclass_LeavesTheirIdsOutAndWalksTheRacesSpeed()
    {
        // Arrange
        var input = Build();

        // Assert
        input.Speed.Should().Be(30);
        input.SubraceId.Should().BeNull();
        input.SubclassId.Should().BeNull();
    }

    [Fact]
    public void For_AClassThatCastsNothing_TakesOnlyTheRacesAndFeatsSpellcasting()
    {
        // Arrange - a fighter, no subclass, with Magic Initiate
        var input = Build(bonusSpellcasting: [new SpellcastingSource("Magic Initiate", "WIS")]);

        // Assert
        input.SpellcastingSources.Should().Equal(new SpellcastingSource("Magic Initiate", "WIS"));
    }

    [Fact]
    public void For_ACaster_PutsTheClassesSpellcastingFirst()
    {
        // Arrange - a level 1 wizard with Magic Initiate
        var wizard = new Class
        {
            Id = "wizard",
            Edition = RulesEdition.Edition2014,
            Name = "Wizard",
            Source = "PHB",
            HitDie = "1d6",
            Spellcasting = new Spellcasting { ClassId = "wizard", SpellcastingAbility = "INT" },
            SpellProgression = [new ClassSpellProgression { ClassId = "wizard", CharacterLevel = 1, CantripsKnown = 3, MaxSpellLevel = 1 }]
        };

        var input = Build(@class: wizard, bonusSpellcasting: [new SpellcastingSource("Magic Initiate", "WIS")]);

        // Assert
        input.SpellcastingSources.Should().Equal(new SpellcastingSource("Wizard", "INT"), new SpellcastingSource("Magic Initiate", "WIS"));
    }

    private static CharacterStatsInput Build(
        Subrace? subrace = null, IEnumerable<string>? featSavingThrows = null, Class? @class = null, IEnumerable<SpellcastingSource>? bonusSpellcasting = null) =>
        CharacterStatsInput.For(
            level: 1,
            abilityScores: new Dictionary<string, int> { ["STR"] = 15, ["CON"] = 14 },
            proficiencyIds: [],
            hitDieResults: new Dictionary<int, int>(),
            featIds: [],
            featSavingThrowAbilityIds: featSavingThrows ?? [],
            race: new Race
            {
                Id = "elf",
                Edition = RulesEdition.Edition2014,
                Name = "Elf",
                Source = "PHB",
                Size = "Medium",
                Speed = 30,
                Subraces = subrace == null ? [] : [subrace]
            },
            subrace: subrace,
            @class: @class ?? Fighter(),
            subclass: null,
            bonusSpellcasting: bonusSpellcasting ?? []);

    private static Class Fighter() => new()
    {
        Id = "fighter",
        Edition = RulesEdition.Edition2014,
        Name = "Fighter",
        Source = "PHB",
        HitDie = "1d10",
        SavingThrowAbilityIds = ["STR", "CON"]
    };
}
