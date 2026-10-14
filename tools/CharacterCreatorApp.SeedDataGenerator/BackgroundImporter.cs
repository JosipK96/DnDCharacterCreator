using System.Text.Json.Nodes;

using CharacterCreatorApp.Domain.ValueObjects;
using CharacterCreatorApp.Infrastructure.Seed.Rows;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Reads backgrounds.json into Backgrounds.json and BackgroundFeatures.json. A background marked
/// "edition": "one" follows the 2024 rules and gets a "_2024" id; the others keep their 2014 ids
/// ("folk_hero"). The copies 5etools makes of a background for another book (the Baldur's Gate
/// ones, the PHB variants such as the Knight) are left out.
/// </summary>
internal sealed class BackgroundImporter(FiveEToolsSource source, EntryRenderer renderer, ProficiencyIndex proficiencies)
{
    private const string FeaturePrefix = "Feature: ";

    private static readonly string[] ProficiencyKeys = ["skillProficiencies", "toolProficiencies"];

    // Kinds of tool rather than tools: one set or instrument of the player's choice
    private static readonly HashSet<string> ToolCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "gaming set", "musical instrument", "artisan's tools",
    };

    // The 2024 rules give every character Common and two standard languages when it is made;
    // the app carries them on the 2024 background, where the 2014 rules put a background's own
    private static readonly List<string> Languages2024 = ["common", "any", "any"];

    private readonly FiveEToolsSource _source = source;
    private readonly EntryRenderer _renderer = renderer;
    private readonly ProficiencyIndex _proficiencies = proficiencies;

    /// <summary>Choices a background offers that the app does not ask for yet, such as a gaming set.</summary>
    public SortedSet<string> SkippedChoices { get; } = [];

    /// <summary>Feats a background names that are not in the feat file.</summary>
    public SortedSet<string> UnresolvedFeats { get; } = [];

    public BackgroundImport Import(IReadOnlyDictionary<string, string> featIdsByKey)
    {
        var backgrounds = new List<BackgroundSeedRow>();
        var features = new List<FeatureSeedRow>();

        foreach (var background in _source.Load("backgrounds.json").Objects("background").Where(b => b["_copy"] == null))
        {
            var edition = background.Str("edition") == "one" ? RulesEdition.Edition2024 : RulesEdition.Edition2014;
            var id = Slug.Of(background.Str("name")!, '_') + (edition == RulesEdition.Edition2024 ? "_2024" : string.Empty);
            var feature = Feature(id, background);

            if (feature != null)
            {
                features.Add(feature);
            }

            backgrounds.Add(new BackgroundSeedRow
            {
                Id = id,
                Name = background.Str("name")!,
                Edition = edition,
                ProficiencyIds = Proficiencies(id, background),
                Languages = edition == RulesEdition.Edition2024 ? Languages2024 : Languages(background),
                AbilityScoreOptions = AbilityScoreOptions(background),
                FeatId = FeatId(id, background, featIdsByKey),
                FeatureId = feature?.Id,
            });
        }

        return new BackgroundImport(
            backgrounds.OrderBy(b => b.Edition, StringComparer.Ordinal).ThenBy(b => b.Name, StringComparer.Ordinal).ToList(),
            features.OrderBy(f => f.Id, StringComparer.Ordinal).ToList());
    }

    // Named skills and tools; "choose" and "anyGamingSet" are picks, noted and left out
    private List<string> Proficiencies(string backgroundId, JsonObject background)
    {
        var ids = new List<string>();

        foreach (var (name, value) in ProficiencyKeys.SelectMany(background.Objects).SelectMany(entry => entry))
        {
            if (value is JsonValue named && named.TryGetValue<bool>(out var isNamed) && isNamed && !ToolCategories.Contains(name))
            {
                if (_proficiencies.IdOf(name) is { } proficiencyId && !ids.Contains(proficiencyId))
                {
                    ids.Add(proficiencyId);
                }

                continue;
            }

            SkippedChoices.Add($"{backgroundId}: {name}");
        }

        return ids;
    }

    // {"dwarvish": true, "anyStandard": 2}: the names, and "any" for each pick
    private static List<string> Languages(JsonObject background)
    {
        var languages = new List<string>();

        foreach (var (key, value) in background.Objects("languageProficiencies").SelectMany(entry => entry))
        {
            if (key.StartsWith("any", StringComparison.Ordinal) && value is JsonValue count && count.TryGetValue<int>(out var picks))
            {
                languages.AddRange(Enumerable.Repeat("any", picks));
            }
            else if (key != "other" && value is JsonValue named && named.TryGetValue<bool>(out var isNamed) && isNamed)
            {
                languages.Add(key);
            }
        }

        return languages;
    }

    // [{"choose": {"weighted": {"from": ["int", "wis", "cha"], "weights": [2, 1]}}}, ...]
    private static List<string> AbilityScoreOptions(JsonObject background) =>
        background.Objects("ability")
            .Select(entry => entry["choose"]?["weighted"]?["from"] as JsonArray)
            .OfType<JsonArray>()
            .Take(1)
            .SelectMany(from => from.Select(a => AbilityScore.Normalize(a.AsString()!)))
            .ToList();

    // {"magic initiate; cleric|xphb": true}: Magic Initiate, whose cleric list the player would pick
    private string? FeatId(string backgroundId, JsonObject background, IReadOnlyDictionary<string, string> featIdsByKey)
    {
        var reference = background.Objects("feats").SelectMany(entry => entry).Select(kvp => kvp.Key).FirstOrDefault();

        if (reference == null)
        {
            return null;
        }

        // Mist Wanderer and Spirit Medium let the player pick an origin feat
        if (reference == "anyFromCategory")
        {
            SkippedChoices.Add($"{backgroundId}: a feat of the player's choice");
            return null;
        }

        var parts = reference.Split('|');
        var name = parts[0].Split(';')[0].Trim();
        var book = parts.Length > 1 && parts[1].Length > 0 ? parts[1] : "PHB";

        if (featIdsByKey.TryGetValue($"{name}|{book}", out var featId))
        {
            return featId;
        }

        UnresolvedFeats.Add($"{backgroundId}: {reference}");
        return null;
    }

    // The entry 5etools marks as the background's feature, "Feature: Shelter of the Faithful"
    private FeatureSeedRow? Feature(string backgroundId, JsonObject background)
    {
        var entry = background.Arr("entries").OfType<JsonObject>().FirstOrDefault(e => e["data"].Bool("isFeature"));

        if (entry?.Str("name") is not { } heading)
        {
            return null;
        }

        var name = heading.StartsWith(FeaturePrefix, StringComparison.Ordinal) ? heading[FeaturePrefix.Length..] : heading;

        var id = $"{backgroundId}_{Slug.Of(name, '_')}";

        return new FeatureSeedRow
        {
            Id = id,
            Name = name,
            Description = SrdText.Describe(id, background, () => _renderer.Render(entry["entries"])),
            Level = 0,
            Type = "background",
        };
    }
}

internal sealed record BackgroundImport(List<BackgroundSeedRow> Backgrounds, List<FeatureSeedRow> Features);
