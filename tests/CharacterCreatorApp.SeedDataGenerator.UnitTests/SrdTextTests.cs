using System.Text.Json.Nodes;

using FluentAssertions;

namespace CharacterCreatorApp.SeedDataGenerator.UnitTests;

/// <summary>
/// The entries are trimmed 5etools objects: only the fields SrdText reads, with the mechanics of
/// the real spells they are named after.
/// </summary>
public class SrdTextTests
{
    [Theory]
    [InlineData("test_fireball", """{"name": "Fireball", "source": "PHB", "page": 241, "srd": true}""")]
    [InlineData("test_fireball_2024", """{"name": "Fireball", "source": "XPHB", "page": 274, "srd52": true}""")]
    [InlineData("test_tashas_hideous_laughter", """{"name": "Tasha's Hideous Laughter", "source": "PHB", "page": 280, "srd": "Hideous Laughter"}""")]
    public void Describe_AnEntryInTheSrd_KeepsTheBookTextAndRecordsItsId(string id, string json)
    {
        // Act
        var description = SrdText.Describe(id, Entry(json), () => "The book's text.", "Dexterity saving throw.");

        // Assert
        description.Should().Be("The book's text.");
        SrdText.KeptIds.Should().Contain(id);
    }

    [Fact]
    public void Describe_AnEntryOutsideTheSrd_GivesItsMechanicsAndWhereTheBookHasItWithoutTheBookText()
    {
        // Arrange - the book text is not even rendered for an entry that may not carry it
        var entry = Entry("""{"name": "Toll the Dead", "source": "XGE", "page": 169}""");

        // Act
        var description = SrdText.Describe("test_toll_the_dead", entry, () => throw new InvalidOperationException("rendered"), "Wisdom saving throw. 1d8 necrotic damage.");

        // Assert
        description.Should().Be("Wisdom saving throw. 1d8 necrotic damage.\n\nSee Xanathar's Guide to Everything, page 169.");
        SrdText.KeptIds.Should().NotContain("test_toll_the_dead");
    }

    [Fact]
    public void Describe_AnEntryOutsideTheSrdWithNoMechanics_GivesOnlyWhereTheBookHasIt()
    {
        // Act
        var description = SrdText.Describe("test_alert", Entry("""{"name": "Alert", "source": "PHB", "page": 165}"""), () => "The book's text.");

        // Assert
        description.Should().Be("See Player's Handbook (2014), page 165.");
    }

    [Theory]
    [InlineData("""{"source": "ZZZ", "page": 12}""", "See ZZZ, page 12.")]
    [InlineData("""{"source": "XPHB"}""", "See Player's Handbook (2024).")]
    public void Reference_ASourceWithOrWithoutATitleOrPage_NamesWhatIsKnown(string json, string expected)
    {
        // Act
        var reference = SrdText.Reference(Entry(json));

        // Assert
        reference.Should().Be(expected);
    }

    [Theory]
    [InlineData("""{"name": "Tasha's Hideous Laughter", "srd": "Hideous Laughter"}""", "Hideous Laughter")]
    [InlineData("""{"name": "Fireball", "srd": true}""", null)]
    [InlineData("""{"name": "Toll the Dead"}""", null)]
    public void SrdName_TheSrdMark_IsTheNameOnlyWhenItIsOne(string json, string? expected)
    {
        // Act
        var name = SrdText.SrdName(Entry(json));

        // Assert
        name.Should().Be(expected);
    }

    [Fact]
    public void SpellMechanics_ASaveForHalfSpell_NamesTheSaveDamageAreaAndScaling()
    {
        // Arrange - Fireball
        var spell = Entry("""
            {"level": 3, "savingThrow": ["dexterity"], "damageInflict": ["fire"], "areaTags": ["S"],
             "entries": ["A bright streak flashes ... {@damage 8d6} fire damage ..."],
             "entriesHigherLevel": [{"type": "entries", "name": "At Higher Levels", "entries": ["..."]}]}
            """);

        // Act
        var mechanics = SrdText.SpellMechanics(spell);

        // Assert
        mechanics.Should().Be("Dexterity saving throw. 8d6 fire damage. Area: sphere. Stronger when cast with a higher-level slot.");
    }

    [Fact]
    public void SpellMechanics_AnAttackCantrip_NamesTheAttackDamageAndCharacterLevelScaling()
    {
        // Arrange - Fire Bolt
        var spell = Entry("""
            {"level": 0, "spellAttack": ["R"], "damageInflict": ["fire"],
             "entries": ["You hurl a mote of fire ... {@damage 1d10} fire damage ..."],
             "scalingLevelDice": {"label": "fire damage", "scaling": {"1": "1d10", "5": "2d10"}}}
            """);

        // Act
        var mechanics = SrdText.SpellMechanics(spell);

        // Assert
        mechanics.Should().Be("Ranged spell attack. 1d10 fire damage. Stronger at character levels 5, 11 and 17.");
    }

    [Fact]
    public void SpellMechanics_ACantripWithTwoScalingDice_NamesOnlyTheirFirstLevelDice()
    {
        // Arrange - Toll the Dead, whose text names every tier of both dice
        var spell = Entry("""
            {"level": 0, "savingThrow": ["wisdom"], "damageInflict": ["necrotic"],
             "entries": ["... {@damage 1d8} necrotic damage ... {@damage 1d12} instead ... {@damage 2d8} ... {@damage 2d12} ..."],
             "scalingLevelDice": [
               {"label": "necrotic damage", "scaling": {"1": "1d8", "5": "2d8", "11": "3d8", "17": "4d8"}},
               {"label": "necrotic damage to a creature missing any of its hit points", "scaling": {"1": "1d12", "5": "2d12", "11": "3d12", "17": "4d12"}}
             ]}
            """);

        // Act
        var mechanics = SrdText.SpellMechanics(spell);

        // Assert
        mechanics.Should().Be("Wisdom saving throw. 1d8 or 1d12 necrotic damage. Stronger at character levels 5, 11 and 17.");
    }

    [Fact]
    public void SpellMechanics_AHealingSpell_NamesWhatItHeals()
    {
        // Arrange - Cure Wounds (2014)
        var spell = Entry("""
            {"level": 1, "miscTags": ["HL"],
             "entries": ["A creature you touch regains a number of hit points equal to {@dice 1d8} + your spellcasting ability modifier."],
             "entriesHigherLevel": [{"type": "entries", "name": "At Higher Levels", "entries": ["..."]}]}
            """);

        // Act
        var mechanics = SrdText.SpellMechanics(spell);

        // Assert
        mechanics.Should().Be("Heals 1d8. Stronger when cast with a higher-level slot.");
    }

    [Fact]
    public void SpellMechanics_ASpellWithNoMechanicalFields_IsEmpty()
    {
        // Arrange - Message
        var spell = Entry("""{"level": 0, "entries": ["You point your finger toward a creature ..."]}""");

        // Act
        var mechanics = SrdText.SpellMechanics(spell);

        // Assert
        mechanics.Should().BeEmpty();
    }

    private static JsonObject Entry(string json) => JsonNode.Parse(json)!.AsObject();
}
