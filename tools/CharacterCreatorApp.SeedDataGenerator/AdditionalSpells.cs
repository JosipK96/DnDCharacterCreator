using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using CharacterCreatorApp.Domain.DomainModels.Spells;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Reads 5etools' "additionalSpells", the way subclasses, feats and races grant spells. Under a
/// level key ("3", "s1", "_") sit plain references ("bless|xphb", "light#c"), filters
/// ({"all": "source=EGW"}) and nested groups ("daily", "ritual"). A "choose" is a pick the player
/// makes: References counts and leaves it out, and Choice reads the ones the app asks for.
/// </summary>
internal static class AdditionalSpells
{
    /// <summary>The references and filters in a node, filters written as "source=EGW".</summary>
    public static IEnumerable<string> References(JsonNode? node, Action onChoice)
    {
        switch (node)
        {
            case JsonValue value when value.TryGetValue<string>(out var reference):
                yield return reference;
                break;
            case JsonArray array:
                foreach (var reference in array.SelectMany(item => References(item, onChoice)))
                {
                    yield return reference;
                }

                break;
            case JsonObject entry when entry.Str("all") is { } filter:
                yield return filter;
                break;
            case JsonObject entry when entry["choose"] != null:
                onChoice();
                break;
            case JsonObject group:
                foreach (var reference in group.SelectMany(kvp => References(kvp.Value, onChoice)))
                {
                    yield return reference;
                }

                break;
        }
    }

    public static bool IsFilter(string reference) => reference.Contains('=', StringComparison.Ordinal);

    /// <summary>
    /// The pick a feat's or race's blocks ask for, when every pick in them is "N spells of level 0
    /// or 1 from one class's list" ("level=0|class=Wizard"): one block for a fixed list, or one
    /// named block per list the player picks between (Magic Initiate's "Cleric Spells"), each
    /// asking the same. Anything else (a school, a ritual, two classes in one filter) is null,
    /// and stays a pick the app does not ask for.
    /// </summary>
    public static BonusSpellChoice? Choice(IReadOnlyList<JsonObject> blocks, string edition)
    {
        var picking = blocks.Where(block => SpellPicks(block).Any()).ToList();

        // Several blocks are alternatives, so each must be a pick of its own named list: Light
        // Bringer's "Light" or "Cleric Cantrip", or Cold Caster's Ray of Frost or another
        // wizard cantrip, is not a list to pick from
        if (picking.Count == 0 || picking.Count != blocks.Count || (blocks.Count > 1 && blocks.Any(block => block.Str("name") == null)))
        {
            return null;
        }

        var choices = picking.Select(block => BlockChoice(block, edition)).ToList();
        var abilityOptions = picking.Select(AbilityOptions).ToList();

        if (choices.Contains(null)
            || choices.Select(c => (c!.Cantrips, c.FirstLevelSpells)).Distinct().Count() > 1
            || choices.Select(c => c!.ClassIds[0]).Distinct().Count() != choices.Count
            || abilityOptions.Exists(options => !options.SequenceEqual(abilityOptions[0])))
        {
            return null;
        }

        return new BonusSpellChoice
        {
            ClassIds = choices.Select(c => c!.ClassIds[0]).ToList(),
            Cantrips = choices[0]!.Cantrips,
            FirstLevelSpells = choices[0]!.FirstLevelSpells,
            AbilityOptions = abilityOptions[0],
        };
    }

    // The spellcasting ability a block lets the player pick, {"choose": ["int", "wis", "cha"]} (the
    // 2024 Magic Initiate's), by canonical id; none for a fixed one ("con") or "inherit"
    private static List<string> AbilityOptions(JsonObject block) =>
        block["ability"] is JsonObject { } ability && ability["choose"] is JsonArray options
            ? options.Select(option => AbilityScore.Normalize(option!.GetValue<string>())).ToList()
            : [];

    private static BonusSpellChoice? BlockChoice(JsonObject block, string edition)
    {
        var picks = SpellPicks(block)
            .Select(choose => (Match: ClassLevelFilter.Match(choose.Str("choose") ?? string.Empty), Count: choose.Int("count") ?? 1))
            .ToList();

        if (picks.Exists(p => !p.Match.Success) || picks.Select(p => ClassName(p.Match)).Distinct().Count() != 1)
        {
            return null;
        }

        var className = ClassName(picks[0].Match);

        return new BonusSpellChoice
        {
            ClassIds = [edition == RulesEdition.Edition2024 ? $"{className}-2024" : className],
            Cantrips = picks.Where(p => p.Match.Groups["level"].Value == "0").Sum(p => p.Count),
            FirstLevelSpells = picks.Where(p => p.Match.Groups["level"].Value == "1").Sum(p => p.Count),
        };
    }

    private static string ClassName(Match match) => match.Groups["class"].Value.ToLowerInvariant();

    // Every {"choose": ...} among a block's spells, however deep its level and frequency keys put
    // it; the block's "ability" may be a choose too, of the spellcasting ability
    private static IEnumerable<JsonObject> SpellPicks(JsonObject block) =>
        SpellKinds.SelectMany(kind => Chooses(block[kind]));

    private static IEnumerable<JsonObject> Chooses(JsonNode? node) =>
        node switch
        {
            JsonObject entry when entry["choose"] != null => [entry],
            JsonObject group => group.SelectMany(kvp => Chooses(kvp.Value)),
            JsonArray array => array.SelectMany(Chooses),
            _ => [],
        };

    private static readonly string[] SpellKinds = ["known", "innate", "prepared"];

    private static readonly Regex ClassLevelFilter = new("^level=(?<level>[01])\\|class=(?<class>[A-Za-z]+)$", RegexOptions.CultureInvariant);

    /// <summary>
    /// "bless", "bless|xphb" or "light#c" (a cantrip) as the spell's name and book. No book, or
    /// an empty one ("fire shield|" in the Zeal domain), means the 2014 Player's Handbook.
    /// </summary>
    public static (string Name, string Source) Parse(string reference)
    {
        var name = reference.Split('|', '#')[0];
        var source = reference.Contains('|', StringComparison.Ordinal) ? reference.Split('|')[1].Split('#')[0] : string.Empty;

        return (name, source.Length == 0 ? "PHB" : source);
    }
}
