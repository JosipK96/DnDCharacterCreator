using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Reads the spell slots out of a 5etools class's or subclass's table groups. A full, half or
/// third caster's slots are a group's "rowsSpellProgression", one array per character level with
/// a count per spell level; a warlock's Pact Magic is two plain columns, "Spell Slots" and "Slot
/// Level".
/// </summary>
internal static partial class SpellSlotTableReader
{
    /// <returns>What the table gives at each character level, indexed 1 to 20 (index 0 is unused).</returns>
    public static SpellSlotRow[] Read(JsonNode? tableGroups)
    {
        var spellSlots = Enumerable.Range(0, 21).Select(_ => new List<int>()).ToArray();
        var pactMagicSlots = new int[21];
        var slotLevels = new int[21];

        foreach (var group in tableGroups as JsonArray ?? [])
        {
            var slotRows = group.Arr("rowsSpellProgression");

            for (var i = 0; i < slotRows.Count && i < 20; i++)
            {
                spellSlots[i + 1] = WithoutTrailingZeros((slotRows[i] as JsonArray ?? []).Select(IntIn).ToList());
            }

            var labels = group.Arr("colLabels")
                .Select(l => FiveEToolsMarkup.ToPlainText(l.AsString() ?? string.Empty))
                .ToList();
            var slotLevelColumn = labels.IndexOf("Slot Level");

            if (slotLevelColumn < 0)
            {
                continue;
            }

            var slotCountColumn = labels.IndexOf("Spell Slots");

            if (slotCountColumn < 0)
            {
                throw new InvalidOperationException($"A Pact Magic table with a slot level but no slot count: {string.Join(", ", labels)}.");
            }

            var rows = group.Arr("rows");

            for (var i = 0; i < rows.Count && i < 20; i++)
            {
                var cells = rows[i] as JsonArray;
                slotLevels[i + 1] = SlotLevelIn(cells?.ElementAtOrDefault(slotLevelColumn));
                pactMagicSlots[i + 1] = IntIn(cells?.ElementAtOrDefault(slotCountColumn));
            }
        }

        return Enumerable.Range(0, 21)
            .Select(level => new SpellSlotRow(spellSlots[level], pactMagicSlots[level], Math.Max(spellSlots[level].Count, slotLevels[level])))
            .ToArray();
    }

    // A table lists every spell level the class ever reaches; a 1st-level wizard's row is
    // [2, 0, 0, ...], kept as [2] so the slots end at the highest spell level
    private static List<int> WithoutTrailingZeros(List<int> slots)
    {
        var count = slots.Count;

        while (count > 0 && slots[count - 1] == 0)
        {
            count--;
        }

        return slots.Take(count).ToList();
    }

    private static int IntIn(JsonNode? cell) =>
        cell is JsonValue value && value.TryGetValue<int>(out var number) ? number : 0;

    // The 2014 table writes a slot level as a link, "{@filter 1st|spells|level=1|class=Warlock}";
    // the 2024 one as the number
    private static int SlotLevelIn(JsonNode? cell)
    {
        if (cell is JsonValue value && value.TryGetValue<int>(out var level))
        {
            return level;
        }

        return cell.AsString() is { } text && SlotLevel().Match(text) is { Success: true } match
            ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)
            : 0;
    }

    [GeneratedRegex(@"level=(\d)")]
    private static partial Regex SlotLevel();
}

/// <summary>
/// One character level of a spell table: the slots per spell level (index 0 for 1st level), a
/// warlock's Pact Magic slots, and the highest spell level with a slot of either kind.
/// </summary>
internal sealed record SpellSlotRow(List<int> SpellSlots, int PactMagicSlots, int MaxSpellLevel);
