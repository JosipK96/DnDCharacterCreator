using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

namespace CharacterCreatorApp.Domain.UnitTests;

public class CharacterDraftTests
{
    private static readonly FakeTimeProvider FakeTimeProvider = new(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero));

    [Fact]
    public void CreateEmpty_SetsCorrectDefaults()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var draft = CharacterDraft.CreateEmpty(userId, FakeTimeProvider);

        // Assert
        draft.Should().NotBeNull();
        draft.Id.Should().NotBeEmpty();
        draft.CreatedByUserId.Should().Be(userId);
        draft.IsCompleted.Should().BeFalse();
        draft.Name.Should().BeNull();
        draft.Level.Should().BeNull();
        draft.RaceId.Should().BeNull();
        draft.SubraceId.Should().BeNull();
        draft.ClassId.Should().BeNull();
        draft.SubclassId.Should().BeNull();
        draft.BackgroundId.Should().BeNull();
        draft.BaseAbilityScores.Should().BeEmpty();
        draft.SelectedClassProficiencyIds.Should().BeEmpty();
        draft.SelectedFeatIds.Should().BeEmpty();
        draft.SelectedSpellIds.Should().BeEmpty();
    }

    [Fact]
    public void CreateEmpty_UsesTimeProvider()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedTime = FakeTimeProvider.GetUtcNow();

        // Act
        var draft = CharacterDraft.CreateEmpty(userId, FakeTimeProvider);

        // Assert
        draft.CreatedAt.Should().Be(expectedTime);
    }

    [Fact]
    public void CreateEmpty_GivesAVersion7IdStartingWithTheCreationTime()
    {
        // Act
        var draft = CharacterDraft.CreateEmpty(Guid.NewGuid(), FakeTimeProvider);

        // Assert
        draft.Id.Version.Should().Be(7);
        UuidV7.TimestampOf(draft.Id).Should().Be(FakeTimeProvider.GetUtcNow());
    }

    [Fact]
    public void CreateEmpty_ALaterDraft_GetsAnIdThatSortsAfterTheEarlierOne()
    {
        // Arrange - its own clock, since advancing the shared one would move every other test
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero));
        var earlier = CharacterDraft.CreateEmpty(Guid.NewGuid(), timeProvider);
        timeProvider.Advance(TimeSpan.FromMilliseconds(1));

        // Act
        var later = CharacterDraft.CreateEmpty(Guid.NewGuid(), timeProvider);

        // Assert - PostgreSQL orders uuids the way their text orders, byte by byte
        string.CompareOrdinal(earlier.Id.ToString(), later.Id.ToString()).Should().BeNegative();
    }

    [Fact]
    public void SetNameAndLevel_UpdatesProperties()
    {
        // Arrange
        var draft = CreateTestDraft();
        var name = "Gandalf";
        var level = 10;

        // Act
        draft.SetNameAndLevel(name, level);

        // Assert
        draft.Name.Should().Be(name);
        draft.Level.Should().Be(level);
    }

    [Fact]
    public void SetClass_UpdatesClassId()
    {
        // Arrange
        var draft = CreateTestDraft();
        var classId = "wizard";

        // Act
        draft.SetClass(classId);

        // Assert
        draft.ClassId.Should().Be(classId);
        draft.SubclassId.Should().BeNull();
    }

    [Fact]
    public void SetClass_WithSubclass_UpdatesBoth()
    {
        // Arrange
        var draft = CreateTestDraft();
        var classId = "wizard";
        var subclassId = "evocation";

        // Act
        draft.SetClass(classId, subclassId);

        // Assert
        draft.ClassId.Should().Be(classId);
        draft.SubclassId.Should().Be(subclassId);
    }

    [Fact]
    public void SetSubclass_UpdatesSubclassOnly()
    {
        // Arrange
        var draft = CreateTestDraft();
        draft.SetClass("fighter");
        var subclassId = "champion";

        // Act
        draft.SetSubclass(subclassId);

        // Assert
        draft.ClassId.Should().Be("fighter"); // Should remain unchanged
        draft.SubclassId.Should().Be(subclassId);
    }

    [Fact]
    public void SetSubclass_CanSetToNull()
    {
        // Arrange
        var draft = CreateTestDraft();
        draft.SetClass("wizard", "evocation");

        // Act
        draft.SetSubclass(null);

        // Assert
        draft.ClassId.Should().Be("wizard");
        draft.SubclassId.Should().BeNull();
    }

    [Fact]
    public void SetRace_UpdatesRaceId()
    {
        // Arrange
        var draft = CreateTestDraft();
        var raceId = "elf";

        // Act
        draft.SetRace(raceId);

        // Assert
        draft.RaceId.Should().Be(raceId);
        draft.SubraceId.Should().BeNull();
    }

    [Fact]
    public void SetRace_WithSubrace_UpdatesBoth()
    {
        // Arrange
        var draft = CreateTestDraft();
        var raceId = "elf";
        var subraceId = "high-elf";

        // Act
        draft.SetRace(raceId, subraceId);

        // Assert
        draft.RaceId.Should().Be(raceId);
        draft.SubraceId.Should().Be(subraceId);
    }

    [Fact]
    public void SetSubrace_UpdatesSubraceOnly()
    {
        // Arrange
        var draft = CreateTestDraft();
        draft.SetRace("elf");
        var subraceId = "wood-elf";

        // Act
        draft.SetSubrace(subraceId);

        // Assert
        draft.RaceId.Should().Be("elf"); // Should remain unchanged
        draft.SubraceId.Should().Be(subraceId);
    }

    [Fact]
    public void SetSubrace_CanSetToNull()
    {
        // Arrange
        var draft = CreateTestDraft();
        draft.SetRace("elf", "high-elf");

        // Act
        draft.SetSubrace(null);

        // Assert
        draft.RaceId.Should().Be("elf");
        draft.SubraceId.Should().BeNull();
    }

    [Fact]
    public void SetBackground_UpdatesBackgroundId()
    {
        // Arrange
        var draft = CreateTestDraft();
        var backgroundId = "soldier";

        // Act
        draft.SetBackground(backgroundId);

        // Assert
        draft.BackgroundId.Should().Be(backgroundId);
    }

    [Fact]
    public void SetBaseAbilityScores_UpdatesScores()
    {
        // Arrange
        var draft = CreateTestDraft();
        var scores = new Dictionary<string, int>
        {
            ["strength"] = 15,
            ["dexterity"] = 14,
            ["constitution"] = 13,
            ["intelligence"] = 12,
            ["wisdom"] = 10,
            ["charisma"] = 8
        };

        // Act
        draft.SetBaseAbilityScores(scores, AbilityScoreMethod.StandardArray);

        // Assert
        draft.BaseAbilityScores.Should().BeEquivalentTo(scores);
        draft.BaseAbilityScoreMethod.Should().Be(AbilityScoreMethod.StandardArray);
    }

    [Fact]
    public void SetBaseAbilityScores_CreatesNewDictionary()
    {
        // Arrange
        var draft = CreateTestDraft();
        var scores = new Dictionary<string, int>
        {
            ["strength"] = 15,
            ["dexterity"] = 14,
            ["constitution"] = 13,
            ["intelligence"] = 12,
            ["wisdom"] = 10,
            ["charisma"] = 8
        };

        // Act
        draft.SetBaseAbilityScores(scores, AbilityScoreMethod.Rolled);
        scores["strength"] = 20; // Modify original

        // Assert - Draft's scores should not be affected
        draft.BaseAbilityScores["strength"].Should().Be(15);
    }

    [Fact]
    public void SetClassProficiencies_UpdatesProficiencies()
    {
        // Arrange
        var draft = CreateTestDraft();
        var proficiencies = new List<string> { "martial-weapons", "light-armor", "shields" };

        // Act
        draft.SetClassProficiencies(proficiencies);

        // Assert
        draft.SelectedClassProficiencyIds.Should().BeEquivalentTo(proficiencies);
    }

    [Fact]
    public void SetFeats_UpdatesFeats()
    {
        // Arrange
        var draft = CreateTestDraft();
        var feats = new List<string> { "alert", "lucky", "war-caster" };

        // Act
        draft.SetFeats(feats);

        // Assert
        draft.SelectedFeatIds.Should().BeEquivalentTo(feats);
    }

    [Fact]
    public void SetFeats_WithAnIncreasePerFeat_StoresThemInOrderUnderCanonicalIds()
    {
        // Arrange
        var draft = CreateTestDraft();

        // Act
        draft.SetFeats(["athlete", "alert"], [new() { ["dexterity"] = 1 }, []]);

        // Assert
        draft.FeatAbilityIncreases.Should().BeEquivalentTo([new Dictionary<string, int> { ["DEX"] = 1 }, []], options => options.WithStrictOrdering());
    }

    [Fact]
    public void SetFeats_WithoutIncreases_StoresAnEmptyOnePerFeat()
    {
        // Arrange
        var draft = CreateTestDraft();

        // Act
        draft.SetFeats(["alert", "lucky"]);

        // Assert
        draft.FeatAbilityIncreases.Should().BeEquivalentTo(new List<Dictionary<string, int>> { new(), new() });
    }

    [Fact]
    public void SetFeats_WithFewerIncreasesThanFeats_Throws()
    {
        // Arrange - which feat the one increase belongs to cannot be told
        var draft = CreateTestDraft();

        // Act
        var act = () => draft.SetFeats(["athlete", "alert"], [new() { ["DEX"] = 1 }]);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("2 feat(s) but 1 ability increase(s)*");
    }

    [Fact]
    public void FeatAbilityIncreaseAt_PastTheIncreasesSaved_IsEmpty()
    {
        // Arrange - drafts saved before the increases were stored have fewer of them than feats
        var draft = CreateTestDraft();
        draft.SetFeats(["alert"]);

        // Act
        var increase = draft.FeatAbilityIncreaseAt(1);

        // Assert
        increase.Should().BeEmpty();
    }

    [Fact]
    public void SetRace_WithAbilityIncreasesInAnySpelling_StoresThemUnderCanonicalIds()
    {
        // Arrange
        var draft = CreateTestDraft();

        // Act
        draft.SetRace("half_elf", abilityIncreases: new Dictionary<string, int> { ["dexterity"] = 1, ["wis"] = 1 });

        // Assert
        draft.RaceAbilityIncreases.Should().BeEquivalentTo(new Dictionary<string, int> { ["DEX"] = 1, ["WIS"] = 1 });
    }

    [Fact]
    public void SetSubrace_WhenTheSubraceChanges_ClearsTheRacesIncreases()
    {
        // Arrange - the variant human's picks mean nothing to a standard human
        var draft = CreateTestDraft();
        draft.SetRace("human", "variant_human", new Dictionary<string, int> { ["STR"] = 1, ["CON"] = 1 });

        // Act
        draft.SetSubrace("standard_human");

        // Assert
        draft.RaceAbilityIncreases.Should().BeEmpty();
    }

    [Fact]
    public void SetSubrace_WithTheSameSubrace_KeepsTheRacesIncreases()
    {
        // Arrange
        var draft = CreateTestDraft();
        draft.SetRace("human", "variant_human", new Dictionary<string, int> { ["STR"] = 1, ["CON"] = 1 });

        // Act
        draft.SetSubrace("variant_human");

        // Assert
        draft.RaceAbilityIncreases.Should().BeEquivalentTo(new Dictionary<string, int> { ["STR"] = 1, ["CON"] = 1 });
    }

    [Fact]
    public void ChooseEdition_WhenTheEditionChanges_ClearsTheRaceAndFeatIncreases()
    {
        // Arrange
        var draft = CreateCompleteTestDraft();
        draft.SetRace("half_elf", abilityIncreases: new Dictionary<string, int> { ["DEX"] = 1, ["WIS"] = 1 });
        draft.SetFeats(["athlete"], [new() { ["DEX"] = 1 }]);

        // Act
        draft.ChooseEdition(RulesEdition.Edition2014);

        // Assert
        draft.RaceAbilityIncreases.Should().BeEmpty();
        draft.FeatAbilityIncreases.Should().BeEmpty();
    }

    [Fact]
    public void SetSpells_UpdatesSpells()
    {
        // Arrange
        var draft = CreateTestDraft();
        var spells = new List<string> { "fireball", "magic-missile", "shield" };

        // Act
        draft.SetSpells(spells);

        // Assert
        draft.SelectedSpellIds.Should().BeEquivalentTo(spells);
    }

    [Fact]
    public void MarkAsCompleted_SetsIsCompletedAndTheCharacterItMade()
    {
        // Arrange
        var draft = CreateTestDraft();
        var characterId = Guid.NewGuid();
        draft.IsCompleted.Should().BeFalse();

        // Act
        draft.MarkAsCompleted(characterId);

        // Assert
        draft.IsCompleted.Should().BeTrue();
        draft.CharacterId.Should().Be(characterId);
    }

    [Fact]
    public void IsReadyToFinalize_WithCompleteData_ReturnsTrue()
    {
        // Arrange
        var draft = CreateTestDraft();
        draft.SetNameAndLevel("Thorin", 5);
        draft.SetRace("dwarf");
        draft.SetClass("fighter");
        draft.SetBackground("soldier");
        draft.SetBaseAbilityScores(new Dictionary<string, int>
        {
            ["strength"] = 15,
            ["dexterity"] = 14,
            ["constitution"] = 13,
            ["intelligence"] = 12,
            ["wisdom"] = 10,
            ["charisma"] = 8
        }, AbilityScoreMethod.Rolled);

        // Act
        var isReady = draft.IsReadyToFinalize();

        // Assert
        isReady.Should().BeTrue();
    }

    [Fact]
    public void IsReadyToFinalize_WithSixScoresNamingOnlyFiveAbilities_ReturnsFalse()
    {
        // Arrange - "STR" and "strength" are the same ability, so WIS never got a score
        var draft = CreateCompleteTestDraft();
        draft.SetBaseAbilityScores(new Dictionary<string, int>
        {
            ["STR"] = 15,
            ["strength"] = 15,
            ["DEX"] = 14,
            ["CON"] = 13,
            ["INT"] = 12,
            ["CHA"] = 8
        }, AbilityScoreMethod.Rolled);

        // Act & Assert
        draft.IsReadyToFinalize().Should().BeFalse();
    }

    [Fact]
    public void IsReadyToFinalize_WithUnknownAbilityKey_ReturnsFalse()
    {
        // Arrange
        var draft = CreateCompleteTestDraft();
        draft.SetBaseAbilityScores(new Dictionary<string, int>
        {
            ["luck"] = 15,
            ["DEX"] = 14,
            ["CON"] = 13,
            ["INT"] = 12,
            ["WIS"] = 10,
            ["CHA"] = 8
        }, AbilityScoreMethod.Rolled);

        // Act & Assert
        draft.IsReadyToFinalize().Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    [InlineData(-1)]
    public void IsReadyToFinalize_WithLevelOutOfRange_ReturnsFalse(int level)
    {
        // Arrange
        var draft = CreateCompleteTestDraft();
        draft.SetNameAndLevel("Thorin", level);

        // Act & Assert
        draft.IsReadyToFinalize().Should().BeFalse();
    }

    [Fact]
    public void SetRace_WhenRaceChanges_ClearsRaceDependentChoices()
    {
        // Arrange
        var draft = CreateCompleteTestDraft();
        draft.SetRace("half_elf");
        draft.SetRaceSkills(["perception", "stealth"]);
        draft.SetLanguages(["giant-phb"]);

        // Act
        draft.SetRace("dwarf");

        // Assert
        draft.SelectedRaceSkillIds.Should().BeEmpty();
        draft.SelectedLanguageIds.Should().BeEmpty();
    }

    [Fact]
    public void SetRace_WhenRaceIsUnchanged_KeepsRaceDependentChoices()
    {
        // Arrange
        var draft = CreateCompleteTestDraft();
        draft.SetRace("half_elf");
        draft.SetRaceSkills(["perception", "stealth"]);

        // Act - re-submitting the same race, e.g. stepping back and forward in the wizard
        draft.SetRace("half_elf", "moon_elf");

        // Assert
        draft.SelectedRaceSkillIds.Should().BeEquivalentTo(["perception", "stealth"]);
        draft.SubraceId.Should().Be("moon_elf");
    }

    [Fact]
    public void SetClass_WhenClassChanges_ClearsClassDependentChoices()
    {
        // Arrange
        var draft = CreateCompleteTestDraft();
        draft.SetClassProficiencies(["athletics"]);
        draft.SetSpells(["fireball"]);
        draft.SetAbilityScoreImprovements([new Dictionary<string, int> { ["STR"] = 2 }]);

        // Act
        draft.SetClass("wizard");

        // Assert
        draft.SelectedClassProficiencyIds.Should().BeEmpty();
        draft.SelectedSpellIds.Should().BeEmpty();
        draft.AbilityScoreImprovements.Should().BeEmpty();
    }

    [Fact]
    public void SetRace_WhenRaceChanges_ClearsTheImprovementsStep()
    {
        // Arrange - the variant human's bonus feat comes first among the feats; a dwarf has none
        var draft = CreateCompleteTestDraft();
        draft.SetRace("human", "variant_human");
        draft.SetAbilityScoreImprovements([new Dictionary<string, int> { ["STR"] = 2 }]);
        draft.SetFeats(["alert", "athlete"], [new(), new() { ["DEX"] = 1 }]);

        // Act
        draft.SetRace("dwarf", "hill_dwarf");

        // Assert
        draft.AbilityScoreImprovements.Should().BeEmpty();
        draft.SelectedFeatIds.Should().BeEmpty();
        draft.FeatAbilityIncreases.Should().BeEmpty();
    }

    [Fact]
    public void SetSubrace_WhenSubraceChanges_ClearsTheImprovementsStep()
    {
        // Arrange - the variant human's feat slot goes with the variant
        var draft = CreateCompleteTestDraft();
        draft.SetRace("human", "variant_human");
        draft.SetFeats(["alert"], [new()]);

        // Act
        draft.SetSubrace("standard_human");

        // Assert
        draft.SelectedFeatIds.Should().BeEmpty();
        draft.FeatAbilityIncreases.Should().BeEmpty();
    }

    [Fact]
    public void SetRace_WithTheSameRaceAndAnotherSubrace_ClearsTheImprovementsStep()
    {
        // Arrange - the race step sends the race and subrace together
        var draft = CreateCompleteTestDraft();
        draft.SetRace("human", "variant_human");
        draft.SetFeats(["alert"], [new()]);

        // Act
        draft.SetRace("human", "standard_human");

        // Assert
        draft.SelectedFeatIds.Should().BeEmpty();
        draft.FeatAbilityIncreases.Should().BeEmpty();
    }

    [Fact]
    public void SetRace_WithTheSameRaceAndSubrace_KeepsTheImprovementsStep()
    {
        // Arrange - stepping back through the wizard resends the race step unchanged
        var draft = CreateCompleteTestDraft();
        draft.SetRace("human", "variant_human");
        draft.SetAbilityScoreImprovements([new Dictionary<string, int> { ["STR"] = 2 }]);
        draft.SetFeats(["alert"], [new()]);

        // Act
        draft.SetRace("human", "variant_human", new Dictionary<string, int> { ["STR"] = 1, ["CON"] = 1 });

        // Assert
        draft.AbilityScoreImprovements.Should().ContainSingle();
        draft.SelectedFeatIds.Should().Equal("alert");
    }

    [Fact]
    public void SetClass_WhenClassChanges_ClearsTheFeatsAndWhatTheyRaise()
    {
        // Arrange - a fighter's Fighting Style feat, taken where a wizard has no improvement
        var draft = CreateCompleteTestDraft();
        draft.SetFeats(["defense_2024"], [new()]);

        // Act
        draft.SetClass("wizard");

        // Assert
        draft.SelectedFeatIds.Should().BeEmpty();
        draft.FeatAbilityIncreases.Should().BeEmpty();
    }

    [Fact]
    public void ChooseFightingStyles_StoresThem()
    {
        // Arrange
        var draft = CreateCompleteTestDraft();

        // Act
        draft.ChooseFightingStyles(["defense_2014"]);

        // Assert
        draft.FightingStyleIds.Should().Equal("defense_2014");
    }

    [Fact]
    public void SetClass_WhenClassChanges_ClearsTheFightingStyles()
    {
        // Arrange - a wizard has no Fighting Style feature
        var draft = CreateCompleteTestDraft();
        draft.ChooseFightingStyles(["defense_2014"]);

        // Act
        draft.SetClass("wizard");

        // Assert
        draft.FightingStyleIds.Should().BeEmpty();
    }

    [Fact]
    public void SetSubclass_WhenSubclassChanges_ClearsTheFightingStyles()
    {
        // Arrange - a level 10 Champion's second style goes with the Champion
        var draft = CreateCompleteTestDraft();
        draft.SetClass("fighter", "champion");
        draft.ChooseFightingStyles(["defense_2014", "archery_2014"]);

        // Act
        draft.SetSubclass("battle-master");

        // Assert
        draft.FightingStyleIds.Should().BeEmpty();
    }

    [Fact]
    public void SetNameAndLevel_WhenLevelChanges_ClearsTheFightingStyles()
    {
        // Arrange - how many styles are owed goes with the level: a paladin has none at level 1
        var draft = CreateCompleteTestDraft();
        draft.ChooseFightingStyles(["defense_2014"]);

        // Act
        draft.SetNameAndLevel("Thorin", 1);

        // Assert
        draft.FightingStyleIds.Should().BeEmpty();
    }

    [Fact]
    public void SetNameAndLevel_WhenOnlyNameChanges_KeepsTheFightingStyles()
    {
        // Arrange
        var draft = CreateCompleteTestDraft();
        draft.ChooseFightingStyles(["defense_2014"]);

        // Act
        draft.SetNameAndLevel("Balin", 5);

        // Assert
        draft.FightingStyleIds.Should().Equal("defense_2014");
    }

    [Fact]
    public void SetSubclass_WhenSubclassChanges_ClearsTheSpells()
    {
        // Arrange - an Eldritch Knight's wizard spells mean nothing to a Champion
        var draft = CreateCompleteTestDraft();
        draft.SetClass("fighter", "eldritch-knight");
        draft.SetSpells(["fire_bolt", "shield"]);

        // Act
        draft.SetSubclass("champion");

        // Assert
        draft.SelectedSpellIds.Should().BeEmpty();
    }

    [Fact]
    public void SetClass_WithTheSameClassAndAnotherSubclass_ClearsTheSpells()
    {
        // Arrange - the class step sends the class and subclass together
        var draft = CreateCompleteTestDraft();
        draft.SetClass("fighter", "eldritch-knight");
        draft.SetSpells(["fire_bolt", "shield"]);

        // Act
        draft.SetClass("fighter", "champion");

        // Assert
        draft.SelectedSpellIds.Should().BeEmpty();
    }

    [Fact]
    public void SetClass_WithTheSameClassAndSubclass_KeepsTheSpells()
    {
        // Arrange - stepping back through the wizard resends the class step unchanged
        var draft = CreateCompleteTestDraft();
        draft.SetClass("fighter", "eldritch-knight");
        draft.SetSpells(["fire_bolt", "shield"]);

        // Act
        draft.SetClass("fighter", "eldritch-knight");

        // Assert
        draft.SelectedSpellIds.Should().Equal("fire_bolt", "shield");
    }

    [Fact]
    public void SetBackground_WhenBackgroundChanges_ClearsLanguageChoices()
    {
        // Arrange
        var draft = CreateCompleteTestDraft();
        draft.SetLanguages(["giant-phb"]);

        // Act
        draft.SetBackground("acolyte");

        // Assert
        draft.SelectedLanguageIds.Should().BeEmpty();
    }

    [Fact]
    public void SetBackground_WithAbilityIncreasesInAnySpelling_StoresThemUnderCanonicalIds()
    {
        // Arrange
        var draft = CreateTestDraft();

        // Act - the 2024 soldier's +2 and +1
        draft.SetBackground("soldier_2024", new Dictionary<string, int> { ["strength"] = 2, ["con"] = 1 });

        // Assert
        draft.BackgroundAbilityIncreases.Should().BeEquivalentTo(new Dictionary<string, int> { ["STR"] = 2, ["CON"] = 1 });
    }

    [Fact]
    public void SetBackground_WithoutAbilityIncreases_ClearsTheEarlierOnes()
    {
        // Arrange - a 2024 background's increases do not carry over to another background
        var draft = CreateTestDraft();
        draft.SetBackground("soldier_2024", new Dictionary<string, int> { ["STR"] = 2, ["CON"] = 1 });

        // Act
        draft.SetBackground("sage_2024");

        // Assert
        draft.BackgroundAbilityIncreases.Should().BeEmpty();
    }

    [Fact]
    public void SetNameAndLevel_WhenLevelChanges_ClearsLevelDependentChoices()
    {
        // Arrange
        var draft = CreateCompleteTestDraft();
        draft.SetSpells(["fireball"]);
        draft.SetAbilityScoreImprovements([new Dictionary<string, int> { ["STR"] = 2 }]);

        // Act
        draft.SetNameAndLevel("Thorin", 2);

        // Assert
        draft.SelectedSpellIds.Should().BeEmpty();
        draft.AbilityScoreImprovements.Should().BeEmpty();
    }

    [Fact]
    public void SetNameAndLevel_WhenLevelChanges_ClearsTheFeatsAndWhatTheyRaise()
    {
        // Arrange - an Epic Boon picked at level 19 must not survive a drop to level 4
        var draft = CreateCompleteTestDraft();
        draft.SetFeats(["boon_of_fortitude"], [new() { ["CON"] = 1 }]);

        // Act
        draft.SetNameAndLevel("Thorin", 4);

        // Assert
        draft.SelectedFeatIds.Should().BeEmpty();
        draft.FeatAbilityIncreases.Should().BeEmpty();
    }

    [Fact]
    public void SetNameAndLevel_WhenOnlyNameChanges_KeepsLevelDependentChoices()
    {
        // Arrange
        var draft = CreateCompleteTestDraft();
        draft.SetAbilityScoreImprovements([new Dictionary<string, int> { ["STR"] = 2 }]);

        // Act
        draft.SetNameAndLevel("Balin", 5);

        // Assert
        draft.AbilityScoreImprovements.Should().HaveCount(1);
        draft.Name.Should().Be("Balin");
    }

    [Fact]
    public void CreateEmpty_UsesThe2024Rules()
    {
        // Act - the player can switch in the first step
        var draft = CreateTestDraft();

        // Assert
        draft.Edition.Should().Be(RulesEdition.Edition2024);
    }

    [Fact]
    public void ChooseEdition_WhenTheEditionChanges_ClearsEveryChoiceThatNamedAnId()
    {
        // Arrange - ids belong to one edition, so none of these could carry over
        var draft = CreateCompleteTestDraft();
        draft.SetSubrace("hill_dwarf");
        draft.SetSubclass("champion");
        draft.SetClassProficiencies(["athletics"]);
        draft.SetRaceSkills(["perception"]);
        draft.SetLanguages(["giant-phb"]);
        draft.SetFeats(["alert_2014"]);
        draft.ChooseFightingStyles(["defense_2024"]);
        draft.SetSpells(["fireball"]);
        draft.SetAbilityScoreImprovements([new Dictionary<string, int> { ["STR"] = 2 }]);
        draft.SetBackground("soldier_2024", new Dictionary<string, int> { ["STR"] = 2, ["CON"] = 1 });

        // Act
        draft.ChooseEdition(RulesEdition.Edition2014);

        // Assert
        draft.Edition.Should().Be(RulesEdition.Edition2014);
        new[] { draft.RaceId, draft.SubraceId, draft.ClassId, draft.SubclassId, draft.BackgroundId }.Should().AllBeEquivalentTo((string?)null);
        draft.BackgroundAbilityIncreases.Should().BeEmpty();
        draft.SelectedClassProficiencyIds.Should().BeEmpty();
        draft.SelectedRaceSkillIds.Should().BeEmpty();
        draft.SelectedLanguageIds.Should().BeEmpty();
        draft.SelectedFeatIds.Should().BeEmpty();
        draft.FightingStyleIds.Should().BeEmpty();
        draft.SelectedSpellIds.Should().BeEmpty();
        draft.AbilityScoreImprovements.Should().BeEmpty();
    }

    [Fact]
    public void ChooseEdition_WhenTheEditionChanges_KeepsTheNameLevelAndAbilityScores()
    {
        // Arrange
        var draft = CreateCompleteTestDraft();

        // Act
        draft.ChooseEdition(RulesEdition.Edition2014);

        // Assert
        draft.Name.Should().Be("Thorin");
        draft.Level.Should().Be(5);
        draft.BaseAbilityScores.Should().HaveCount(6);
    }

    [Fact]
    public void ChooseEdition_WithTheSameEdition_KeepsEveryChoice()
    {
        // Arrange - step 1 saves the edition every time, not only when it changes
        var draft = CreateCompleteTestDraft();
        draft.SetSpells(["fireball"]);

        // Act
        draft.ChooseEdition(draft.Edition);

        // Assert
        draft.ClassId.Should().Be("fighter");
        draft.SelectedSpellIds.Should().Equal("fireball");
    }

    [Fact]
    public void ChooseEdition_WithAnUnknownEdition_Throws()
    {
        // Arrange
        var draft = CreateTestDraft();

        // Act
        var act = () => draft.ChooseEdition("2019");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Unknown rules edition: 2019*");
    }

    private static CharacterDraft CreateCompleteTestDraft()
    {
        var draft = CreateTestDraft();
        draft.SetNameAndLevel("Thorin", 5);
        draft.SetRace("dwarf");
        draft.SetClass("fighter");
        draft.SetBackground("soldier");
        draft.SetBaseAbilityScores(new Dictionary<string, int>
        {
            ["STR"] = 15,
            ["DEX"] = 14,
            ["CON"] = 13,
            ["INT"] = 12,
            ["WIS"] = 10,
            ["CHA"] = 8
        }, AbilityScoreMethod.Rolled);

        return draft;
    }

    [Fact]
    public void IsReadyToFinalize_WithoutName_ReturnsFalse()
    {
        // Arrange
        var draft = CreateCompleteDraft();
        draft.SetNameAndLevel(null!, 5);

        // Act
        var isReady = draft.IsReadyToFinalize();

        // Assert
        isReady.Should().BeFalse();
    }

    [Fact]
    public void IsReadyToFinalize_WithEmptyName_ReturnsFalse()
    {
        // Arrange
        var draft = CreateCompleteDraft();
        draft.SetNameAndLevel("   ", 5);

        // Act
        var isReady = draft.IsReadyToFinalize();

        // Assert
        isReady.Should().BeFalse();
    }

    [Fact]
    public void IsReadyToFinalize_WithoutNameAndLevel_ReturnsFalse()
    {
        // Arrange - one step sets both, so a draft cannot have one without the other
        var draft = CreateCompleteDraftWithout("name and level");

        // Act
        var isReady = draft.IsReadyToFinalize();

        // Assert
        isReady.Should().BeFalse();
    }

    [Fact]
    public void IsReadyToFinalize_WithoutRace_ReturnsFalse()
    {
        // Arrange
        var draft = CreateCompleteDraftWithout("race");

        // Act
        var isReady = draft.IsReadyToFinalize();

        // Assert
        isReady.Should().BeFalse();
    }

    [Fact]
    public void IsReadyToFinalize_WithoutClass_ReturnsFalse()
    {
        // Arrange
        var draft = CreateCompleteDraftWithout("class");

        // Act
        var isReady = draft.IsReadyToFinalize();

        // Assert
        isReady.Should().BeFalse();
    }

    [Fact]
    public void IsReadyToFinalize_WithoutBackground_ReturnsFalse()
    {
        // Arrange
        var draft = CreateCompleteDraftWithout("background");

        // Act
        var isReady = draft.IsReadyToFinalize();

        // Assert
        isReady.Should().BeFalse();
    }

    [Fact]
    public void IsReadyToFinalize_WithFewerThanSixAbilityScores_ReturnsFalse()
    {
        // Arrange
        var draft = CreateCompleteDraft();
        draft.SetBaseAbilityScores(new Dictionary<string, int>
        {
            ["strength"] = 15,
            ["dexterity"] = 14,
            ["constitution"] = 13
            // Missing 3 scores
        }, AbilityScoreMethod.Rolled);

        // Act
        var isReady = draft.IsReadyToFinalize();

        // Assert
        isReady.Should().BeFalse();
    }

    [Fact]
    public void IsReadyToFinalize_WithMoreThanSixAbilityScores_ReturnsFalse()
    {
        // Arrange
        var draft = CreateCompleteDraft();
        draft.SetBaseAbilityScores(new Dictionary<string, int>
        {
            ["strength"] = 15,
            ["dexterity"] = 14,
            ["constitution"] = 13,
            ["intelligence"] = 12,
            ["wisdom"] = 10,
            ["charisma"] = 8,
            ["luck"] = 10 // Extra score
        }, AbilityScoreMethod.Rolled);

        // Act
        var isReady = draft.IsReadyToFinalize();

        // Assert
        isReady.Should().BeFalse();
    }

    [Fact]
    public void IsReadyToFinalize_DoesNotRequireSubrace()
    {
        // Arrange
        var draft = CreateCompleteDraft();

        // Act
        var isReady = draft.IsReadyToFinalize();

        // Assert
        isReady.Should().BeTrue();
    }

    [Fact]
    public void IsReadyToFinalize_DoesNotRequireSubclass()
    {
        // Arrange
        var draft = CreateCompleteDraft();

        // Act
        var isReady = draft.IsReadyToFinalize();

        // Assert
        isReady.Should().BeTrue();
    }

    [Fact]
    public void IsReadyToFinalize_DoesNotRequireProficiencies()
    {
        // Arrange
        var draft = CreateCompleteDraft();

        // Act
        var isReady = draft.IsReadyToFinalize();

        // Assert
        isReady.Should().BeTrue();
    }

    [Fact]
    public void IsReadyToFinalize_DoesNotRequireFeats()
    {
        // Arrange
        var draft = CreateCompleteDraft();

        // Act
        var isReady = draft.IsReadyToFinalize();

        // Assert
        isReady.Should().BeTrue();
    }

    [Fact]
    public void IsReadyToFinalize_DoesNotRequireSpells()
    {
        // Arrange
        var draft = CreateCompleteDraft();

        // Act
        var isReady = draft.IsReadyToFinalize();

        // Assert
        isReady.Should().BeTrue();
    }

    // Helper methods
    private static CharacterDraft CreateTestDraft()
    {
        return CharacterDraft.CreateEmpty(Guid.NewGuid(), FakeTimeProvider);
    }

    private static CharacterDraft CreateCompleteDraft() => CreateCompleteDraftWithout(step: null);

    // Every required step but the one named, which is the only way a draft lacks one: its
    // choices change through its methods alone
    private static CharacterDraft CreateCompleteDraftWithout(string? step)
    {
        var draft = CreateTestDraft();

        if (step != "name and level")
        {
            draft.SetNameAndLevel("Test Character", 5);
        }

        if (step != "race")
        {
            draft.SetRace("human");
        }

        if (step != "class")
        {
            draft.SetClass("fighter");
        }

        if (step != "background")
        {
            draft.SetBackground("soldier");
        }

        draft.SetBaseAbilityScores(new Dictionary<string, int>
        {
            ["strength"] = 15,
            ["dexterity"] = 14,
            ["constitution"] = 13,
            ["intelligence"] = 12,
            ["wisdom"] = 10,
            ["charisma"] = 8
        }, AbilityScoreMethod.Rolled);
        return draft;
    }
}