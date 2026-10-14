using System.Text.Json.Nodes;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Reads 5etools' "additionalSpells", the way subclasses, feats and races grant spells. Under a
/// level key ("3", "s1", "_") sit plain references ("bless|xphb", "light#c"), filters
/// ({"all": "source=EGW"}) and nested groups ("daily", "ritual"). A "choose" is a pick the player
/// makes, which the app does not ask for, so it is counted and left out.
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
