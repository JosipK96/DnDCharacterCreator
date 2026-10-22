using System.Text.Json.Nodes;

using FluentAssertions;

namespace CharacterCreatorApp.SeedDataGenerator.UnitTests;

/// <summary>The tables are cut down from 5etools' class files: the first rows of each.</summary>
public class SpellSlotTableReaderTests
{
    [Fact]
    public void Read_ASpellSlotTable_ReadsTheSlotsPerSpellLevelWithoutTheEmptyColumns()
    {
        // Arrange - a full caster's first three levels
        var tableGroups = JsonNode.Parse("""
            [
              { "colLabels": ["Cantrips Known"], "rows": [[3], [3], [3]] },
              {
                "colLabels": ["{@filter 1st|spells|level=1}", "{@filter 2nd|spells|level=2}", "{@filter 3rd|spells|level=3}"],
                "rowsSpellProgression": [[2, 0, 0], [3, 0, 0], [4, 2, 0]]
              }
            ]
            """);

        // Act
        var rows = SpellSlotTableReader.Read(tableGroups);

        // Assert
        rows[1].Should().BeEquivalentTo(new SpellSlotRow([2], PactMagicSlots: 0, MaxSpellLevel: 1));
        rows[3].Should().BeEquivalentTo(new SpellSlotRow([4, 2], PactMagicSlots: 0, MaxSpellLevel: 2));
    }

    [Fact]
    public void Read_ALevelWithNoSlots_HasNone()
    {
        // Arrange - a 2014 paladin casts nothing at level 1
        var tableGroups = JsonNode.Parse("""
            [{ "colLabels": ["1st", "2nd"], "rowsSpellProgression": [[0, 0], [2, 0]] }]
            """);

        // Act
        var rows = SpellSlotTableReader.Read(tableGroups);

        // Assert
        rows[1].Should().BeEquivalentTo(new SpellSlotRow([], PactMagicSlots: 0, MaxSpellLevel: 0));
        rows[20].Should().BeEquivalentTo(new SpellSlotRow([], PactMagicSlots: 0, MaxSpellLevel: 0));
    }

    [Fact]
    public void Read_A2014PactMagicTable_ReadsTheSlotsAndTheirLevelFromTheLink()
    {
        // Arrange - the 2014 warlock's columns, the slot level written as a filter link
        var tableGroups = JsonNode.Parse("""
            [{
              "colLabels": ["{@filter Cantrips Known|spells|level=0|class=Warlock}", "{@filter Spells Known|spells|class=Warlock}", "Spell Slots", "Slot Level", "{@filter Invocations Known|optionalfeatures|feature type=ei}"],
              "rows": [
                [2, 2, 1, "{@filter 1st|spells|level=1|class=Warlock}", 0],
                [2, 3, 2, "{@filter 1st|spells|level=1|class=Warlock}", 2],
                [2, 4, 2, "{@filter 2nd|spells|level=2|class=Warlock}", 2]
              ]
            }]
            """);

        // Act
        var rows = SpellSlotTableReader.Read(tableGroups);

        // Assert
        rows[1].Should().BeEquivalentTo(new SpellSlotRow([], PactMagicSlots: 1, MaxSpellLevel: 1));
        rows[3].Should().BeEquivalentTo(new SpellSlotRow([], PactMagicSlots: 2, MaxSpellLevel: 2));
    }

    [Fact]
    public void Read_A2024PactMagicTable_ReadsTheSlotsAndTheirLevelAsNumbers()
    {
        // Arrange - the 2024 warlock's columns, in another order
        var tableGroups = JsonNode.Parse("""
            [{
              "colLabels": ["{@filter Invocations|optionalfeatures|feature type=ei}", "{@filter Cantrips|spells|level=0|class=Warlock}", "{@filter Prepared Spells|spells|level=!0|class=Warlock}", "Spell Slots", "Slot Level"],
              "rows": [[1, 2, 2, 1, 1], [3, 2, 3, 2, 1]]
            }]
            """);

        // Act
        var rows = SpellSlotTableReader.Read(tableGroups);

        // Assert
        rows[2].Should().BeEquivalentTo(new SpellSlotRow([], PactMagicSlots: 2, MaxSpellLevel: 1));
    }

    [Fact]
    public void Read_TwoGroupsOfSpellSlots_Throws()
    {
        // Arrange
        var tableGroups = JsonNode.Parse("""
            [{ "rowsSpellProgression": [[2]] }, { "rowsSpellProgression": [[3]] }]
            """);

        // Act
        var read = () => SpellSlotTableReader.Read(tableGroups);

        // Assert
        read.Should().Throw<InvalidOperationException>().WithMessage("*more than one group*");
    }

    [Fact]
    public void Read_ASlotLevelWithoutASlotCount_Throws()
    {
        // Arrange
        var tableGroups = JsonNode.Parse("""
            [{ "colLabels": ["Slot Level"], "rows": [[1]] }]
            """);

        // Act
        var read = () => SpellSlotTableReader.Read(tableGroups);

        // Assert - the app would show a warlock without slots, so the generator stops
        read.Should().Throw<InvalidOperationException>().WithMessage("*no slot count*");
    }
}
