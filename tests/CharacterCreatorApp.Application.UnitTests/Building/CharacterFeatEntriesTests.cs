using CharacterCreatorApp.Application.Features.CharacterCreation.Building;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

namespace CharacterCreatorApp.Application.UnitTests.Building;

public class CharacterFeatEntriesTests
{
    private static readonly List<FeatReadModel> Feats = [Feat("magic_initiate"), Feat("alert")];

    private static readonly List<SpellReadModel> Spells =
        [Spell("light"), Spell("guidance"), Spell("bless"), Spell("fire_bolt"), Spell("mage_hand"), Spell("shield")];

    [Fact]
    public void Build_WithAFeatTakenTwice_ListsItTwiceEachWithItsOwnPick()
    {
        // Arrange
        List<FeatSpellPick> picks =
        [
            new() { FeatId = "magic_initiate", ClassId = "cleric", SpellIds = ["light", "guidance", "bless"] },
            new() { FeatId = "magic_initiate", ClassId = "wizard", SpellIds = ["fire_bolt", "mage_hand", "shield"] },
        ];

        // Act
        var entries = CharacterFeatEntries.Build(["magic_initiate", "alert", "magic_initiate"], picks, Feats, Spells);

        // Assert
        entries.Select(e => e.Feat.Id).Should().Equal("magic_initiate", "alert", "magic_initiate");
        entries[0].Spells.Select(s => s.Id).Should().Equal("light", "guidance", "bless");
        entries[1].Spells.Should().BeEmpty();
        entries[2].Spells.Select(s => s.Id).Should().Equal("fire_bolt", "mage_hand", "shield");
    }

    [Fact]
    public void Build_WithAFeatTakenTwice_GivesEachTakeTheAbilityPickedForIt()
    {
        // Arrange - the 2024 Magic Initiate's ability is picked each time with its spells
        List<FeatSpellPick> picks =
        [
            new() { FeatId = "magic_initiate", ClassId = "cleric", SpellIds = ["light"], AbilityId = "WIS" },
            new() { FeatId = "magic_initiate", ClassId = "wizard", SpellIds = ["fire_bolt"], AbilityId = "INT" },
        ];

        // Act
        var entries = CharacterFeatEntries.Build(["magic_initiate", "alert", "magic_initiate"], picks, Feats, Spells);

        // Assert
        entries.Select(e => e.SpellcastingAbilityId).Should().Equal("WIS", null, "INT");
    }

    [Fact]
    public void Build_WithAFeatTakenOnce_ListsItOnce()
    {
        // Act
        var entries = CharacterFeatEntries.Build(["alert"], [], Feats, Spells);

        // Assert
        entries.Should().ContainSingle().Which.Feat.Id.Should().Be("alert");
        entries[0].Spells.Should().BeEmpty();
    }

    [Fact]
    public void Build_WithMoreTakesThanPicks_LeavesTheLaterTakeWithoutSpells()
    {
        // Arrange - a character made before the picks were kept per feat has none
        List<FeatSpellPick> picks = [new() { FeatId = "magic_initiate", ClassId = "cleric", SpellIds = ["light", "guidance", "bless"] }];

        // Act
        var entries = CharacterFeatEntries.Build(["magic_initiate", "magic_initiate"], picks, Feats, Spells);

        // Assert
        entries.Should().HaveCount(2);
        entries[0].Spells.Select(s => s.Id).Should().Equal("light", "guidance", "bless");
        entries[1].Spells.Should().BeEmpty();
    }

    [Fact]
    public void Build_SkipsAFeatAndASpellThatWereNotLoaded()
    {
        // Arrange - a reload of the seed data dropped "lucky" and "aid"
        List<FeatSpellPick> picks = [new() { FeatId = "magic_initiate", ClassId = "cleric", SpellIds = ["light", "aid"] }];

        // Act
        var entries = CharacterFeatEntries.Build(["lucky", "magic_initiate"], picks, Feats, Spells);

        // Assert
        entries.Should().ContainSingle().Which.Feat.Id.Should().Be("magic_initiate");
        entries[0].Spells.Select(s => s.Id).Should().Equal("light");
    }

    private static FeatReadModel Feat(string id) => new()
    {
        Id = id,
        Name = id,
        Description = "A feat.",
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
    };

    private static SpellReadModel Spell(string id) => new()
    {
        Id = id,
        Name = id,
        Source = "PHB",
        School = "Evocation",
        CastingTimeUnit = "action",
        RangeType = "point",
        DurationType = "instant",
        Description = "A spell.",
    };
}
