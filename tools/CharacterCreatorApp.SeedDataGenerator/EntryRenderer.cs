using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Turns 5etools "entries" (nested paragraphs, lists, tables and references to other features)
/// into plain text. Paragraphs are separated by a blank line and list items and table rows go
/// one to a line, so a page that keeps line breaks shows the structure.
/// </summary>
internal sealed partial class EntryRenderer(Func<string, string, JsonObject?> resolveReference)
{
    // Types whose content is their "entries", shown after the name when they have one
    private static readonly HashSet<string> BlockTypes =
    [
        "entries", "section", "inset", "insetReadaloud", "variant", "variantInner", "variantSub",
        "item", "itemSub", "itemSpell", "quote", "flowchart", "flowBlock",
    ];

    private readonly Func<string, string, JsonObject?> _resolveReference = resolveReference;
    private readonly HashSet<string> _resolving = [];

    /// <summary>Entry types nothing here knows how to show; reported, then shown as their entries.</summary>
    public SortedSet<string> UnknownTypes { get; } = [];

    /// <summary>References that named nothing in the data; shown as the name they carry.</summary>
    public SortedSet<string> UnresolvedReferences { get; } = [];

    public string Render(JsonNode? entries) => Tidy(string.Join("\n\n", Blocks(entries)));

    /// <summary>The entries under a heading: "Name. First paragraph" and the rest after it.</summary>
    public string RenderNamed(string? name, JsonNode? entries) => Tidy(string.Join("\n\n", Named(name, entries)));

    private List<string> Blocks(JsonNode? node)
    {
        var blocks = new List<string>();

        switch (node)
        {
            case JsonArray array:
                foreach (var item in array)
                {
                    blocks.AddRange(Blocks(item));
                }

                break;
            case JsonValue value:
                blocks.Add(Text(value));
                break;
            case JsonObject entry:
                blocks.AddRange(EntryBlocks(entry));
                break;
        }

        return blocks.Where(b => b.Length > 0).ToList();
    }

    private List<string> EntryBlocks(JsonObject entry)
    {
        var type = entry.Str("type");

        switch (type)
        {
            case "list":
                return [string.Join("\n", entry.Arr("items").Select(item => "• " + Inline(item)))];
            case "options":
                return [string.Join("\n", entry.Arr("entries").Select(item => "• " + Inline(item)))];
            case "table":
                return [Table(entry)];
            case "refClassFeature":
                return Referenced("classFeature", entry.Str("classFeature"));
            case "refSubclassFeature":
                return Referenced("subclassFeature", entry.Str("subclassFeature"));
            case "refOptionalfeature":
                return [NameOf(entry.Str("optionalfeature"))];
            case "refFeat":
                return [NameOf(entry.Str("feat"))];
            case "abilityDc":
                return [$"{entry.Str("name")} save DC = 8 + your proficiency bonus + your {Abilities(entry)} modifier"];
            case "abilityAttackMod":
                return [$"{entry.Str("name")} attack modifier = your proficiency bonus + your {Abilities(entry)} modifier"];
            case "statblock" or "statblockInline":
                return [Text(entry.Str("name") ?? string.Empty)];
            case "hr" or "image" or "gallery":
                return [];
        }

        if (type != null && !BlockTypes.Contains(type))
        {
            UnknownTypes.Add(type);
        }

        return Named(entry.Str("name"), entry["entries"] ?? entry["entry"] ?? entry["items"]);
    }

    private List<string> Named(string? name, JsonNode? body)
    {
        var blocks = Blocks(body);

        if (string.IsNullOrWhiteSpace(name))
        {
            return blocks;
        }

        var title = Text(name);

        if (blocks.Count == 0)
        {
            return [title];
        }

        blocks[0] = $"{title}. {blocks[0]}";
        return blocks;
    }

    // A list item or table cell on one line
    private string Inline(JsonNode? node) => string.Join(" ", Blocks(node));

    private string Table(JsonObject table)
    {
        var lines = new List<string>();

        if (table.Str("caption") is { } caption)
        {
            lines.Add(Text(caption));
        }

        var labels = table.Arr("colLabels").Select(Inline).ToList();

        if (labels.Count > 0)
        {
            lines.Add(string.Join(" | ", labels));
        }

        foreach (var row in table.Arr("rows"))
        {
            // A row is an array of cells, or an object holding one under "row"
            var cells = row as JsonArray ?? row.Arr("row");
            lines.Add(string.Join(" | ", cells.Select(Cell)));
        }

        return string.Join("\n", lines);
    }

    private string Cell(JsonNode? cell)
    {
        if (cell is JsonObject entry && entry.Str("type") == "cell")
        {
            var roll = entry["roll"];

            if (roll.Int("exact") is { } exact)
            {
                return exact.ToString(CultureInfo.InvariantCulture);
            }

            if (roll.Int("min") is { } min && roll.Int("max") is { } max)
            {
                return $"{min}–{max}";
            }

            return Inline(entry["entry"]);
        }

        return Inline(cell);
    }

    private List<string> Referenced(string kind, string? reference)
    {
        if (reference == null)
        {
            return [];
        }

        var feature = _resolveReference(kind, reference);

        // A reference back to a feature already being shown would never end
        if (feature == null || !_resolving.Add(reference))
        {
            if (feature == null)
            {
                UnresolvedReferences.Add($"{kind} {reference}");
            }

            return [NameOf(reference)];
        }

        try
        {
            return Named(feature.Str("name"), feature["entries"]);
        }
        finally
        {
            _resolving.Remove(reference);
        }
    }

    private static string NameOf(string? reference) =>
        reference == null ? string.Empty : Text(reference.Split('|')[0]);

    private static string Abilities(JsonObject entry) =>
        string.Join(" or ", entry.Arr("attributes").Select(a => AbilityScore.FromId(a.AsString() ?? string.Empty).Name));

    private static string Text(JsonValue value) =>
        value.TryGetValue<string>(out var text) ? Text(text) : value.ToJsonString();

    private static string Text(string text) => FiveEToolsMarkup.ToPlainText(text);

    private static string Tidy(string text)
    {
        // 5etools has a few en dashes lost to a bad encoding ("18�20"); a range is the
        // only place they occur
        text = text.Replace('�', '–');

        return ExtraBlankLines().Replace(text, "\n\n").Trim();
    }

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExtraBlankLines();
}
