using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Which book text the seed files may carry. Only the System Reference Document (SRD 5.1 for the
/// 2014 rules, SRD 5.2 for 2024) is licensed for publication, under CC-BY 4.0; the rest of the
/// books' text is not. 5etools marks an SRD entry with "srd" or "srd52": true, or the SRD's own
/// name where it renames one ("Hideous Laughter"). Any other entry gets no book prose, only what
/// its mechanics say and where the book has it.
/// </summary>
internal static partial class SrdText
{
    private static readonly Dictionary<string, string> Areas = new()
    {
        ["C"] = "cube",
        ["H"] = "hemisphere",
        ["L"] = "line",
        ["N"] = "cone",
        ["Q"] = "square",
        ["R"] = "circle",
        ["S"] = "sphere",
        ["W"] = "wall",
        ["Y"] = "cylinder",
    };

    /// <summary>
    /// The ids of the rows that kept SRD text, written to SrdIds.json: the record of what is used
    /// under CC-BY 4.0, and what SeedDataIntegrityTests lets carry book text.
    /// </summary>
    public static SortedSet<string> KeptIds { get; } = new(StringComparer.Ordinal);

    /// <summary>True when the entry is in SRD 5.1 or 5.2, so its text may be published.</summary>
    public static bool IsSrd(JsonObject entry) => Marked(entry["srd"]) || Marked(entry["srd52"]);

    /// <summary>The name the SRD gives the entry when it differs from the book's, or null.</summary>
    public static string? SrdName(JsonObject entry) => NameIn(entry["srd"]) ?? NameIn(entry["srd52"]);

    /// <summary>
    /// The book's text for an SRD entry. For any other, the mechanics summary when there is one,
    /// then where the book has the entry. The book text is only rendered when it may be used.
    /// </summary>
    public static string Describe(string id, JsonObject entry, Func<string> bookText, string mechanics = "")
    {
        if (IsSrd(entry))
        {
            KeptIds.Add(id);
            return bookText();
        }

        var reference = Reference(entry);
        return mechanics.Length == 0 ? reference : $"{mechanics}\n\n{reference}";
    }

    /// <summary>"See Player's Handbook (2014), page 241." from the entry's source and page.</summary>
    public static string Reference(JsonObject entry)
    {
        var source = entry.Str("source") ?? "the source book";
        var book = SourceBooks.Title(source);

        return entry.Int("page") is > 0 and var page
            ? string.Create(CultureInfo.InvariantCulture, $"See {book}, page {page}.")
            : $"See {book}.";
    }

    /// <summary>
    /// What a spell does, in facts only, from 5etools' structured fields and the dice in its text:
    /// "Dexterity saving throw. 8d6 fire damage. Area: sphere. Stronger with a higher-level slot."
    /// Range, duration and components are columns of their own.
    /// </summary>
    public static string SpellMechanics(JsonObject spell)
    {
        var parts = new List<string>();

        var attacks = Strings(spell, "spellAttack")
            .Select(a => a switch { "M" => "melee", "R" => "ranged", _ => null })
            .OfType<string>()
            .ToList();

        if (attacks.Count != 0)
        {
            parts.Add($"{Capitalise(string.Join(" or ", attacks))} spell attack.");
        }

        var saves = Strings(spell, "savingThrow").Select(Capitalise).ToList();

        if (saves.Count != 0)
        {
            parts.Add($"{string.Join(" or ", saves)} saving throw.");
        }

        var text = spell["entries"]?.ToJsonString() ?? string.Empty;

        // A cantrip's text names the dice of every tier; its table's first row is the damage at level 1
        var firstTier = ScalingTables(spell["scalingLevelDice"]).Select(t => t["scaling"].Str("1")).OfType<string>().ToList();
        var dice = firstTier.Count != 0
            ? firstTier
            : DamageDice().Matches(text).Select(m => m.Groups["dice"].Value).Distinct().ToList();
        var types = Strings(spell, "damageInflict").ToList();

        if (dice.Count != 0 || types.Count != 0)
        {
            parts.Add($"{string.Join(" ", new[] { string.Join(" or ", dice), string.Join(" or ", types) }.Where(s => s.Length != 0))} damage.");
        }

        if (HealingDice().Match(text) is { Success: true } healing && Strings(spell, "miscTags").Contains("HL"))
        {
            parts.Add($"Heals {healing.Groups["dice"].Value}.");
        }

        var conditions = Strings(spell, "conditionInflict").ToList();

        if (conditions.Count != 0)
        {
            parts.Add($"Can inflict: {string.Join(", ", conditions)}.");
        }

        var areas = Strings(spell, "areaTags").Select(a => Areas.GetValueOrDefault(a)).OfType<string>().ToList();

        if (areas.Count != 0)
        {
            parts.Add($"Area: {string.Join(" or ", areas)}.");
        }

        if (spell["scalingLevelDice"] != null)
        {
            parts.Add("Stronger at character levels 5, 11 and 17.");
        }
        else if (spell["entriesHigherLevel"] != null)
        {
            parts.Add("Stronger when cast with a higher-level slot.");
        }

        return string.Join(" ", parts);
    }

    private static bool Marked(JsonNode? value) => value switch
    {
        JsonValue v when v.TryGetValue<bool>(out var flag) => flag,
        JsonValue v when v.TryGetValue<string>(out var name) => name.Length != 0,
        _ => false,
    };

    private static string? NameIn(JsonNode? value) =>
        value is JsonValue v && v.TryGetValue<string>(out var name) && name.Length != 0 ? name : null;

    // "scalingLevelDice" is one table, or a list of them when a spell has two damage rolls
    private static IEnumerable<JsonObject> ScalingTables(JsonNode? scaling) => scaling switch
    {
        JsonObject table => [table],
        JsonArray tables => tables.OfType<JsonObject>(),
        _ => [],
    };

    private static IEnumerable<string> Strings(JsonObject entry, string key) =>
        entry.Arr(key).Select(n => n.AsString()).OfType<string>();

    private static string Capitalise(string text) =>
        text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];

    // "{@damage 8d6}" and "{@damage 1d10 + 5}": the dice a spell's damage rolls
    [GeneratedRegex(@"\{@damage (?<dice>[^}|]+)")]
    private static partial Regex DamageDice();

    // The first "{@dice 2d4 + 2}" of a healing spell
    [GeneratedRegex(@"\{@dice (?<dice>[^}|]+)")]
    private static partial Regex HealingDice();
}
