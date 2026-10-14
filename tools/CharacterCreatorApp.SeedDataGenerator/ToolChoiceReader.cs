using System.Text.Json.Nodes;

using CharacterCreatorApp.Domain.DomainModels.Proficiencies;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Reads the tool picks in 5etools' "toolProficiencies": a kind of tool ("anyGamingSet": 1, or
/// "gaming set": true for one), or a "choose" list that may mix kinds and tools ("musical
/// instrument", "navigator's tools"). A kind becomes every proficiency of it.
/// </summary>
internal static class ToolChoiceReader
{
    /// <summary>The pick one key and value stand for, or null when they are no pick the app knows.</summary>
    public static ToolChoice? Read(string key, JsonNode? value, ProficiencyIndex proficiencies)
    {
        if (key == "choose")
        {
            return new ToolChoice
            {
                Count = value.Int("count") ?? 1,
                Options = value.Arr("from")
                    .Select(from => from.AsString())
                    .OfType<string>()
                    .SelectMany(name => proficiencies.IdsOfKind(name) ?? (proficiencies.IdOf(name) is { } id ? [id] : []))
                    .Distinct()
                    .Order(StringComparer.Ordinal)
                    .ToList(),
            };
        }

        if (proficiencies.IdsOfKind(key) is not { } options)
        {
            return null;
        }

        return new ToolChoice
        {
            Count = value is JsonValue count && count.TryGetValue<int>(out var picks) ? picks : 1,
            Options = options,
        };
    }

    /// <summary>
    /// Several entries are alternatives: the monk's "one artisan's tools or one instrument" is
    /// {"anyArtisansTool": 1}, {"anyMusicalInstrument": 1}. Read as one pick from them all; null
    /// when an entry is more than one such pick (a House Agent's set per house).
    /// </summary>
    public static ToolChoice? ReadAlternatives(IReadOnlyList<JsonObject> entries, ProficiencyIndex proficiencies)
    {
        var choices = entries
            .Select(entry => entry.Count == 1 ? Read(entry.First().Key, entry.First().Value, proficiencies) : null)
            .ToList();

        if (choices.Exists(c => c is null) || choices.Select(c => c!.Count).Distinct().Count() != 1)
        {
            return null;
        }

        return new ToolChoice
        {
            Count = choices[0]!.Count,
            Options = choices.SelectMany(c => c!.Options).Distinct().Order(StringComparer.Ordinal).ToList(),
        };
    }
}
