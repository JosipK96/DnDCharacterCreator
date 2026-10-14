using System.Text.Json.Nodes;

using FluentAssertions;

namespace CharacterCreatorApp.SeedDataGenerator.UnitTests;

public class EntryRendererTests
{
    private static readonly JsonObject PreserveLife = JsonNode.Parse("""
        {
          "name": "Channel Divinity: Preserve Life",
          "entries": ["You restore {@dice 5d6|five times your cleric level} hit points."]
        }
        """)!.AsObject();

    private readonly EntryRenderer _renderer = new((kind, reference) =>
        kind == "subclassFeature" && reference.StartsWith("Channel Divinity: Preserve Life|", StringComparison.Ordinal)
            ? PreserveLife
            : null);

    [Fact]
    public void Render_Paragraphs_TakesOutTheMarkupAndLeavesABlankLineBetween()
    {
        // Arrange
        var entries = Entries("""["You deal {@damage 1d8} damage.", "Then {@spell fireball|phb} goes off."]""");

        // Act
        var text = _renderer.Render(entries);

        // Assert
        text.Should().Be("You deal 1d8 damage.\n\nThen fireball goes off.");
    }

    [Fact]
    public void Render_NamedEntries_PutTheNameBeforeTheFirstParagraph()
    {
        // Arrange
        var entries = Entries("""[{"type": "entries", "name": "Spellbook", "entries": ["Six spells.", "Two more each level."]}]""");

        // Act
        var text = _renderer.Render(entries);

        // Assert
        text.Should().Be("Spellbook. Six spells.\n\nTwo more each level.");
    }

    [Fact]
    public void Render_List_PutsEachItemOnItsOwnLine()
    {
        // Arrange
        var entries = Entries("""
            [{"type": "list", "items": ["Archery", {"type": "item", "name": "Defense", "entry": "+1 AC in armor."}]}]
            """);

        // Act
        var text = _renderer.Render(entries);

        // Assert
        text.Should().Be("• Archery\n• Defense. +1 AC in armor.");
    }

    [Fact]
    public void Render_Table_WritesTheLabelsAndEachRowAcross()
    {
        // Arrange
        var entries = Entries("""
            [{"type": "table", "caption": "Life Domain Spells", "colLabels": ["Cleric Level", "Spells"],
              "rows": [["1st", "{@spell bless}, {@spell cure wounds}"], [{"type": "cell", "roll": {"min": 3, "max": 4}}, "x"]]}]
            """);

        // Act
        var text = _renderer.Render(entries);

        // Assert
        text.Should().Be("Life Domain Spells\nCleric Level | Spells\n1st | bless, cure wounds\n3–4 | x");
    }

    [Fact]
    public void Render_Reference_ShowsTheFeatureItNamesUnderItsName()
    {
        // Arrange
        var entries = Entries("""[{"type": "refSubclassFeature", "subclassFeature": "Channel Divinity: Preserve Life|Cleric||Life||2"}]""");

        // Act
        var text = _renderer.Render(entries);

        // Assert
        text.Should().Be("Channel Divinity: Preserve Life. You restore five times your cleric level hit points.");
        _renderer.UnresolvedReferences.Should().BeEmpty();
    }

    [Fact]
    public void Render_ReferenceToNothing_ShowsItsNameAndIsReported()
    {
        // Arrange
        var entries = Entries("""[{"type": "refClassFeature", "classFeature": "Nowhere|Fighter||3"}]""");

        // Act
        var text = _renderer.Render(entries);

        // Assert
        text.Should().Be("Nowhere");
        _renderer.UnresolvedReferences.Should().Equal("classFeature Nowhere|Fighter||3");
    }

    [Fact]
    public void Render_AbilityDc_SpellsOutTheFormula()
    {
        // Arrange
        var entries = Entries("""[{"type": "abilityDc", "name": "Spell", "attributes": ["int"]}]""");

        // Act
        var text = _renderer.Render(entries);

        // Assert
        text.Should().Be("Spell save DC = 8 + your proficiency bonus + your Intelligence modifier");
    }

    [Fact]
    public void Render_ABrokenDashInARange_BecomesAnEnDash()
    {
        // Arrange - 5etools has "18�20" for "18–20" in a few places
        var entries = Entries("[\"A critical hit on a roll of 18�20.\"]");

        // Act
        var text = _renderer.Render(entries);

        // Assert
        text.Should().Be("A critical hit on a roll of 18–20.");
    }

    [Fact]
    public void Render_AnUnknownType_ShowsItsEntriesAndIsReported()
    {
        // Arrange
        var entries = Entries("""[{"type": "somethingNew", "entries": ["Still shown."]}]""");

        // Act
        var text = _renderer.Render(entries);

        // Assert
        text.Should().Be("Still shown.");
        _renderer.UnknownTypes.Should().Equal("somethingNew");
    }

    private static JsonNode Entries(string json) => JsonNode.Parse(json)!;
}
