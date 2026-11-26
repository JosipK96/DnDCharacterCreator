using System.Text.Json.Nodes;

using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

namespace CharacterCreatorApp.SeedDataGenerator.UnitTests;

/// <summary>The race shapes the importer reads beyond the Player's Handbooks' (#142).</summary>
public class RaceImporterTests
{
    private static readonly List<string> AllSix = ["STR", "DEX", "CON", "INT", "WIS", "CHA"];

    [Theory]
    [InlineData("Dwarf", "PHB", "dwarf")]
    [InlineData("Elf", "XPHB", "elf")]
    [InlineData("Aasimar", "MPMM", "aasimar_mpmm")]
    [InlineData("Goblin (Dankwood)", "AWM", "goblin_dankwood_awm")]
    public void BaseId_ForARaceOfAnotherBook_EndsInTheBook(string name, string book, string expected)
    {
        // Act
        var id = RaceImporter.BaseId(name, book);

        // Assert
        id.Should().Be(expected);
    }

    [Theory]
    [InlineData("Elf; Drow Lineage", "Drow Lineage")]
    [InlineData("Dragonborn (Black)", "Black")]
    [InlineData("Dragonborn (Chromatic; Black)", "Black")]
    [InlineData("Variant; Gifted Aetherborn", "Gifted Aetherborn")]
    public void VersionName_IsTheVersionsOwnName(string fullName, string expected)
    {
        // Act
        var name = RaceImporter.VersionName(fullName);

        // Assert
        name.Should().Be(expected);
    }

    [Theory]
    [InlineData("Variant; Mark of Detection", "Mark of Detection")]
    [InlineData("Zendikar; Grotag Tribe", "Grotag Tribe")]
    [InlineData("Hill", "Hill")]
    public void SubraceName_IsTheSubracesOwnName(string name, string expected)
    {
        // Act
        var own = RaceImporter.SubraceName(name);

        // Assert
        own.Should().Be(expected);
    }

    [Theory]
    [InlineData("30", null)]
    [InlineData("""{"walk": 30}""", null)]
    [InlineData("""{"walk": 25, "fly": 50}""", "Flying speed 50 feet.")]
    [InlineData("""{"walk": 30, "swim": true}""", "Swimming speed equal to your walking speed.")]
    [InlineData("""{"walk": 30, "fly": 30, "climb": 30}""", "Flying speed 30 feet. Climbing speed 30 feet.")]
    public void OtherSpeeds_SaysEverySpeedButWalking(string speed, string? expected)
    {
        // Act
        var mechanics = RaceImporter.OtherSpeeds(JsonNode.Parse(speed));

        // Assert
        mechanics.Should().Be(expected);
    }

    [Theory]
    [InlineData("""{"creatureTypes": ["humanoid"]}""", null)]
    [InlineData("""{}""", null)]
    [InlineData("""{"creatureTypes": ["fey"]}""", "Your creature type is Fey.")]
    public void CreatureType_SaysOnlyOneOtherThanHumanoid(string race, string? expected)
    {
        // Act
        var mechanics = RaceImporter.CreatureType(Entry(race));

        // Assert
        mechanics.Should().Be(expected);
    }

    [Fact]
    public void AbilityChoice_ForTheHalfElf_IsTwoPlusOnes()
    {
        // Act
        var choice = AbilityChoice("""{"ability": [{"cha": 2, "choose": {"from": ["str", "dex", "con", "int", "wis"], "count": 2}}]}""", lineageRules: false);

        // Assert
        choice.Should().BeEquivalentTo(new RaceImporter.AbilityChoiceRow(2, ["STR", "DEX", "CON", "INT", "WIS"], 1, 1));
    }

    [Fact]
    public void AbilityChoice_WithAnAmount_IsThatMuchOnEachPick()
    {
        // Act - a SCAG winged tiefling's +2 to Dexterity or Charisma
        var choice = AbilityChoice("""{"ability": [{"int": 1, "choose": {"from": ["dex", "cha"], "count": 1, "amount": 2}}]}""", lineageRules: false);

        // Assert
        choice.Should().BeEquivalentTo(new RaceImporter.AbilityChoiceRow(2, ["DEX", "CHA"], 2, 2));
    }

    [Fact]
    public void AbilityChoice_UnderTashasLineageRules_IsPlusTwoAndPlusOneOrPlusOneToThree()
    {
        // Act - a Monsters of the Multiverse race has no increases of its own
        var choice = AbilityChoice("""{"lineage": "VRGR"}""", lineageRules: true);

        // Assert
        choice.Should().BeEquivalentTo(new RaceImporter.AbilityChoiceRow(3, AllSix, 2, 1));
    }

    [Fact]
    public void AbilityChoice_UnderTashasLineageRulesWithIncreasesOfItsOwn_KeepsThem()
    {
        // Act - Custom Lineage names its +2 itself
        var choice = AbilityChoice("""{"lineage": true, "ability": [{"choose": {"from": ["str", "dex", "con", "int", "wis", "cha"], "amount": 2}}]}""", lineageRules: true);

        // Assert
        choice.Should().BeEquivalentTo(new RaceImporter.AbilityChoiceRow(2, AllSix, 2, 2));
    }

    [Fact]
    public void AbilityChoice_WithASecondChoice_NotesItAndKeepsTheFirst()
    {
        // Arrange
        var skipped = new List<string>();

        // Act
        var choice = RaceImporter.AbilityChoice(
            [Entry("""{"ability": [{"choose": {"from": ["str"], "count": 1}}]}"""), Entry("""{"ability": [{"choose": {"from": ["dex"], "count": 1}}]}""")],
            lineageRules: false,
            skipped.Add);

        // Assert
        choice.Options.Should().Equal("STR");
        skipped.Should().ContainSingle().Which.Should().StartWith("ability increases");
    }

    [Fact]
    public void EditionOfBook_ForA2024Book_Is2024()
    {
        // Act - RHW's dhampir and Lorwyn's species carry no "edition" of their own
        var editions = new[] { "XPHB", "RHW", "LFL", "EFA", "VGM", "MPMM" }.Select(SourceBooks.EditionOfBook);

        // Assert
        editions.Should().Equal(
            RulesEdition.Edition2024, RulesEdition.Edition2024, RulesEdition.Edition2024, RulesEdition.Edition2024,
            RulesEdition.Edition2014, RulesEdition.Edition2014);
    }

    private static RaceImporter.AbilityChoiceRow AbilityChoice(string race, bool lineageRules) =>
        RaceImporter.AbilityChoice([Entry(race)], lineageRules, _ => { });

    private static JsonObject Entry(string json) => JsonNode.Parse(json)!.AsObject();
}
