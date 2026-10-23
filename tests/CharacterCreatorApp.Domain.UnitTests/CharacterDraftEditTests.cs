using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

namespace CharacterCreatorApp.Domain.UnitTests;

public class CharacterDraftEditTests
{
    private static readonly FakeTimeProvider FakeTimeProvider = new(new DateTimeOffset(2026, 10, 23, 9, 0, 0, TimeSpan.Zero));

    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly Guid CharacterId = Guid.CreateVersion7();

    [Fact]
    public void StartEdit_FromABuild_CopiesEveryChoiceIntoAnUnfinishedDraftOfTheCharacter()
    {
        // Arrange
        var build = CompletedBuild();

        // Act
        var edit = CharacterDraft.StartEdit(build, FakeTimeProvider);

        // Assert
        edit.Id.Should().NotBe(build.Id);
        edit.IsCompleted.Should().BeFalse();
        edit.CharacterId.Should().Be(CharacterId);
        edit.CreatedByUserId.Should().Be(UserId);
        edit.ChoicesNeeded.Should().BeEmpty();
        edit.Should().BeEquivalentTo(build, options => options
            .ComparingByMembers<CharacterDraft>()
            .Excluding(d => d.Id)
            .Excluding(d => d.IsCompleted)
            .Excluding(d => d.CreatedAt)
            .Excluding(d => d.UpdatedAt));
    }

    [Fact]
    public void StartEdit_FromABuild_SharesNoListWithIt()
    {
        // Arrange - an edit left unsaved must leave the build as it was
        var build = CompletedBuild();
        var edit = CharacterDraft.StartEdit(build, FakeTimeProvider);

        // Act
        edit.ToolPicks[0].Add("lute");
        edit.FeatSpells[0].SpellIds.Add("light");
        edit.AbilityScoreImprovements[0]["DEX"] = 2;
        edit.BaseAbilityScores["STR"] = 8;

        // Assert
        build.ToolPicks[0].Should().Equal("flute");
        build.FeatSpells[0].SpellIds.Should().Equal("guidance", "bless");
        build.AbilityScoreImprovements[0].Should().BeEquivalentTo(new Dictionary<string, int> { ["STR"] = 2 });
        build.BaseAbilityScores["STR"].Should().Be(15);
    }

    [Fact]
    public void StartEdit_FromABuildWithoutTheScoreMethod_MarksTheAbilityScores()
    {
        // Arrange - drafts finalized before the method was kept have none
        var build = CharacterDraft.CreateEmpty(UserId, FakeTimeProvider);
        build.MarkAsCompleted(CharacterId);

        // Act
        var edit = CharacterDraft.StartEdit(build, FakeTimeProvider);

        // Assert
        edit.ChoicesNeeded.Should().Equal(DraftChoice.AbilityScores);
    }

    [Fact]
    public void StartEdit_FromAnUnfinishedDraft_Throws()
    {
        // Arrange
        var draft = CharacterDraft.CreateEmpty(UserId, FakeTimeProvider);

        // Act
        var act = () => CharacterDraft.StartEdit(draft, FakeTimeProvider);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void StartEdit_FromACharacter_KeepsWhatItStoresAndMarksTheRest()
    {
        // Arrange
        var character = Character.Create(
            UserId, "Mirabel", 4, RulesEdition.Edition2014, "elf", "elf_high", "wizard", "wizard_evocation", "sage",
            new Dictionary<string, int> { ["STR"] = 8, ["DEX"] = 16, ["CON"] = 14, ["INT"] = 17, ["WIS"] = 12, ["CHA"] = 10 },
            FakeTimeProvider);
        character.SetSize("Medium");

        // Act
        var edit = CharacterDraft.StartEdit(character, ["XGE", "PHB"], FakeTimeProvider);

        // Assert
        edit.CharacterId.Should().Be(character.Id);
        edit.CreatedByUserId.Should().Be(UserId);
        edit.IsCompleted.Should().BeFalse();
        edit.Name.Should().Be("Mirabel");
        edit.Level.Should().Be(4);
        edit.Edition.Should().Be(RulesEdition.Edition2014);
        edit.AllowedSources.Should().Equal("PHB", "XGE");
        edit.RaceId.Should().Be("elf");
        edit.SubraceId.Should().Be("elf_high");
        edit.Size.Should().Be("Medium");
        edit.ClassId.Should().Be("wizard");
        edit.SubclassId.Should().Be("wizard_evocation");
        edit.BackgroundId.Should().Be("sage");
        edit.BaseAbilityScores.Should().BeEmpty("the character keeps only its final scores");
        edit.SelectedSpellIds.Should().BeEmpty();
        edit.ChoicesNeeded.Should().Equal(DraftChoice.NotKeptByCharacter);
    }

    // A row per step: saving it takes its mark away and leaves the others
    public static TheoryData<string, Action<CharacterDraft>> StepSaves => new()
    {
        { DraftChoice.Race, d => d.SetRace("elf", "elf_high") },
        { DraftChoice.Race, d => d.SetSubrace("elf_wood") },
        { DraftChoice.Background, d => d.SetBackground("sage") },
        { DraftChoice.Languages, d => d.SetLanguages(["dwarvish-phb"]) },
        { DraftChoice.AbilityScores, d => d.SetBaseAbilityScores(StandardArray(), AbilityScoreMethod.StandardArray) },
        { DraftChoice.Proficiencies, d => d.SetClassProficiencies(["arcana", "history"]) },
        { DraftChoice.Proficiencies, d => d.ChooseTools([]) },
        { DraftChoice.Improvements, d => d.SetFeats([]) },
        { DraftChoice.Improvements, d => d.SetAbilityScoreImprovements([]) },
        { DraftChoice.Improvements, d => d.ChooseFightingStyles([]) },
        { DraftChoice.Spells, d => d.SetSpells(["fire_bolt"]) },
        { DraftChoice.Spells, d => d.ChooseBonusSpells([], []) },
    };

    [Theory]
    [MemberData(nameof(StepSaves))]
    public void SavingAStep_TakesAwayOnlyItsMark(string choice, Action<CharacterDraft> save)
    {
        // Arrange
        var character = Character.Create(
            UserId, "Mirabel", 4, RulesEdition.Edition2014, "elf", "elf_high", "wizard", "wizard_evocation", "sage",
            StandardArray(), FakeTimeProvider);
        var edit = CharacterDraft.StartEdit(character, [], FakeTimeProvider);

        // Act
        save(edit);

        // Assert
        edit.ChoicesNeeded.Should().Equal(DraftChoice.NotKeptByCharacter.Where(c => c != choice));
    }

    [Fact]
    public void MarkAsCompleted_AnEdit_ClearsItsMarks()
    {
        // Arrange - finalize passes only once nothing is missing
        var character = Character.Create(
            UserId, "Mirabel", 4, RulesEdition.Edition2014, "elf", null, "wizard", null, "sage", StandardArray(), FakeTimeProvider);
        var edit = CharacterDraft.StartEdit(character, [], FakeTimeProvider);

        // Act
        edit.MarkAsCompleted(character.Id);

        // Assert
        edit.ChoicesNeeded.Should().BeEmpty();
        edit.IsCompleted.Should().BeTrue();
        edit.CharacterId.Should().Be(character.Id);
    }

    private static Dictionary<string, int> StandardArray() =>
        new() { ["STR"] = 15, ["DEX"] = 14, ["CON"] = 13, ["INT"] = 12, ["WIS"] = 10, ["CHA"] = 8 };

    // A finalized draft with a choice in every list, nested ones included
    private static CharacterDraft CompletedBuild()
    {
        var build = CharacterDraft.CreateEmpty(UserId, FakeTimeProvider);
        build.SetNameAndLevel("Brother Aldo", 4);
        build.ChooseEdition(RulesEdition.Edition2014);
        build.AllowBooks(["XGE"], new DraftChoiceSources(null, null, null, null, null, new Dictionary<string, string>(), new Dictionary<string, string>()));
        build.SetRace("half_elf", abilityIncreases: new Dictionary<string, int> { ["WIS"] = 1, ["CON"] = 1 });
        build.SetRaceSkills(["insight", "perception"]);
        build.SetClass("cleric", "cleric_life");
        build.SetBackground("acolyte");
        build.SetBaseAbilityScores(StandardArray(), AbilityScoreMethod.StandardArray);
        build.SetClassProficiencies(["medicine", "religion"]);
        build.SetLanguages(["elvish-phb", "dwarvish-phb"]);
        build.ChooseTools([["flute"]]);
        build.SetFeats(["magic_initiate_2014"], [[]], [new FeatProficiencyPick { FeatId = "skilled_2014", ProficiencyIds = ["arcana"] }]);
        build.SetAbilityScoreImprovements([new Dictionary<string, int> { ["STR"] = 2 }]);
        build.SetSpells(["sacred_flame", "cure_wounds"]);
        build.ChooseBonusSpells(["light"], [new FeatSpellPick { FeatId = "magic_initiate_2014", ClassId = "cleric", SpellIds = ["guidance", "bless"] }]);
        build.MarkAsCompleted(CharacterId);
        return build;
    }
}
