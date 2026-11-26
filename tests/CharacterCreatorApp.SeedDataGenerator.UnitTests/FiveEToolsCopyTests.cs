using System.Text.Json.Nodes;

using FluentAssertions;

namespace CharacterCreatorApp.SeedDataGenerator.UnitTests;

public class FiveEToolsCopyTests
{
    private static readonly JsonObject Goblin = Entry("""
        {
          "name": "Goblin", "source": "VGM", "page": 119, "srd": true, "reprintedAs": ["Goblin|MPMM"],
          "size": ["S"], "ability": [{"dex": 2, "con": 1}],
          "entries": [{"name": "Size", "entries": ["Small."]}, {"name": "Fury of the Small", "entries": ["Goblins hit harder."]}]
        }
        """);

    [Fact]
    public void Resolve_ForAnEntryThatIsNoCopy_ReturnsItAsItIs()
    {
        // Act
        var resolved = FiveEToolsCopy.Resolve(Goblin, _ => null);

        // Assert
        resolved.Should().BeSameAs(Goblin);
    }

    [Fact]
    public void Resolve_ForACopy_TakesTheSourcesKeysUnderItsOwn()
    {
        // Arrange - ERLW's goblin: VGM's, with its own name, book and page
        var copy = Entry("""{"name": "Goblin", "source": "ERLW", "page": 26, "_copy": {"name": "Goblin", "source": "VGM"}}""");

        // Act
        var resolved = FiveEToolsCopy.Resolve(copy, FindGoblin);

        // Assert
        (resolved.Str("source"), resolved.Int("page")).Should().Be(("ERLW", 26));
        resolved["ability"]!.ToJsonString().Should().Be("""[{"dex":2,"con":1}]""");
        resolved.ContainsKey("_copy").Should().BeFalse();
    }

    [Fact]
    public void Resolve_ForACopyOfAnSrdEntry_IsNoSrdTextAndNoReprint()
    {
        // Arrange
        var copy = Entry("""{"name": "Goblin", "source": "GGR", "_copy": {"name": "Goblin", "source": "VGM"}}""");

        // Act
        var resolved = FiveEToolsCopy.Resolve(copy, FindGoblin);

        // Assert - the SRD licenses the source's text, not the copy's book
        resolved.ContainsKey("srd").Should().BeFalse();
        resolved.ContainsKey("reprintedAs").Should().BeFalse();
    }

    [Fact]
    public void Resolve_WithReplaceAndAppend_ChangesTheSourcesEntries()
    {
        // Arrange
        var copy = Entry("""
            {"name": "Goblin", "source": "ERLW", "_copy": {"name": "Goblin", "source": "VGM", "_mod": {"entries": [
              {"mode": "replaceArr", "replace": "Size", "items": {"name": "Size", "entries": ["Small, in Eberron."]}},
              {"mode": "appendArr", "items": {"name": "Darkvision", "entries": ["60 feet."]}}
            ]}}}
            """);

        // Act
        var resolved = FiveEToolsCopy.Resolve(copy, FindGoblin);

        // Assert
        resolved.Objects("entries").Select(e => e.Str("name")).Should().Equal("Size", "Fury of the Small", "Darkvision");
        resolved.Objects("entries").First()["entries"]![0]!.AsString().Should().Be("Small, in Eberron.");
        Goblin.Objects("entries").Should().HaveCount(2, "the source stays as it is");
    }

    [Fact]
    public void Resolve_WithReplaceTxt_ChangesTheTextNotTheNames()
    {
        // Arrange - AWM's Dankwood goblin
        var copy = Entry("""
            {"name": "Goblin (Dankwood)", "source": "AWM", "_copy": {"name": "Goblin", "source": "VGM", "_mod": {"entries":
              {"mode": "replaceTxt", "replace": "Goblins", "with": "Dankwood goblins", "flags": "g"}}}}
            """);

        // Act
        var resolved = FiveEToolsCopy.Resolve(copy, FindGoblin);

        // Assert
        var fury = resolved.Objects("entries").Single(e => e.Str("name") == "Fury of the Small");
        fury["entries"]![0]!.AsString().Should().Be("Dankwood goblins hit harder.");
    }

    [Fact]
    public void Resolve_ForACopyOfAnEntryNotInTheData_Throws()
    {
        // Arrange
        var copy = Entry("""{"name": "Boggart", "source": "LFL", "_copy": {"name": "Goblin", "source": "MPMM"}}""");

        // Act
        var act = () => FiveEToolsCopy.Resolve(copy, _ => null);

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("Boggart (LFL) copies Goblin (MPMM)*");
    }

    private static JsonObject? FindGoblin(JsonObject copy) =>
        copy.Str("name") == "Goblin" && copy.Str("source") == "VGM" ? Goblin : null;

    private static JsonObject Entry(string json) => JsonNode.Parse(json)!.AsObject();
}
