using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

namespace CharacterCreatorApp.Domain.UnitTests;

public class CharacterDraftBooksTests
{
    private const string Handbook = "XPHB";
    private const string Other = "EFA";

    private static readonly FakeTimeProvider FakeTimeProvider = new(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero));

    [Theory]
    [InlineData(RulesEdition.Edition2014, "PHB")]
    [InlineData(RulesEdition.Edition2024, "XPHB")]
    public void PlayersHandbook_ForEachEdition_IsItsHandbooksCode(string edition, string expected)
    {
        // Act
        var code = RulesEdition.PlayersHandbook(edition);

        // Assert
        code.Should().Be(expected);
    }

    [Fact]
    public void CreateEmpty_AllowsOnlyThe2024PlayersHandbook()
    {
        // Act
        var draft = CharacterDraft.CreateEmpty(Guid.NewGuid(), FakeTimeProvider);

        // Assert
        draft.AllowedSources.Should().Equal(Handbook);
    }

    [Fact]
    public void ChooseEdition_WhenTheEditionChanges_AllowsOnlyTheNewEditionsPlayersHandbook()
    {
        // Arrange
        var draft = CharacterDraft.CreateEmpty(Guid.NewGuid(), FakeTimeProvider);
        draft.AllowBooks([Other], NothingChosen());

        // Act
        draft.ChooseEdition(RulesEdition.Edition2014);

        // Assert
        draft.AllowedSources.Should().Equal("PHB");
    }

    [Fact]
    public void ChooseEdition_WithTheSameEdition_KeepsTheBooks()
    {
        // Arrange
        var draft = CharacterDraft.CreateEmpty(Guid.NewGuid(), FakeTimeProvider);
        draft.AllowBooks([Other], NothingChosen());

        // Act
        draft.ChooseEdition(RulesEdition.Edition2024);

        // Assert
        draft.AllowedSources.Should().Equal(Handbook, Other);
    }

    [Fact]
    public void AllowBooks_WithoutThePlayersHandbook_StillAllowsIt()
    {
        // Arrange
        var draft = CharacterDraft.CreateEmpty(Guid.NewGuid(), FakeTimeProvider);

        // Act
        draft.AllowBooks([Other, Other], NothingChosen());

        // Assert
        draft.AllowedSources.Should().Equal(Handbook, Other);
        draft.AllowsSource(Other).Should().BeTrue();
        draft.AllowsSource("XGE").Should().BeFalse();
    }

    [Fact]
    public void AllowBooks_WithEveryChoiceFromAnAllowedBook_KeepsThemAll()
    {
        // Arrange
        var draft = DraftWithEveryChoice();

        // Act
        draft.AllowBooks([], AllFrom(Handbook));

        // Assert
        draft.RaceId.Should().Be("elf_2024");
        draft.SubraceId.Should().Be("elf_drow_lineage_2024");
        draft.ClassId.Should().Be("fighter-2024");
        draft.SubclassId.Should().Be("eldritch-knight-2024");
        draft.BackgroundId.Should().Be("soldier_2024");
        draft.SelectedFeatIds.Should().Equal("alert_2024");
        draft.FightingStyleIds.Should().Equal("defense_2024");
        draft.SelectedSpellIds.Should().Equal("fire_bolt_2024", "shield_2024");
        draft.SelectedLanguageIds.Should().Equal("elvish-xphb");
    }

    [Fact]
    public void AllowBooks_WithTheRacesBookRemoved_ClearsTheRaceAndWhatWasPickedForIt()
    {
        // Arrange
        var draft = DraftWithEveryChoice();

        // Act
        draft.AllowBooks([], AllFrom(Handbook) with { Race = Other });

        // Assert
        draft.RaceId.Should().BeNull();
        draft.SubraceId.Should().BeNull();
        draft.RaceAbilityIncreases.Should().BeEmpty();
        draft.SelectedRaceSkillIds.Should().BeEmpty();
        draft.SelectedLanguageIds.Should().BeEmpty();
        draft.SelectedFeatIds.Should().BeEmpty();
        draft.AbilityScoreImprovements.Should().BeEmpty();
        draft.ClassId.Should().Be("fighter-2024");
        draft.BackgroundId.Should().Be("soldier_2024");
    }

    [Fact]
    public void AllowBooks_WithOnlyTheSubracesBookRemoved_ClearsTheSubraceAndItsIncreases()
    {
        // Arrange
        var draft = DraftWithEveryChoice();

        // Act
        draft.AllowBooks([], AllFrom(Handbook) with { Subrace = Other });

        // Assert
        draft.RaceId.Should().Be("elf_2024");
        draft.SubraceId.Should().BeNull();
        draft.RaceAbilityIncreases.Should().BeEmpty();
        draft.SelectedFeatIds.Should().BeEmpty();
        draft.SelectedRaceSkillIds.Should().Equal("perception");
    }

    [Fact]
    public void AllowBooks_WithTheClassesBookRemoved_ClearsTheClassAndWhatWasPickedForIt()
    {
        // Arrange
        var draft = DraftWithEveryChoice();

        // Act
        draft.AllowBooks([], AllFrom(Handbook) with { Class = Other });

        // Assert
        draft.ClassId.Should().BeNull();
        draft.SubclassId.Should().BeNull();
        draft.SelectedClassProficiencyIds.Should().BeEmpty();
        draft.SelectedSpellIds.Should().BeEmpty();
        draft.FightingStyleIds.Should().BeEmpty();
        draft.SelectedFeatIds.Should().BeEmpty();
        draft.AbilityScoreImprovements.Should().BeEmpty();
        draft.RaceId.Should().Be("elf_2024");
    }

    [Fact]
    public void AllowBooks_WithOnlyTheSubclassesBookRemoved_ClearsTheSubclassItsSpellsAndStyles()
    {
        // Arrange
        var draft = DraftWithEveryChoice();

        // Act
        draft.AllowBooks([], AllFrom(Handbook) with { Subclass = Other });

        // Assert
        draft.ClassId.Should().Be("fighter-2024");
        draft.SubclassId.Should().BeNull();
        draft.SelectedSpellIds.Should().BeEmpty();
        draft.FightingStyleIds.Should().BeEmpty();
        draft.SelectedClassProficiencyIds.Should().Equal("athletics");
    }

    [Fact]
    public void AllowBooks_WithTheBackgroundsBookRemoved_ClearsTheBackgroundItsIncreasesAndTheLanguages()
    {
        // Arrange
        var draft = DraftWithEveryChoice();

        // Act
        draft.AllowBooks([], AllFrom(Handbook) with { Background = Other });

        // Assert
        draft.BackgroundId.Should().BeNull();
        draft.BackgroundAbilityIncreases.Should().BeEmpty();
        draft.SelectedLanguageIds.Should().BeEmpty();
        draft.RaceId.Should().Be("elf_2024");
    }

    [Fact]
    public void AllowBooks_WithAFeatsBookRemoved_ClearsTheImprovementsStep()
    {
        // Arrange
        var draft = DraftWithEveryChoice();
        var sources = AllFrom(Handbook);

        // Act
        draft.AllowBooks([], sources with { Feats = new Dictionary<string, string> { ["alert_2024"] = Other, ["defense_2024"] = Handbook } });

        // Assert
        draft.SelectedFeatIds.Should().BeEmpty();
        draft.FeatAbilityIncreases.Should().BeEmpty();
        draft.AbilityScoreImprovements.Should().BeEmpty();
        draft.FightingStyleIds.Should().Equal("defense_2024");
    }

    [Fact]
    public void AllowBooks_WithAStylesAndASpellsBookRemoved_DropsOnlyThosePicks()
    {
        // Arrange
        var draft = DraftWithEveryChoice();
        var sources = AllFrom(Handbook) with
        {
            Feats = new Dictionary<string, string> { ["alert_2024"] = Handbook, ["defense_2024"] = Other },
            Spells = new Dictionary<string, string> { ["fire_bolt_2024"] = Handbook, ["shield_2024"] = Other },
        };

        // Act
        draft.AllowBooks([], sources);

        // Assert
        draft.FightingStyleIds.Should().BeEmpty();
        draft.SelectedSpellIds.Should().Equal("fire_bolt_2024");
        draft.SelectedFeatIds.Should().Equal("alert_2024");
        draft.SubclassId.Should().Be("eldritch-knight-2024");
    }

    [Fact]
    public void AllowBooks_WithAChoiceWhoseBookIsUnknown_KeepsItForFinalizeToReport()
    {
        // Arrange - a feat or spell the reference data no longer has, after a reseed
        var draft = DraftWithEveryChoice();
        var sources = AllFrom(Handbook) with { Feats = new Dictionary<string, string>(), Spells = new Dictionary<string, string>() };

        // Act
        draft.AllowBooks([], sources);

        // Assert
        draft.SelectedFeatIds.Should().Equal("alert_2024");
        draft.FightingStyleIds.Should().Equal("defense_2024");
        draft.SelectedSpellIds.Should().Equal("fire_bolt_2024", "shield_2024");
    }

    [Fact]
    public void ClearMissingChoices_WithEveryChoiceStillThere_KeepsThemAll()
    {
        // Arrange
        var draft = DraftWithEveryChoice();

        // Act
        draft.ClearMissingChoices(AllFrom(Other));

        // Assert - which book a choice is from does not matter here, only that it has one
        draft.RaceId.Should().Be("elf_2024");
        draft.SubraceId.Should().Be("elf_drow_lineage_2024");
        draft.ClassId.Should().Be("fighter-2024");
        draft.SubclassId.Should().Be("eldritch-knight-2024");
        draft.BackgroundId.Should().Be("soldier_2024");
        draft.SelectedFeatIds.Should().Equal("alert_2024");
        draft.FightingStyleIds.Should().Equal("defense_2024");
        draft.SelectedSpellIds.Should().Equal("fire_bolt_2024", "shield_2024");
        draft.SelectedLanguageIds.Should().Equal("elvish-xphb");
    }

    [Fact]
    public void ClearMissingChoices_WithTheSubclassGone_ClearsTheSubclassItsSpellsAndStyles()
    {
        // Arrange
        var draft = DraftWithEveryChoice();

        // Act
        draft.ClearMissingChoices(AllFrom(Handbook) with { Subclass = null });

        // Assert
        draft.ClassId.Should().Be("fighter-2024");
        draft.SubclassId.Should().BeNull();
        draft.SelectedSpellIds.Should().BeEmpty();
        draft.FightingStyleIds.Should().BeEmpty();
        draft.SelectedClassProficiencyIds.Should().Equal("athletics");
    }

    [Fact]
    public void ClearMissingChoices_WithTheRaceGone_ClearsTheRaceAndWhatWasPickedForIt()
    {
        // Arrange
        var draft = DraftWithEveryChoice();

        // Act
        draft.ClearMissingChoices(AllFrom(Handbook) with { Race = null, Subrace = null });

        // Assert
        draft.RaceId.Should().BeNull();
        draft.SubraceId.Should().BeNull();
        draft.RaceAbilityIncreases.Should().BeEmpty();
        draft.SelectedRaceSkillIds.Should().BeEmpty();
        draft.SelectedLanguageIds.Should().BeEmpty();
        draft.SelectedFeatIds.Should().BeEmpty();
        draft.ClassId.Should().Be("fighter-2024");
        draft.BackgroundId.Should().Be("soldier_2024");
    }

    [Fact]
    public void ClearMissingChoices_WithTheClassGone_ClearsTheClassAndWhatWasPickedForIt()
    {
        // Arrange
        var draft = DraftWithEveryChoice();

        // Act
        draft.ClearMissingChoices(AllFrom(Handbook) with { Class = null, Subclass = null });

        // Assert
        draft.ClassId.Should().BeNull();
        draft.SubclassId.Should().BeNull();
        draft.SelectedClassProficiencyIds.Should().BeEmpty();
        draft.SelectedSpellIds.Should().BeEmpty();
        draft.FightingStyleIds.Should().BeEmpty();
        draft.AbilityScoreImprovements.Should().BeEmpty();
        draft.RaceId.Should().Be("elf_2024");
    }

    [Fact]
    public void ClearMissingChoices_WithTheBackgroundGone_ClearsTheBackgroundItsIncreasesAndTheLanguages()
    {
        // Arrange
        var draft = DraftWithEveryChoice();

        // Act
        draft.ClearMissingChoices(AllFrom(Handbook) with { Background = null });

        // Assert
        draft.BackgroundId.Should().BeNull();
        draft.BackgroundAbilityIncreases.Should().BeEmpty();
        draft.SelectedLanguageIds.Should().BeEmpty();
        draft.RaceId.Should().Be("elf_2024");
    }

    [Fact]
    public void ClearMissingChoices_WithAFeatAndASpellGone_ClearsTheImprovementsAndDropsTheSpell()
    {
        // Arrange
        var draft = DraftWithEveryChoice();
        var sources = AllFrom(Handbook) with
        {
            Feats = new Dictionary<string, string> { ["defense_2024"] = Handbook },
            Spells = new Dictionary<string, string> { ["fire_bolt_2024"] = Handbook },
        };

        // Act
        draft.ClearMissingChoices(sources);

        // Assert
        draft.SelectedFeatIds.Should().BeEmpty();
        draft.AbilityScoreImprovements.Should().BeEmpty();
        draft.FightingStyleIds.Should().Equal("defense_2024");
        draft.SelectedSpellIds.Should().Equal("fire_bolt_2024");
        draft.SubclassId.Should().Be("eldritch-knight-2024");
    }

    private static CharacterDraft DraftWithEveryChoice()
    {
        var draft = CharacterDraft.CreateEmpty(Guid.NewGuid(), FakeTimeProvider);
        draft.SetNameAndLevel("Test Character", 4);
        draft.SetRace("elf_2024", "elf_drow_lineage_2024", new Dictionary<string, int> { ["DEX"] = 1 });
        draft.SetRaceSkills(["perception"]);
        draft.SetClass("fighter-2024", "eldritch-knight-2024");
        draft.SetClassProficiencies(["athletics"]);
        draft.SetBackground("soldier_2024", new Dictionary<string, int> { ["STR"] = 2, ["CON"] = 1 });
        draft.SetLanguages(["elvish-xphb"]);
        draft.SetFeats(["alert_2024"]);
        draft.SetAbilityScoreImprovements([new Dictionary<string, int> { ["STR"] = 2 }]);
        draft.ChooseFightingStyles(["defense_2024"]);
        draft.SetSpells(["fire_bolt_2024", "shield_2024"]);

        return draft;
    }

    private static DraftChoiceSources AllFrom(string source) => new(
        source,
        source,
        source,
        source,
        source,
        new Dictionary<string, string> { ["alert_2024"] = source, ["defense_2024"] = source },
        new Dictionary<string, string> { ["fire_bolt_2024"] = source, ["shield_2024"] = source });

    private static DraftChoiceSources NothingChosen() =>
        new(null, null, null, null, null, new Dictionary<string, string>(), new Dictionary<string, string>());
}
