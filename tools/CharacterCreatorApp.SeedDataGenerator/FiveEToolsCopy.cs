using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Resolves a 5etools entry that is a copy of another ("_copy": {"name": "Goblin", "source":
/// "VGM", "_mod": {...}}): the entry it names, with the copy's own keys on top and the changes in
/// "_mod" applied to its lists. ERLW's goblinoids, MOT's reprints, the Plane Shift humans and most
/// of Lorwyn's species are copies. What says where the source's text is licensed or reprinted
/// stays with the source, so a copy of an SRD race is not SRD text.
/// </summary>
internal static class FiveEToolsCopy
{
    // The source's marks that belong to it alone, unless the copy's "_preserve" keeps one
    private static readonly string[] SourceOnlyKeys =
    [
        "srd", "srd52", "basicRules", "basicRules2024", "reprintedAs", "otherSources", "additionalSources",
        "hasFluff", "hasFluffImages", "soundClip", "_versions",
    ];

    /// <summary>
    /// The entry as 5etools shows it; one that is no copy comes back as it is. A copy whose source
    /// <paramref name="find"/> does not find stops the run, since the data would be incomplete.
    /// </summary>
    /// <param name="entry">A race or subrace, which may have "_copy".</param>
    /// <param name="find">Finds the entry a "_copy" object names.</param>
    public static JsonObject Resolve(JsonObject entry, Func<JsonObject, JsonObject?> find)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(find);

        if (entry["_copy"] is not JsonObject copy)
        {
            return entry;
        }

        var original = find(copy)
            ?? throw new InvalidDataException($"{entry.Str("name")} ({entry.Str("source")}) copies {copy.Str("name")} ({copy.Str("source")}), which is not in the data");
        var result = Resolve(original, find).DeepClone().AsObject();
        var preserved = copy["_preserve"] as JsonObject ?? [];

        foreach (var key in SourceOnlyKeys.Where(k => !preserved.ContainsKey(k)))
        {
            result.Remove(key);
        }

        foreach (var (key, value) in entry.Where(kvp => kvp.Key != "_copy"))
        {
            result[key] = value?.DeepClone();
        }

        foreach (var (key, changes) in copy["_mod"] as JsonObject ?? [])
        {
            foreach (var change in (changes as JsonArray ?? (changes is JsonObject one ? [one.DeepClone()] : [])).OfType<JsonObject>())
            {
                Apply(result, key, change);
            }
        }

        return result;
    }

    // The list changes races use: replace an item by name, append, insert, remove, and replace text
    private static void Apply(JsonObject target, string key, JsonObject change)
    {
        if (target[key] is not JsonArray list)
        {
            list = [];
            target[key] = list;
        }

        var items = Items(change["items"]);

        switch (change.Str("mode"))
        {
            case "replaceArr":
                var index = IndexOf(list, change["replace"].AsString());

                if (index < 0)
                {
                    throw new InvalidDataException($"{target.Str("name")}: no {change["replace"].AsString()} in {key} to replace");
                }

                list.RemoveAt(index);

                foreach (var item in items.AsEnumerable().Reverse())
                {
                    list.Insert(index, item);
                }

                break;
            case "appendArr":
                foreach (var item in items)
                {
                    list.Add(item);
                }

                break;
            case "insertArr":
                var at = change.Int("index") ?? 0;

                foreach (var item in items.AsEnumerable().Reverse())
                {
                    list.Insert(Math.Min(at, list.Count), item);
                }

                break;
            case "removeArr":
                foreach (var name in Names(change["names"]))
                {
                    if (IndexOf(list, name) is >= 0 and var found)
                    {
                        list.RemoveAt(found);
                    }
                }

                break;
            case "replaceTxt":
                var pattern = new Regex(change.Str("replace") ?? string.Empty, (change.Str("flags") ?? string.Empty).Contains('i') ? RegexOptions.IgnoreCase : RegexOptions.None);
                ReplaceText(list, pattern, change.Str("with") ?? string.Empty);
                break;
            default:
                throw new InvalidDataException($"{target.Str("name")}: _mod mode {change.Str("mode")} is not handled");
        }
    }

    private static List<JsonNode?> Items(JsonNode? items) =>
        items is JsonArray many ? many.Select(i => i?.DeepClone()).ToList() : items is null ? [] : [items.DeepClone()];

    private static IEnumerable<string> Names(JsonNode? node) =>
        node is JsonArray many ? many.Select(n => n.AsString()).OfType<string>() : node.AsString() is { } single ? [single] : [];

    private static int IndexOf(JsonArray list, string? name)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] is JsonObject item && item.Str("name") == name)
            {
                return i;
            }
        }

        return -1;
    }

    // Every string in the list and what it holds, the names left alone
    private static void ReplaceText(JsonNode? node, Regex pattern, string replacement)
    {
        switch (node)
        {
            case JsonArray list:
                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i] is JsonValue value && value.TryGetValue<string>(out var text))
                    {
                        list[i] = pattern.Replace(text, replacement);
                    }
                    else
                    {
                        ReplaceText(list[i], pattern, replacement);
                    }
                }

                break;
            case JsonObject item:
                ReplaceText(item["entries"], pattern, replacement);
                ReplaceText(item["items"], pattern, replacement);
                break;
        }
    }
}
