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

    private static CharacterStatsInput Build(Subrace? subrace = null, IEnumerable<string>? featSavingThrows = null) =>
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
            @class: new Class
            {
                Id = "fighter",
                Edition = RulesEdition.Edition2014,
                Name = "Fighter",
                Source = "PHB",
                HitDie = "1d10",
                SavingThrowAbilityIds = ["STR", "CON"]
            },
            subclass: null);
}
