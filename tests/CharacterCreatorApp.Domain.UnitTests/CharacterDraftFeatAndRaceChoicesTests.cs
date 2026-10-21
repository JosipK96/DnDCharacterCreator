using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

namespace CharacterCreatorApp.Domain.UnitTests;

/// <summary>
/// The size, the background's picked feat, the spells a race or feat lets the player pick, the
/// skills or tools a feat does, and what clears each of them.
/// </summary>
public class CharacterDraftFeatAndRaceChoicesTests
{
    // The fixture is a 2014 draft, so its Player's Handbook is always allowed
    private const string Handbook = "PHB";
    private const string Other = "XGE";

    private static readonly FakeTimeProvider FakeTimeProvider = new(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero));

    [Fact]
    public void SetRace_WithASize_KeepsIt()
    {
        // Arrange
        var draft = NewDraft();

        // Act
        draft.SetRace("human_2024", null, null, "Small");

        // Assert
        draft.Size.Should().Be("Small");
    }

    [Fact]
    public void SetRace_WithAnotherRace_ClearsTheRacesSpells()
    {
        // Arrange
        var draft = DraftWithEveryChoice();

        // Act
        draft.SetRace("dwarf_2024");

        // Assert
        draft.RaceSpellIds.Should().BeEmpty();
        draft.Size.Should().BeNull();
    }

    [Fact]
    public void SetRace_WithTheSameRaceAndSubrace_KeepsTheRacesSpells()
    {
        // Arrange
        var draft = DraftWithEveryChoice();

        // Act
        draft.SetRace("elf", "high_elf");

        // Assert
        draft.RaceSpellIds.Should().Equal("fire_bolt");
    }

    [Fact]
    public void SetSubrace_WithAnotherSubrace_ClearsTheRacesSpellsAndTheFeatsSpells()
    {
        // Arrange
        var draft = DraftWithEveryChoice();

        // Act
        draft.SetSubrace("wood_elf");

        // Assert
        draft.RaceSpellIds.Should().BeEmpty();
        draft.FeatSpells.Should().BeEmpty();
    }

    [Fact]
    public void SetBackground_WithAFeat_KeepsIt()
    {
        // Arrange
        var draft = NewDraft();

        // Act
        draft.SetBackground("mist_wanderer_2024", null, "mist_walker");

        // Assert
        draft.BackgroundFeatId.Should().Be("mist_walker");
    }

    [Fact]
    public void SetBackground_WithAnotherFeat_ClearsTheFeatsSpells()
    {
        // Arrange - the background's feat comes first among the feats whose spells are picked
        var draft = DraftWithEveryChoice();

        // Act
        draft.SetBackground("acolyte", null, "echoing_soul");

        // Assert
        draft.FeatSpells.Should().BeEmpty();
        draft.BackgroundFeatId.Should().Be("echoing_soul");
    }

    [Fact]
    public void SetBackground_WithTheSameBackgroundAndFeat_KeepsTheFeatsSpells()
    {
        // Arrange
        var draft = DraftWithEveryChoice();

        // Act
        draft.SetBackground("acolyte", null, "mist_walker");

        // Assert
        draft.FeatSpells.Should().ContainSingle();
    }

    [Fact]
    public void SetFeats_WithTheSameFeats_KeepsTheFeatsSpells()
    {
        // Arrange - the improvements step saves again on the way back through it
        var draft = DraftWithEveryChoice();

        // Act
        draft.SetFeats(["magic_initiate_2014"]);

        // Assert
        draft.FeatSpells.Should().ContainSingle();
    }

    [Fact]
    public void SetFeats_WithOtherFeats_ClearsTheFeatsSpells()
    {
        // Arrange
        var draft = DraftWithEveryChoice();

        // Act
        draft.SetFeats(["alert_2014"]);

        // Assert
        draft.FeatSpells.Should().BeEmpty();
    }

    [Fact]
    public void ChooseFightingStyles_WithOtherStyles_ClearsTheFeatsSpells()
    {
        // Arrange - a 2024 Blessed Warrior has two cantrips to pick
        var draft = DraftWithEveryChoice();

        // Act
        draft.ChooseFightingStyles(["blessed_warrior"]);

        // Assert
        draft.FeatSpells.Should().BeEmpty();
    }

    [Fact]
    public void SetSubclass_WithAnotherSubclass_ClearsTheFeatsSpellsWithTheStyles()
    {
        // Arrange
        var draft = DraftWithEveryChoice();

        // Act
        draft.SetSubclass("eldritch-knight");

        // Assert
        draft.FightingStyleIds.Should().BeEmpty();
        draft.FeatSpells.Should().BeEmpty();
    }

    [Fact]
    public void ChooseBonusSpells_KeepsTheRacesAndTheFeatsSpells()
    {
        // Arrange
        var draft = NewDraft();
        var pick = new FeatSpellPick { FeatId = "magic_initiate_2024", ClassId = "wizard-2024", SpellIds = ["light_2024", "sleep_2024"] };

        // Act
        draft.ChooseBonusSpells(["fire_bolt_2024"], [pick]);

        // Assert
        draft.RaceSpellIds.Should().Equal("fire_bolt_2024");
        draft.FeatSpells.Should().BeEquivalentTo([pick]);
    }

    [Fact]
    public void ChooseBonusSpells_WithASpellcastingAbility_KeepsItsCanonicalId()
    {
        // Arrange - the 2024 Magic Initiate's ability arrives in any spelling
        var draft = NewDraft();
        var pick = new FeatSpellPick { FeatId = "magic_initiate_2024", ClassId = "wizard-2024", SpellIds = ["light_2024"], AbilityId = "wisdom" };

        // Act
        draft.ChooseBonusSpells([], [pick]);

        // Assert
        draft.FeatSpells.Should().ContainSingle().Which.AbilityId.Should().Be("WIS");
    }

    [Fact]
    public void SetFeats_WithProficiencyPicks_KeepsThemInOrder()
    {
        // Arrange - Skilled taken twice, once for the background and once for the race
        var draft = NewDraft();
        FeatProficiencyPick[] picks =
        [
            new() { FeatId = "skilled_2024", ProficiencyIds = ["stealth", "lute", "arcana"] },
            new() { FeatId = "skilled_2024", ProficiencyIds = ["history", "drum", "insight"] },
        ];

        // Act
        draft.SetFeats(["skilled_2024"], null, [.. picks]);

        // Assert
        draft.FeatProficiencyPicks.Should().Equal(picks);
    }

    [Fact]
    public void SetFeats_WithoutProficiencyPicks_ClearsThem()
    {
        // Arrange - the improvements step always sends them with the feats
        var draft = DraftWithEveryChoice();

        // Act
        draft.SetFeats(["magic_initiate_2014"]);

        // Assert
        draft.FeatProficiencyPicks.Should().BeEmpty();
    }

    [Fact]
    public void SetBackground_WithAnotherFeat_ClearsTheFeatsProficiencyPicks()
    {
        // Arrange - the background's feat comes first among the feats whose picks are kept
        var draft = DraftWithEveryChoice();

        // Act
        draft.SetBackground("acolyte", null, "echoing_soul");

        // Assert
        draft.FeatProficiencyPicks.Should().BeEmpty();
    }

    [Fact]
    public void SetBackground_WithTheSameBackgroundAndFeat_KeepsTheFeatsProficiencyPicks()
    {
        // Arrange
        var draft = DraftWithEveryChoice();

        // Act
        draft.SetBackground("acolyte", null, "mist_walker");

        // Assert
        draft.FeatProficiencyPicks.Should().ContainSingle();
    }

    [Fact]
    public void SetRace_WithAnotherRace_ClearsTheFeatsProficiencyPicksWithTheImprovements()
    {
        // Arrange
        var draft = DraftWithEveryChoice();

        // Act
        draft.SetRace("dwarf");

        // Assert
        draft.SelectedFeatIds.Should().BeEmpty();
        draft.FeatProficiencyPicks.Should().BeEmpty();
    }

    [Fact]
    public void ChooseEdition_WhenTheEditionChanges_ClearsEveryNewChoice()
    {
        // Arrange
        var draft = DraftWithEveryChoice();

        // Act
        draft.ChooseEdition(RulesEdition.Edition2024);

        // Assert
        draft.Size.Should().BeNull();
        draft.RaceSpellIds.Should().BeEmpty();
        draft.BackgroundFeatId.Should().BeNull();
        draft.FeatSpells.Should().BeEmpty();
        draft.FeatProficiencyPicks.Should().BeEmpty();
    }

    [Fact]
    public void AllowBooks_WithTheBackgroundFeatsBookRemoved_ClearsTheFeatAndTheFeatsSpells()
    {
        // Arrange
        var draft = DraftWithEveryChoice();
        var sources = AllFrom(Handbook) with { Feats = new Dictionary<string, string> { ["magic_initiate_2014"] = Handbook, ["mist_walker"] = Other } };

        // Act
        draft.AllowBooks([], sources);

        // Assert
        draft.BackgroundFeatId.Should().BeNull();
        draft.FeatSpells.Should().BeEmpty();
        draft.FeatProficiencyPicks.Should().BeEmpty();
        draft.SelectedFeatIds.Should().Equal("magic_initiate_2014");
    }

    [Fact]
    public void AllowBooks_WithASpellsBookRemoved_DropsItFromTheRacesSpellsAndClearsTheFeatsSpells()
    {
        // Arrange
        var draft = DraftWithEveryChoice();
        var sources = AllFrom(Handbook) with
        {
            Spells = new Dictionary<string, string> { ["fire_bolt"] = Other, ["light"] = Handbook, ["guidance"] = Handbook, ["bless"] = Other },
        };

        // Act
        draft.AllowBooks([], sources);

        // Assert
        draft.RaceSpellIds.Should().BeEmpty();
        draft.FeatSpells.Should().BeEmpty();
    }

    [Fact]
    public void AllowBooks_WithEveryNewChoiceFromAnAllowedBook_KeepsThemAll()
    {
        // Arrange
        var draft = DraftWithEveryChoice();

        // Act
        draft.AllowBooks([], AllFrom(Handbook));

        // Assert
        draft.Size.Should().Be("Medium");
        draft.RaceSpellIds.Should().Equal("fire_bolt");
        draft.BackgroundFeatId.Should().Be("mist_walker");
        draft.FeatSpells.Should().ContainSingle();
    }

    private static CharacterDraft NewDraft() => CharacterDraft.CreateEmpty(Guid.NewGuid(), FakeTimeProvider);

    // One of each, whatever its rules: the draft keeps the picks and leaves the checks to the steps
    private static CharacterDraft DraftWithEveryChoice()
    {
        var draft = NewDraft();
        draft.ChooseEdition(RulesEdition.Edition2014);
        draft.SetNameAndLevel("Test Character", 4);
        draft.SetRace("elf", "high_elf", null, "Medium");
        draft.SetClass("fighter", "champion");
        draft.ChooseFightingStyles(["defense"]);
        draft.SetBackground("acolyte", null, "mist_walker");
        draft.SetFeats(
            ["magic_initiate_2014"],
            null,
            [new FeatProficiencyPick { FeatId = "mist_walker", ProficiencyIds = ["stealth"] }]);
        draft.ChooseBonusSpells(
            ["fire_bolt"],
            [new FeatSpellPick { FeatId = "magic_initiate_2014", ClassId = "cleric", SpellIds = ["light", "guidance", "bless"] }]);

        return draft;
    }

    private static DraftChoiceSources AllFrom(string source) => new(
        source,
        source,
        source,
        source,
        source,
        new Dictionary<string, string> { ["magic_initiate_2014"] = source, ["mist_walker"] = source, ["defense"] = source },
        new Dictionary<string, string> { ["fire_bolt"] = source, ["light"] = source, ["guidance"] = source, ["bless"] = source });
}
