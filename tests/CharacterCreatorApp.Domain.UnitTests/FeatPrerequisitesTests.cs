using CharacterCreatorApp.Domain.DomainModels.Feats;

using FluentAssertions;

namespace CharacterCreatorApp.Domain.UnitTests;

public class FeatPrerequisitesTests
{
    // STR 12, DEX 14: short of 13 in Strength, past it in Dexterity
    private static readonly Dictionary<string, int> Scores = new()
    {
        ["STR"] = 12,
        ["DEX"] = 14,
        ["CON"] = 13,
        ["INT"] = 10,
        ["WIS"] = 15,
        ["CHA"] = 8
    };

    private static readonly string[] NoFeatures = [];

    private static readonly string[] Medium = ["Medium"];

    [Fact]
    public void FindUnmet_WithNoRequirements_ReturnsNull()
    {
        // Act
        var unmet = new FeatPrerequisites().FindUnmet(1, "human", null, Medium, "fighter", Scores, NoFeatures);

        // Assert
        unmet.Should().BeNull();
    }

    [Theory]
    [InlineData(18, "Level 19")]
    [InlineData(19, null)]
    public void FindUnmet_WithAMinimumLevel_ChecksTheCharactersLevel(int level, string? expected)
    {
        // Arrange - an epic boon
        var prerequisites = new FeatPrerequisites { MinimumLevel = 19 };

        // Act
        var unmet = prerequisites.FindUnmet(level, "human", null, Medium, "fighter", Scores, NoFeatures);

        // Assert
        unmet.Should().Be(expected);
    }

    [Fact]
    public void FindUnmet_WithEitherOfTwoAbilities_IsMetByTheOneTheCharacterHas()
    {
        // Arrange - Athlete (2024): Strength or Dexterity 13
        var prerequisites = new FeatPrerequisites { AbilityMinimums = [Ability("STR", 13), Ability("DEX", 13)] };

        // Act
        var unmet = prerequisites.FindUnmet(4, "human", null, Medium, "fighter", Scores, NoFeatures);

        // Assert
        unmet.Should().BeNull();
    }

    [Fact]
    public void FindUnmet_WithNeitherAbilityHighEnough_NamesBothWithTheirMinimums()
    {
        // Arrange - STR 12 is short of 13 and DEX 14 of 15
        var prerequisites = new FeatPrerequisites { AbilityMinimums = [Ability("STR", 13), Ability("DEX", 15)] };

        // Act
        var unmet = prerequisites.FindUnmet(4, "human", null, Medium, "fighter", Scores, NoFeatures);

        // Assert
        unmet.Should().Be("Strength 13 or Dexterity 15");
    }

    [Fact]
    public void FindUnmet_WithNeitherAbilityHighEnoughForOneMinimum_NamesTheMinimumOnce()
    {
        // Arrange - both short of 15
        var prerequisites = new FeatPrerequisites { AbilityMinimums = [Ability("STR", 15), Ability("DEX", 15)] };

        // Act
        var unmet = prerequisites.FindUnmet(4, "human", null, Medium, "fighter", Scores, NoFeatures);

        // Assert
        unmet.Should().Be("Strength or Dexterity 15");
    }

    [Fact]
    public void FindUnmet_ForAnotherRace_NamesTheRacesThatQualify()
    {
        // Arrange - Elven Accuracy
        var prerequisites = new FeatPrerequisites { RaceIds = ["elf", "half_elf"] };

        // Act
        var unmet = prerequisites.FindUnmet(4, "dwarf", null, Medium, "fighter", Scores, NoFeatures);

        // Assert
        unmet.Should().Be("Elf or Half-Elf");
    }

    [Theory]
    [InlineData("wood_elf", null)]
    [InlineData("high_elf", "Wood Elf")]
    [InlineData(null, "Wood Elf")]
    public void FindUnmet_WithASubraceRequirement_ChecksTheSubraceNotJustTheRace(string? subraceId, string? expected)
    {
        // Arrange - Wood Elf Magic: a high elf is an elf, and still may not take it
        var prerequisites = new FeatPrerequisites { SubraceIds = ["wood_elf"] };

        // Act
        var unmet = prerequisites.FindUnmet(4, "elf", subraceId, Medium, "fighter", Scores, NoFeatures);

        // Assert
        unmet.Should().Be(expected);
    }

    [Theory]
    [InlineData("dwarf", new[] { "Medium" }, null)]
    [InlineData("halfling", new[] { "Small" }, null)]
    [InlineData("human_2024", new[] { "Small", "Medium" }, null)]
    [InlineData("elf", new[] { "Medium" }, "Dwarf or a Small race")]
    public void FindUnmet_WithARaceOrASize_IsMetByEither(string raceId, string[] raceSizes, string? expected)
    {
        // Arrange - Squat Nimbleness: a dwarf or a Small race; a race the player sizes may be Small
        var prerequisites = new FeatPrerequisites { RaceIds = ["dwarf"], Sizes = ["Small"] };

        // Act
        var unmet = prerequisites.FindUnmet(4, raceId, null, raceSizes, "fighter", Scores, NoFeatures);

        // Assert
        unmet.Should().Be(expected);
    }

    [Fact]
    public void FindUnmet_ForAnotherClass_NamesTheClassesThatQualify()
    {
        // Arrange - Squire of Solamnia
        var prerequisites = new FeatPrerequisites { ClassIds = ["fighter", "paladin"] };

        // Act
        var unmet = prerequisites.FindUnmet(4, "human", null, Medium, "wizard", Scores, NoFeatures);

        // Assert
        unmet.Should().Be("Fighter or Paladin");
    }

    [Theory]
    [InlineData("dwarf_2024", null, "Elf")]
    [InlineData(null, "wizard-2024", "Fighter or Paladin")]
    public void FindUnmet_For2024Ids_NamesTheAlternativesWithoutTheirEdition(string? raceId, string? classId, string expected)
    {
        // Arrange - a 2024 feat names 2024 ids, which the player reads without the edition
        var prerequisites = raceId != null
            ? new FeatPrerequisites { RaceIds = ["elf_2024"] }
            : new FeatPrerequisites { ClassIds = ["fighter-2024", "paladin-2024"] };

        // Act
        var unmet = prerequisites.FindUnmet(4, raceId, null, Medium, classId, Scores, NoFeatures);

        // Assert
        unmet.Should().Be(expected);
    }

    [Theory]
    [InlineData(new[] { "Spellcasting", "Fighting Style" }, null)]
    [InlineData(new[] { "Spellcasting", "Divine Sense" }, "The Fighting Style feature")]
    public void FindUnmet_WithAFeatureRequirement_ChecksTheCharactersFeatures(string[] featureNames, string? expected)
    {
        // Arrange - Archery (2024), a Fighting Style feat
        var prerequisites = new FeatPrerequisites { Features = ["Fighting Style"] };

        // Act
        var unmet = prerequisites.FindUnmet(1, "human", null, Medium, "fighter-2024", Scores, featureNames);

        // Assert
        unmet.Should().Be(expected);
    }

    [Fact]
    public void FindUnmet_WithAbilityKeysInAnySpelling_ReadsTheScore()
    {
        // Arrange - improvements spell abilities "strength", drafts "STR"
        var prerequisites = new FeatPrerequisites { AbilityMinimums = [Ability("STR", 13)] };
        var scores = new Dictionary<string, int> { ["strength"] = 13 };

        // Act
        var unmet = prerequisites.FindUnmet(4, "human", null, Medium, "fighter", scores, NoFeatures);

        // Assert
        unmet.Should().BeNull();
    }

    [Fact]
    public void FindUnmet_WithOnlyUncheckedRequirements_ReturnsNull()
    {
        // Arrange - War Caster asks for spellcasting, which the app does not model
        var prerequisites = new FeatPrerequisites { Unchecked = ["The ability to cast at least one spell"] };

        // Act
        var unmet = prerequisites.FindUnmet(4, "human", null, Medium, "fighter", Scores, NoFeatures);

        // Assert
        unmet.Should().BeNull();
    }

    [Fact]
    public void Describe_ListsEveryRequirementAsThePlayerReadsIt()
    {
        // Arrange
        var prerequisites = new FeatPrerequisites
        {
            MinimumLevel = 4,
            AbilityMinimums = [Ability("STR", 13), Ability("DEX", 13)],
            RaceIds = ["half_orc"],
            SubraceIds = ["mountain_dwarf"],
            Sizes = ["Small"],
            ClassIds = ["barbarian"],
            Features = ["Fighting Style"],
            Unchecked = ["Proficiency with heavy armor"],
        };

        // Act
        var lines = prerequisites.Describe();

        // Assert
        lines.Should().Equal(
            "Level 4",
            "Strength or Dexterity 13",
            "Half-Orc or Mountain Dwarf or a Small race",
            "Barbarian",
            "The Fighting Style feature",
            "Proficiency with heavy armor");
    }

    [Fact]
    public void Describe_AtLevelOne_LeavesTheLevelOut()
    {
        // Arrange - a level 1 minimum asks for nothing a character does not already have
        var prerequisites = new FeatPrerequisites { MinimumLevel = 1, ClassIds = ["fighter"] };

        // Act
        var lines = prerequisites.Describe();

        // Assert
        lines.Should().Equal("Fighter");
    }

    private static AbilityMinimum Ability(string abilityId, int score) => new() { AbilityId = abilityId, Score = score };
}
