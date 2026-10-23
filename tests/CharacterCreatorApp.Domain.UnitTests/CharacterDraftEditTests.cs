using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
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

    [Fact]
    public void LevelUp_AnEditOfABuild_RaisesTheLevelByOneAndKeepsEveryPick()
    {
        // Arrange - SetNameAndLevel would clear the improvements, spells and styles
        var build = CompletedBuild();
        var edit = CharacterDraft.StartEdit(build, FakeTimeProvider);

        // Act
        edit.LevelUp(Cleric(), Life(), StandardArray());

        // Assert
        edit.Level.Should().Be(5);
        edit.Should().BeEquivalentTo(build, options => options
            .ComparingByMembers<CharacterDraft>()
            .Excluding(d => d.Id)
            .Excluding(d => d.Level)
            .Excluding(d => d.IsCompleted)
            .Excluding(d => d.IsLevelUp)
            .Excluding(d => d.LevelUpChoices)
            .Excluding(d => d.ChoicesNeeded)
            .Excluding(d => d.CreatedAt)
            .Excluding(d => d.UpdatedAt));
    }

    [Fact]
    public void LevelUp_ALevelWithNothingElseToPick_AsksOnlyForTheHitPoints()
    {
        // Arrange
        var edit = CharacterDraft.StartEdit(CompletedBuild(), FakeTimeProvider);

        // Act
        edit.LevelUp(Cleric(), Life(), StandardArray());

        // Assert
        edit.IsLevelUp.Should().BeTrue();
        edit.LevelUpChoices.Should().Equal(DraftChoice.HitPoints);
        edit.ChoicesNeeded.Should().Equal(DraftChoice.HitPoints);
    }

    [Fact]
    public void LevelUp_ALevelWithAnImprovement_AsksForTheHitPointsAndTheImprovement()
    {
        // Arrange - the class data gives this cleric an improvement at 5
        var edit = CharacterDraft.StartEdit(CompletedBuild(), FakeTimeProvider);
        var cleric = Cleric();
        cleric.AsiLevels = [5];

        // Act
        edit.LevelUp(cleric, Life(), StandardArray());

        // Assert
        edit.LevelUpChoices.Should().Equal(DraftChoice.HitPoints, DraftChoice.Improvements);
        edit.ChoicesNeeded.Should().Equal(DraftChoice.HitPoints, DraftChoice.Improvements);
    }

    [Fact]
    public void LevelUp_AnOlderCharactersEdit_AlsoAsksForWhatTheEditCouldNotFillIn()
    {
        // Arrange - no build is kept, so the edit already marks the steps it could not fill in
        var character = Character.Create(
            UserId, "Mirabel", 4, RulesEdition.Edition2014, "elf", null, "cleric", "cleric_life", "sage", StandardArray(), FakeTimeProvider);
        var edit = CharacterDraft.StartEdit(character, [], FakeTimeProvider);

        // Act
        edit.LevelUp(Cleric(), Life(), character.AbilityScores);

        // Assert
        edit.LevelUpChoices.Should().Equal(DraftChoice.NotKeptByCharacter.Prepend(DraftChoice.HitPoints));
        edit.ChoicesNeeded.Should().Equal(edit.LevelUpChoices);
    }

    [Fact]
    public void LevelUp_AtTheHighestLevel_Throws()
    {
        // Arrange
        var character = Character.Create(
            UserId, "Mirabel", CharacterDraft.MaxLevel, RulesEdition.Edition2014, "elf", null, "cleric", "cleric_life", "sage",
            StandardArray(), FakeTimeProvider);
        var edit = CharacterDraft.StartEdit(character, [], FakeTimeProvider);

        // Act
        var act = () => edit.LevelUp(Cleric(), Life(), StandardArray());

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void LevelUp_ANewCharactersDraft_Throws()
    {
        // Arrange - only a saved character levels up
        var draft = CharacterDraft.CreateEmpty(UserId, FakeTimeProvider);
        draft.SetNameAndLevel("Mirabel", 4);
        draft.SetClass("cleric");

        // Act
        var act = () => draft.LevelUp(Cleric(), Life(), StandardArray());

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void LevelUp_TwiceOnOneDraft_Throws()
    {
        // Arrange - each level-up starts again from the saved character
        var edit = CharacterDraft.StartEdit(CompletedBuild(), FakeTimeProvider);
        edit.LevelUp(Cleric(), Life(), StandardArray());

        // Act
        var act = () => edit.LevelUp(Cleric(), Life(), StandardArray());

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void LevelUp_WithAnotherClass_Throws()
    {
        // Arrange
        var edit = CharacterDraft.StartEdit(CompletedBuild(), FakeTimeProvider);
        var wizard = Cleric();
        wizard.Id = "wizard";

        // Act
        var act = () => edit.LevelUp(wizard, Life(), StandardArray());

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetHitDieResult_OnALevelUp_KeepsItForTheNewLevelAndTakesAwayItsMark()
    {
        // Arrange
        var edit = CharacterDraft.StartEdit(CompletedBuild(), FakeTimeProvider);
        edit.LevelUp(Cleric(), Life(), StandardArray());

        // Act
        edit.SetHitDieResult(7);

        // Assert
        edit.HitDieResults.Should().Equal(new Dictionary<int, int> { [5] = 7 });
        edit.ChoicesNeeded.Should().BeEmpty();
        edit.LevelUpChoices.Should().Equal([DraftChoice.HitPoints], "the level-up still shows the step");
    }

    [Fact]
    public void SetHitDieResult_NotALevelUp_Throws()
    {
        // Arrange
        var edit = CharacterDraft.StartEdit(CompletedBuild(), FakeTimeProvider);

        // Act
        var act = () => edit.SetHitDieResult(7);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SetHitDieResult_BelowOne_Throws()
    {
        // Arrange
        var edit = CharacterDraft.StartEdit(CompletedBuild(), FakeTimeProvider);
        edit.LevelUp(Cleric(), Life(), StandardArray());

        // Act
        var act = () => edit.SetHitDieResult(0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MarkAsCompleted_ALevelUp_LeavesABuildThatIsNoLevelUp()
    {
        // Arrange - an edit of it later is a plain edit
        var edit = CharacterDraft.StartEdit(CompletedBuild(), FakeTimeProvider);
        edit.LevelUp(Cleric(), Life(), StandardArray());
        edit.SetHitDieResult(8);

        // Act
        edit.MarkAsCompleted(CharacterId);

        // Assert
        edit.IsLevelUp.Should().BeFalse();
        edit.LevelUpChoices.Should().BeEmpty();
        edit.HitDieResults.Should().Equal(new Dictionary<int, int> { [5] = 8 });
    }

    [Fact]
    public void StartEdit_FromALevelledUpBuild_CopiesItsHitDieResults()
    {
        // Arrange
        var levelUp = CharacterDraft.StartEdit(CompletedBuild(), FakeTimeProvider);
        levelUp.LevelUp(Cleric(), Life(), StandardArray());
        levelUp.SetHitDieResult(3);
        levelUp.MarkAsCompleted(CharacterId);

        // Act
        var edit = CharacterDraft.StartEdit(levelUp, FakeTimeProvider);
        edit.HitDieResults[5] = 8;

        // Assert
        edit.IsLevelUp.Should().BeFalse();
        levelUp.HitDieResults.Should().Equal(new Dictionary<int, int> { [5] = 3 }, "the edit shares no list with its build");
    }

    [Fact]
    public void SetNameAndLevel_ALowerLevel_DropsTheHitDieResultsAboveIt()
    {
        // Arrange - a character levelled up to 5 then 6
        var levelUp = CharacterDraft.StartEdit(CompletedBuild(), FakeTimeProvider);
        levelUp.LevelUp(Cleric(), Life(), StandardArray());
        levelUp.SetHitDieResult(3);
        levelUp.MarkAsCompleted(CharacterId);
        var secondLevelUp = CharacterDraft.StartEdit(levelUp, FakeTimeProvider);
        secondLevelUp.LevelUp(Cleric(), Life(), StandardArray());
        secondLevelUp.SetHitDieResult(5);
        secondLevelUp.MarkAsCompleted(CharacterId);
        var edit = CharacterDraft.StartEdit(secondLevelUp, FakeTimeProvider);

        // Act
        edit.SetNameAndLevel("Brother Aldo", 5);

        // Assert
        edit.HitDieResults.Should().Equal(new Dictionary<int, int> { [5] = 3 });
    }

    [Fact]
    public void SetClass_AnotherClass_DropsTheHitDieResults()
    {
        // Arrange - they were rolled on the old class's die
        var levelUp = CharacterDraft.StartEdit(CompletedBuild(), FakeTimeProvider);
        levelUp.LevelUp(Cleric(), Life(), StandardArray());
        levelUp.SetHitDieResult(3);
        levelUp.MarkAsCompleted(CharacterId);
        var edit = CharacterDraft.StartEdit(levelUp, FakeTimeProvider);

        // Act
        edit.SetClass("wizard");

        // Assert
        edit.HitDieResults.Should().BeEmpty();
    }

    [Fact]
    public void SetSubclass_WhereThereWasNone_KeepsTheSpellsAndFightingStylesAndTakesAwayItsMark()
    {
        // Arrange - a level-up to a fighter's third level keeps its style from level 1
        var draft = CharacterDraft.CreateEmpty(UserId, FakeTimeProvider);
        draft.SetClass("fighter");
        draft.ChooseFightingStyles(["defense_2014"]);
        draft.SetSpells(["fire_bolt"]);

        // Act
        draft.SetSubclass("eldritch_knight");

        // Assert
        draft.SubclassId.Should().Be("eldritch_knight");
        draft.FightingStyleIds.Should().Equal("defense_2014");
        draft.SelectedSpellIds.Should().Equal("fire_bolt");
    }

    // The 2014 cleric's rules data as far as a level-up reads it: its subclass comes at level 1,
    // and no level has an improvement unless a test gives it one
    private static Class Cleric() => new()
    {
        Id = "cleric",
        Name = "Cleric",
        Edition = RulesEdition.Edition2014,
        HitDie = "1d8",
        SubclassLevel = 1,
    };

    private static Subclass Life() => new() { Id = "cleric_life", Name = "Life Domain", ClassId = "cleric" };

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
