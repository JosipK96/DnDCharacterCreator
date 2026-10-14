using System.Text.Json.Nodes;

using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

namespace CharacterCreatorApp.SeedDataGenerator.UnitTests;

/// <summary>
/// The prerequisite lists are copied from 5etools' feats.json.
/// </summary>
public class FeatPrerequisiteParserTests
{
    private static readonly Dictionary<string, string> FeatNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Initiate of High Sorcery"] = "Initiate of High Sorcery",
        ["Purple Dragon Rook"] = "Purple Dragon Rook",
        ["Spellfire Spark"] = "Spellfire Spark",
    };

    [Fact]
    public void Parse_WithNone_AsksForNothing()
    {
        // Act
        var prerequisites = FeatPrerequisiteParser.Parse(null, RulesEdition.Edition2014, FeatNames);

        // Assert
        prerequisites.Describe().Should().BeEmpty();
    }

    [Fact]
    public void Parse_AlternativesThatDifferInTheirAbility_FoldIntoOneEitherOrList()
    {
        // Arrange - Athlete, as an older release listed it
        var json = JsonNode.Parse("""[{"level":4,"ability":[{"str":13}]},{"level":4,"ability":[{"dex":13}]}]""");

        // Act
        var prerequisites = FeatPrerequisiteParser.Parse(json, RulesEdition.Edition2024, FeatNames);

        // Assert
        prerequisites.MinimumLevel.Should().Be(4);
        prerequisites.AbilityMinimums.Select(m => (m.AbilityId, m.Score)).Should().Equal(("STR", 13), ("DEX", 13));
    }

    [Theory]
    [InlineData(RulesEdition.Edition2014, new[] { "elf", "half_elf" })]
    [InlineData(RulesEdition.Edition2024, new[] { "elf_2024", "half_elf_2024" })]
    public void Parse_RaceNames_BecomeTheRaceIdsOfTheFeatsEdition(string edition, string[] expected)
    {
        // Arrange - Elven Accuracy
        var json = JsonNode.Parse("""[{"race":[{"name":"elf"},{"name":"half-elf"}]}]""");

        // Act
        var prerequisites = FeatPrerequisiteParser.Parse(json, edition, FeatNames);

        // Assert
        prerequisites.RaceIds.Should().Equal(expected);
    }

    [Theory]
    [InlineData("elf", "wood", "wood_elf")]
    [InlineData("elf", "high", "high_elf")]
    [InlineData("elf", "drow", "drow_elf")]
    public void Parse_RaceWithASubrace_BecomesTheSubraceIdTheRaceImporterWrites(string race, string subrace, string expected)
    {
        // Arrange - Wood Elf Magic, Fey Teleportation, Drow High Magic
        var json = JsonNode.Parse($$"""[{"race":[{"name":"{{race}}","subrace":"{{subrace}}"}]}]""");

        // Act
        var prerequisites = FeatPrerequisiteParser.Parse(json, RulesEdition.Edition2014, FeatNames);

        // Assert
        prerequisites.SubraceIds.Should().Equal(expected);
        prerequisites.RaceIds.Should().BeEmpty();
    }

    [Fact]
    public void Parse_ASizedRace_BecomesASizeBesideTheRaces()
    {
        // Arrange - Squat Nimbleness
        var json = JsonNode.Parse("""[{"race":[{"name":"dwarf"},{"name":"small race","displayEntry":"a Small race"}]}]""");

        // Act
        var prerequisites = FeatPrerequisiteParser.Parse(json, RulesEdition.Edition2014, FeatNames);

        // Assert
        prerequisites.RaceIds.Should().Equal("dwarf");
        prerequisites.Sizes.Should().Equal("Small");
        prerequisites.Describe().Should().Equal("Dwarf or a Small race");
    }

    [Theory]
    [InlineData(RulesEdition.Edition2014, new[] { "fighter", "paladin" })]
    [InlineData(RulesEdition.Edition2024, new[] { "fighter-2024", "paladin-2024" })]
    public void Parse_ClassLevels_BecomeALevelAndTheClassIdsOfTheFeatsEdition(string edition, string[] expected)
    {
        // Arrange - Squire of Solamnia, less its campaign and background
        var json = JsonNode.Parse("""
            [{"level":{"level":1,"class":{"name":"Fighter","visible":true}}},{"level":{"level":1,"class":{"name":"Paladin","visible":true}}}]
            """);

        // Act
        var prerequisites = FeatPrerequisiteParser.Parse(json, edition, FeatNames);

        // Assert
        prerequisites.MinimumLevel.Should().Be(1);
        prerequisites.ClassIds.Should().Equal(expected);
    }

    [Theory]
    [InlineData("""[{"feature":["Fighting Style"]}]""", new[] { "Fighting Style" })]
    [InlineData("""[{"level":4,"spellcasting2020":true}]""", new[] { "Spellcasting", "Pact Magic" })]
    [InlineData("""[{"spellcastingFeature":true}]""", new[] { "Spellcasting" })]
    public void Parse_ClassFeatureRequirements_BecomeFeatureNamesTheAppChecks(string prerequisiteJson, string[] expected)
    {
        // Arrange - Archery, War Caster (2024), Cartomancer
        var json = JsonNode.Parse(prerequisiteJson);

        // Act
        var prerequisites = FeatPrerequisiteParser.Parse(json, RulesEdition.Edition2024, FeatNames);

        // Assert
        prerequisites.Features.Should().Equal(expected);
        prerequisites.Unchecked.Should().BeEmpty();
    }

    [Theory]
    [InlineData("""{"spellcasting":true}""", "The ability to cast at least one spell")]
    [InlineData("""{"proficiency":[{"armor":"heavy"}]}""", "Proficiency with heavy armor")]
    [InlineData("""{"proficiency":[{"armor":"shield"}]}""", "Proficiency with shields")]
    [InlineData("""{"proficiency":[{"weaponGroup":"martial"}]}""", "Proficiency with martial weapons")]
    [InlineData("""{"campaign":["Dragonlance"]}""", "A Dragonlance campaign")]
    [InlineData("""{"campaign":["Eberron"]}""", "An Eberron campaign")]
    [InlineData("""{"background":[{"name":"Rune Carver","displayEntry":"{@background Rune Carver|BGG}"}]}""", "The Rune Carver background")]
    [InlineData("""{"feat":["purple dragon rook|frhof"]}""", "The Purple Dragon Rook feat")]
    [InlineData("""{"feat":["initiate of high sorcery|dsotdq|initiate of high sorcery (nuitari)"]}""", "The Initiate of High Sorcery (Nuitari) feat")]
    [InlineData("""{"featCategory":["D"]}""", "A Dragonmark feat")]
    [InlineData("""{"exclusiveFeatCategory":["D"]}""", "No other Dragonmark feat")]
    [InlineData("""{"other":"No other dragonmark"}""", "No other dragonmark")]
    public void Parse_WhatTheAppCannotCheck_IsKeptInWordsForThePlayer(string requirementJson, string expected)
    {
        // Arrange
        var json = JsonNode.Parse($"[{requirementJson}]");

        // Act
        var prerequisites = FeatPrerequisiteParser.Parse(json, RulesEdition.Edition2014, FeatNames);

        // Assert
        prerequisites.Unchecked.Should().Equal(expected);
    }

    [Fact]
    public void Parse_ARequirementRepeatedAcrossAlternatives_IsListedOnce()
    {
        // Arrange - Initiate of High Sorcery names its campaign in each alternative
        var json = JsonNode.Parse("""[{"campaign":["Dragonlance"]},{"campaign":["Dragonlance"]}]""");

        // Act
        var prerequisites = FeatPrerequisiteParser.Parse(json, RulesEdition.Edition2014, FeatNames);

        // Assert
        prerequisites.Unchecked.Should().Equal("A Dragonlance campaign");
    }

    [Fact]
    public void Parse_AlternativesAskingForDifferentKindsOfThing_AreDescribedAndNotChecked()
    {
        // Arrange - Squire of Solamnia: folded, it would refuse a Knight of Solamnia who is
        // neither a fighter nor a paladin
        var json = JsonNode.Parse("""
            [
              {"campaign":["Dragonlance"],"level":{"level":1,"class":{"name":"Fighter","visible":true}}},
              {"campaign":["Dragonlance"],"level":{"level":1,"class":{"name":"Paladin","visible":true}}},
              {"campaign":["Dragonlance"],"background":[{"name":"Knight of Solamnia","displayEntry":"{@background Knight of Solamnia|DSotDQ}"}]}
            ]
            """);

        // Act
        var prerequisites = FeatPrerequisiteParser.Parse(json, RulesEdition.Edition2014, FeatNames);

        // Assert
        prerequisites.MinimumLevel.Should().BeNull();
        prerequisites.ClassIds.Should().BeEmpty();
        prerequisites.Unchecked.Should().Equal("Fighter or Paladin or the Knight of Solamnia background", "A Dragonlance campaign");
    }

    [Fact]
    public void Parse_DifferentAlternativesWithSeveralRequirements_KeepEachAlternativeTogether()
    {
        // Arrange - Spellfire Adept
        var json = JsonNode.Parse("""[{"level":4,"feat":["spellfire spark|frhof"]},{"feature":["Spellcasting","Pact Magic"]}]""");

        // Act
        var prerequisites = FeatPrerequisiteParser.Parse(json, RulesEdition.Edition2024, FeatNames);

        // Assert
        prerequisites.MinimumLevel.Should().BeNull();
        prerequisites.Features.Should().BeEmpty();
        prerequisites.Unchecked.Should().Equal("Level 4 and the Spellfire Spark feat, or the Spellcasting or Pact Magic feature");
    }
}
