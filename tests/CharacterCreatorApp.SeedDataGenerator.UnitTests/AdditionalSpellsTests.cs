using System.Text.Json.Nodes;

using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

namespace CharacterCreatorApp.SeedDataGenerator.UnitTests;

/// <summary>
/// The blocks are 5etools' "additionalSpells" from feats.json and races.json, shortened to the
/// keys that matter.
/// </summary>
public class AdditionalSpellsTests
{
    [Fact]
    public void Choice_ForMagicInitiate_OffersEachNamedList()
    {
        // Arrange - the 2024 feat, whose spellcasting ability is a choose of its own
        var blocks = Blocks("""
            [
              {"name": "Cleric Spells", "ability": {"choose": ["int", "wis", "cha"]},
               "innate": {"_": {"daily": {"1": [{"choose": "level=1|class=Cleric"}]}}},
               "known": {"_": [{"choose": "level=0|class=Cleric", "count": 2}]}},
              {"name": "Wizard Spells", "ability": {"choose": ["int", "wis", "cha"]},
               "innate": {"_": {"daily": {"1": [{"choose": "level=1|class=Wizard"}]}}},
               "known": {"_": [{"choose": "level=0|class=Wizard", "count": 2}]}}
            ]
            """);

        // Act
        var choice = AdditionalSpells.Choice(blocks, RulesEdition.Edition2024);

        // Assert
        choice!.ClassIds.Should().Equal("cleric-2024", "wizard-2024");
        (choice.Cantrips, choice.FirstLevelSpells).Should().Be((2, 1));
        choice.AbilityOptions.Should().Equal("INT", "WIS", "CHA");
    }

    [Fact]
    public void Choice_WithListsCastWithDifferentAbilitiesToPick_IsNull()
    {
        // Arrange - which abilities the player picks from would depend on the list
        var blocks = Blocks("""
            [
              {"name": "Cleric Spells", "ability": {"choose": ["wis", "cha"]},
               "known": {"_": [{"choose": "level=0|class=Cleric", "count": 2}]}},
              {"name": "Wizard Spells", "ability": {"choose": ["int", "wis", "cha"]},
               "known": {"_": [{"choose": "level=0|class=Wizard", "count": 2}]}}
            ]
            """);

        // Act
        var choice = AdditionalSpells.Choice(blocks, RulesEdition.Edition2024);

        // Assert
        choice.Should().BeNull();
    }

    [Fact]
    public void Choice_ForTheHighElf_IsOneWizardCantrip()
    {
        // Arrange
        var blocks = Blocks("""[{"ability": "int", "known": {"1": {"_": [{"choose": "level=0|class=Wizard"}]}}}]""");

        // Act
        var choice = AdditionalSpells.Choice(blocks, RulesEdition.Edition2014);

        // Assert
        choice!.ClassIds.Should().Equal("wizard");
        (choice.Cantrips, choice.FirstLevelSpells).Should().Be((1, 0));

        // Its Intelligence is fixed, so there is no ability to pick
        choice.AbilityOptions.Should().BeEmpty();
    }

    [Theory]
    // A school filter (Fey Touched) is not a class list
    [InlineData("""[{"innate": {"_": {"daily": {"1e": ["misty step", {"choose": "level=1|school=E;D"}]}}}}]""")]
    // Light, or a cleric cantrip for one who knows it: a named block without a pick
    [InlineData("""[{"name": "Light", "known": {"_": ["light|xphb#c"]}}, {"name": "Cleric Cantrip", "known": {"_": [{"choose": "level=0|class=Cleric"}]}}]""")]
    // Ray of Frost, or another wizard cantrip: two unnamed blocks are alternatives
    [InlineData("""[{"known": {"_": ["ray of frost|xphb#c"]}}, {"known": {"_": [{"choose": "level=0|class=Wizard"}]}}]""")]
    // Only fixed spells
    [InlineData("""[{"known": {"_": ["thaumaturgy#c"]}}]""")]
    public void Choice_ForAnyOtherShape_IsNull(string json)
    {
        // Act
        var choice = AdditionalSpells.Choice(Blocks(json), RulesEdition.Edition2024);

        // Assert
        choice.Should().BeNull();
    }

    [Fact]
    public void FixedAbility_ForTheTiefling_IsCharisma()
    {
        // Arrange - Thaumaturgy, then Hellish Rebuke at 3 and Darkness at 5
        var blocks = Blocks("""
            [
              {"ability": "cha",
               "innate": {"3": {"daily": {"1": ["hellish rebuke#2"]}}, "5": {"daily": {"1": ["darkness"]}}},
               "known": {"1": ["thaumaturgy#c"]}}
            ]
            """);

        // Act
        var ability = AdditionalSpells.FixedAbility(blocks);

        // Assert
        ability.Should().Be("CHA");
    }

    [Theory]
    [InlineData("""[{"ability": {"choose": ["int", "wis", "cha"]}, "known": {"1": ["prestidigitation#c"]}}]""")]
    [InlineData("""[{"ability": "inherit", "known": {"1": ["light#c"]}}]""")]
    [InlineData("""[{"ability": "cha", "known": {"1": ["thaumaturgy#c"]}}, {"ability": "int", "known": {"1": ["light#c"]}}]""")]
    [InlineData("""[{"ability": "cha"}]""")]
    [InlineData("""[]""")]
    public void FixedAbility_ForAPickInheritTwoAbilitiesOrNoSpells_IsNull(string json)
    {
        // Act - a pick (a 2024 high elf's, which its spell choice holds instead), "inherit", two
        // abilities, an ability with no spells, or no block at all
        var ability = AdditionalSpells.FixedAbility(Blocks(json));

        // Assert
        ability.Should().BeNull();
    }

    private static List<JsonObject> Blocks(string json) => JsonNode.Parse(json)!.AsArray().OfType<JsonObject>().ToList();
}
