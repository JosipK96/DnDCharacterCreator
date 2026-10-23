using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

namespace CharacterCreatorApp.Domain.UnitTests;

public class CharacterTests
{
    private static readonly FakeTimeProvider FakeTimeProvider = new(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero));

    [Fact]
    public void Create_WithValidData_ReturnsCharacter()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var name = "Thorin Oakenshield";
        var level = 5;
        var raceId = "dwarf";
        var classId = "fighter";
        var backgroundId = "soldier";
        var abilityScores = CreateValidAbilityScores();

        // Act
        var character = Character.Create(
            userId,
            name,
            level,
            RulesEdition.Edition2024,
            raceId,
            null,
            classId,
            null,
            backgroundId,
            abilityScores,
            FakeTimeProvider
        );

        // Assert
        character.Should().NotBeNull();
        character.Id.Should().NotBeEmpty();
        character.UserId.Should().Be(userId);
        character.Name.Should().Be(name);
        character.Level.Should().Be(level);
        character.Edition.Should().Be(RulesEdition.Edition2024);
        character.RaceId.Should().Be(raceId);
        character.ClassId.Should().Be(classId);
        character.BackgroundId.Should().Be(backgroundId);
        character.SubraceId.Should().BeNull();
        character.SubclassId.Should().BeNull();
        character.AbilityScores.Should().HaveCount(6);
        character.CreatedAt.Should().Be(FakeTimeProvider.GetUtcNow());
    }

    [Fact]
    public void Create_GivesAVersion7IdStartingWithTheCreationTime()
    {
        // Act
        var character = Character.Create(
            Guid.NewGuid(), "Thorin", 5, RulesEdition.Edition2014, "dwarf", null, "fighter", null, "soldier",
            CreateValidAbilityScores(), FakeTimeProvider);

        // Assert
        character.Id.Version.Should().Be(7);
        UuidV7.TimestampOf(character.Id).Should().Be(FakeTimeProvider.GetUtcNow());
    }

    [Fact]
    public void Create_WithAnUnknownEdition_Throws()
    {
        // Act
        Action act = () => Character.Create(
            Guid.NewGuid(), "Thorin", 5, "2019", "dwarf", null, "fighter", null, "soldier",
            CreateValidAbilityScores(), FakeTimeProvider);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Unknown rules edition: 2019*");
    }

    [Fact]
    public void Create_WithSubraceAndSubclass_SetsOptionalProperties()
    {
        // Arrange
        var abilityScores = CreateValidAbilityScores();

        // Act
        var character = Character.Create(
            Guid.NewGuid(),
            "Gandalf",
            10,
            RulesEdition.Edition2014,
            "elf",
            "high-elf",
            "wizard",
            "evocation",
            "sage",
            abilityScores,
            FakeTimeProvider
        );

        // Assert
        character.SubraceId.Should().Be("high-elf");
        character.SubclassId.Should().Be("evocation");
    }

    [Fact]
    public void Create_SetsEmptyCollections()
    {
        // Arrange
        var abilityScores = CreateValidAbilityScores();

        // Act
        var character = Character.Create(
            Guid.NewGuid(),
            "Test",
            1,
            RulesEdition.Edition2014,
            "human",
            null,
            "fighter",
            null,
            "soldier",
            abilityScores,
            FakeTimeProvider
        );

        // Assert
        character.ProficiencyIds.Should().BeEmpty();
        character.LanguageIds.Should().BeEmpty();
        character.SpellIds.Should().BeEmpty();
    }

    [Fact]
    public void ApplyRaceAbilityScoreBonuses_AddsCorrectBonus()
    {
        // Arrange
        var character = CreateTestCharacter();
        var bonuses = new List<RaceAbilityBonus>
        {
            new() { RaceId = "dwarf", AbilityScoreId = "constitution", Bonus = 2 }
        };

        var originalConstitution = character.AbilityScores["CON"];

        // Act
        character.ApplyRaceAbilityScoreBonuses(bonuses);

        // Assert
        character.AbilityScores["CON"].Should().Be(originalConstitution + 2);
    }

    [Fact]
    public void ApplyRaceAbilityScoreBonuses_CapsAt20()
    {
        // Arrange
        var character = CreateTestCharacter();
        character.AbilityScores["STR"] = 19;

        var bonuses = new List<RaceAbilityBonus>
        {
            new() { RaceId = "half-orc", AbilityScoreId = "strength", Bonus = 2 }
        };

        // Act
        character.ApplyRaceAbilityScoreBonuses(bonuses);

        // Assert - Should cap at 20, not go to 21
        character.AbilityScores["STR"].Should().Be(20);
    }

    [Fact]
    public void ApplyRaceAbilityScoreBonuses_HandlesMultipleBonuses()
    {
        // Arrange
        var character = CreateTestCharacter();
        var bonuses = new List<RaceAbilityBonus>
        {
            new() { RaceId = "dwarf", AbilityScoreId = "constitution", Bonus = 2 },
            new() { RaceId = "dwarf", AbilityScoreId = "wisdom", Bonus = 1 }
        };

        var originalConstitution = character.AbilityScores["CON"];
        var originalWisdom = character.AbilityScores["WIS"];

        // Act
        character.ApplyRaceAbilityScoreBonuses(bonuses);

        // Assert
        character.AbilityScores["CON"].Should().Be(originalConstitution + 2);
        character.AbilityScores["WIS"].Should().Be(originalWisdom + 1);
    }

    [Fact]
    public void ApplyRaceAbilityScoreBonuses_CreatesAbilityScoreIfNotExists()
    {
        // Arrange
        var character = CreateTestCharacter();
        character.AbilityScores.Remove("CHA"); // Remove one

        var bonuses = new List<RaceAbilityBonus>
        {
            new() { RaceId = "human", AbilityScoreId = "charisma", Bonus = 1 }
        };

        // Act
        character.ApplyRaceAbilityScoreBonuses(bonuses);

        // Assert
        character.AbilityScores.Should().ContainKey("CHA");
        character.AbilityScores["CHA"].Should().Be(1);
    }

    [Fact]
    public void ApplySubraceAbilityScoreBonuses_AddsCorrectBonus()
    {
        // Arrange
        var character = CreateTestCharacter();
        var bonuses = new List<SubraceAbilityBonus>
        {
            new() { SubraceId = "high-elf", AbilityScoreId = "intelligence", Bonus = 1 }
        };

        var originalIntelligence = character.AbilityScores["INT"];

        // Act
        character.ApplySubraceAbilityScoreBonuses(bonuses);

        // Assert
        character.AbilityScores["INT"].Should().Be(originalIntelligence + 1);
    }

    [Fact]
    public void ApplySubraceAbilityScoreBonuses_CapsAt20()
    {
        // Arrange
        var character = CreateTestCharacter();
        character.AbilityScores["DEX"] = 20;

        var bonuses = new List<SubraceAbilityBonus>
        {
            new() { SubraceId = "wood-elf", AbilityScoreId = "dexterity", Bonus = 2 }
        };

        // Act
        character.ApplySubraceAbilityScoreBonuses(bonuses);

        // Assert - Should stay at 20
        character.AbilityScores["DEX"].Should().Be(20);
    }

    [Fact]
    public void SetLanguages_RemovesDuplicates()
    {
        // Arrange
        var character = CreateTestCharacter();
        var languages = new[] { "common", "dwarvish", "common", "elvish", "dwarvish" };

        // Act
        character.SetLanguages(languages);

        // Assert
        character.LanguageIds.Should().HaveCount(3);
        character.LanguageIds.Should().Contain("common");
        character.LanguageIds.Should().Contain("dwarvish");
        character.LanguageIds.Should().Contain("elvish");
    }

    [Fact]
    public void SetProficiencies_RemovesDuplicates()
    {
        // Arrange
        var character = CreateTestCharacter();
        var proficiencies = new[] { "martial-weapons", "light-armor", "martial-weapons", "shields" };

        // Act
        character.SetProficiencies(proficiencies);

        // Assert
        character.ProficiencyIds.Should().HaveCount(3);
        character.ProficiencyIds.Should().Contain("martial-weapons");
        character.ProficiencyIds.Should().Contain("light-armor");
        character.ProficiencyIds.Should().Contain("shields");
    }

    [Fact]
    public void SetFeatSavingThrows_KeepsEachAbilityOnceUnderItsCanonicalId()
    {
        // Arrange
        var character = CreateTestCharacter();

        // Act
        character.SetFeatSavingThrows(["con", "CON", "wisdom"]);

        // Assert
        character.FeatSavingThrowAbilityIds.Should().Equal("CON", "WIS");
    }

    [Fact]
    public void SetSize_KeepsIt()
    {
        // Arrange
        var character = CreateTestCharacter();

        // Act
        character.SetSize("Small");

        // Assert
        character.Size.Should().Be("Small");
    }

    [Fact]
    public void SetSpells_RemovesDuplicates()
    {
        // Arrange
        var character = CreateTestCharacter();
        var spells = new[] { "fireball", "magic-missile", "fireball", "shield" };

        // Act
        character.SetSpells(spells);

        // Assert
        character.SpellIds.Should().HaveCount(3);
        character.SpellIds.Should().Contain("fireball");
        character.SpellIds.Should().Contain("magic-missile");
        character.SpellIds.Should().Contain("shield");
    }

    [Theory]
    [InlineData("STR")]
    [InlineData("str")]
    [InlineData("Str")]
    [InlineData("Strength")]
    [InlineData("strength")]
    public void Create_NormalizesAbilityKeySpellings(string key)
    {
        // Arrange
        var scores = new Dictionary<string, int>
        {
            [key] = 15,
            ["DEX"] = 14,
            ["CON"] = 13,
            ["INT"] = 12,
            ["WIS"] = 10,
            ["CHA"] = 8
        };

        // Act
        var character = Character.Create(
            Guid.NewGuid(), "Test", 1, RulesEdition.Edition2014, "human", null, "fighter", null, "soldier", scores, FakeTimeProvider);

        // Assert
        character.AbilityScores.Should().HaveCount(6);
        character.AbilityScores["STR"].Should().Be(15);
    }

    [Fact]
    public void ApplyRaceAbilityScoreBonuses_WithSeedStyleAbbreviations_UpdatesExistingScore()
    {
        // Arrange - seed data stores bonuses as "con", drafts store scores as "CON"
        var character = CreateTestCharacter();
        var originalConstitution = character.AbilityScores["CON"];

        var bonuses = new List<RaceAbilityBonus>
        {
            new() { RaceId = "dwarf", AbilityScoreId = "con", Bonus = 2 }
        };

        // Act
        character.ApplyRaceAbilityScoreBonuses(bonuses);

        // Assert - must add to the existing entry, not create a second "con" key
        character.AbilityScores.Should().HaveCount(6);
        character.AbilityScores["CON"].Should().Be(originalConstitution + 2);
    }

    [Fact]
    public void ApplyAbilityScoreImprovements_AddsPointsToMatchingScores()
    {
        // Arrange
        var character = CreateTestCharacter();
        var originalStrength = character.AbilityScores["STR"];
        var originalDexterity = character.AbilityScores["DEX"];

        List<Dictionary<string, int>> improvements =
        [
            new() { ["strength"] = 2 },
            new() { ["strength"] = 1, ["dexterity"] = 1 }
        ];

        // Act
        character.ApplyAbilityScoreImprovements(improvements);

        // Assert
        character.AbilityScores.Should().HaveCount(6);
        character.AbilityScores["STR"].Should().Be(originalStrength + 3);
        character.AbilityScores["DEX"].Should().Be(originalDexterity + 1);
    }

    [Fact]
    public void ApplyAbilityScoreImprovements_CapsAt20()
    {
        // Arrange
        var character = CreateTestCharacter();
        character.AbilityScores["STR"] = 19;

        List<Dictionary<string, int>> improvements = [new() { ["strength"] = 2 }];

        // Act
        character.ApplyAbilityScoreImprovements(improvements);

        // Assert
        character.AbilityScores["STR"].Should().Be(20);
    }

    [Fact]
    public void ApplyBackgroundAbilityIncreases_AddsThemCappedAt20()
    {
        // Arrange - a rolled 19 with a 2024 background's +2 stops at 20
        var character = CreateTestCharacter();
        character.AbilityScores["STR"] = 19;
        var originalConstitution = character.AbilityScores["CON"];

        // Act
        character.ApplyBackgroundAbilityIncreases(new Dictionary<string, int> { ["STR"] = 2, ["CON"] = 1 });

        // Assert
        character.AbilityScores["STR"].Should().Be(20);
        character.AbilityScores["CON"].Should().Be(originalConstitution + 1);
    }

    [Fact]
    public void ApplyRaceAbilityIncreases_AddsThePickedOnes()
    {
        // Arrange - a half-elf's +1 to Dexterity and Wisdom
        var character = CreateTestCharacter();

        // Act
        character.ApplyRaceAbilityIncreases(new Dictionary<string, int> { ["DEX"] = 1, ["WIS"] = 1 });

        // Assert
        (character.AbilityScores["DEX"], character.AbilityScores["WIS"]).Should().Be((15, 11));
    }

    [Theory]
    [InlineData(20, 20, 20)] // a feat's +1 stops at 20
    [InlineData(20, 30, 21)] // an Epic Boon's goes on to 30
    [InlineData(30, 30, 30)]
    [InlineData(22, 20, 22)] // a score already past the cap stays where it is
    public void ApplyFeatAbilityIncrease_RaisesUpToTheFeatsMaximum(int strength, int maximum, int expected)
    {
        // Arrange
        var character = CreateTestCharacter();
        character.AbilityScores["STR"] = strength;

        // Act
        character.ApplyFeatAbilityIncrease(new Dictionary<string, int> { ["STR"] = 1 }, maximum);

        // Assert
        character.AbilityScores["STR"].Should().Be(expected);
    }

    [Fact]
    public void ApplyAbilityScoreImprovements_WithNoImprovements_LeavesScoresUnchanged()
    {
        // Arrange
        var character = CreateTestCharacter();
        var before = new Dictionary<string, int>(character.AbilityScores);

        // Act
        character.ApplyAbilityScoreImprovements([]);

        // Assert
        character.AbilityScores.Should().BeEquivalentTo(before);
    }

    [Fact]
    public void Create_WithUnknownAbilityKey_ThrowsException()
    {
        // Arrange
        var scores = new Dictionary<string, int>
        {
            ["luck"] = 15,
            ["DEX"] = 14,
            ["CON"] = 13,
            ["INT"] = 12,
            ["WIS"] = 10,
            ["CHA"] = 8
        };

        // Act
        Action act = () => Character.Create(
            Guid.NewGuid(), "Test", 1, RulesEdition.Edition2014, "human", null, "fighter", null, "soldier", scores, FakeTimeProvider);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Invalid ability score key: luck*");
    }

    [Fact]
    public void ReplaceWith_TakesEveryBuiltValueButTheIdOwnerAndCreation()
    {
        // Arrange - the edit is built a day later, as a new Character with an id of its own
        var userId = Guid.NewGuid();
        var saved = Character.Create(
            userId, "Old Name", 1, RulesEdition.Edition2014, "human", null, "fighter", null, "soldier", CreateValidAbilityScores(), FakeTimeProvider);
        var later = new FakeTimeProvider(FakeTimeProvider.GetUtcNow().AddDays(1));
        var rebuilt = Character.Create(
            userId, "New Name", 2, RulesEdition.Edition2014, "elf", "elf_high", "wizard", null, "sage",
            new Dictionary<string, int> { ["INT"] = 17 }, later);
        rebuilt.SetProficiencies(["arcana"]);
        rebuilt.SetLanguages(["elvish-phb"]);
        rebuilt.SetSpells(["fire_bolt"]);
        rebuilt.SetFeats(["alert_2014"]);
        rebuilt.SetFeatSpells([new FeatSpellPick { FeatId = "magic_initiate_2014", ClassId = "wizard", SpellIds = ["light"] }]);
        rebuilt.SetSize("Medium");
        rebuilt.SetFeatSavingThrows(["CON"]);

        // Act
        saved.ReplaceWith(rebuilt);

        // Assert
        saved.Should().BeEquivalentTo(rebuilt, options => options
            .ComparingByMembers<Character>()
            .Excluding(c => c.Id)
            .Excluding(c => c.CreatedAt));
        saved.Id.Should().NotBe(rebuilt.Id);
        saved.CreatedAt.Should().Be(FakeTimeProvider.GetUtcNow());
        saved.UserId.Should().Be(userId);
    }

    [Fact]
    public void ReplaceWith_AnotherUsersCharacter_Throws()
    {
        // Arrange
        var saved = CreateTestCharacter();
        var someoneElses = CreateTestCharacter();

        // Act
        var act = () => saved.ReplaceWith(someoneElses);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    // Helper methods
    private static Character CreateTestCharacter()
    {
        var abilityScores = CreateValidAbilityScores();

        return Character.Create(
            Guid.NewGuid(),
            "Test Character",
            1,
            RulesEdition.Edition2014,
            "human",
            null,
            "fighter",
            null,
            "soldier",
            abilityScores,
            FakeTimeProvider
        );
    }

    private static Dictionary<string, int> CreateValidAbilityScores()
    {
        return new Dictionary<string, int>
        {
            ["strength"] = 15,
            ["dexterity"] = 14,
            ["constitution"] = 13,
            ["intelligence"] = 12,
            ["wisdom"] = 10,
            ["charisma"] = 8
        };
    }
}