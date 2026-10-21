using System.Text.Json.Nodes;

using CharacterCreatorApp.Domain.DomainModels.Proficiencies;
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

    private const string AnyFromCategory = "anyFromCategory";

    private static readonly string[] ProficiencyKeys = ["skillProficiencies", "toolProficiencies"];

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

            var (featChoiceCategory, featAlternativeCategory) = FeatCategories(background);

            backgrounds.Add(new BackgroundSeedRow
            {
                Id = id,
                Name = background.Str("name")!,
                Edition = edition,
                Source = SourceBooks.Of(background, edition),
                ProficiencyIds = Proficiencies(id, background),
                ToolChoices = ToolChoices(id, background),
                Languages = edition == RulesEdition.Edition2024 ? Languages2024 : Languages(background),
                AbilityScoreOptions = AbilityScoreOptions(background),
                FeatId = FeatId(id, background, featIdsByKey),
                FeatChoiceCategory = featChoiceCategory,
                FeatAlternativeCategory = featAlternativeCategory,
                FeatSpellListClassId = FeatSpellListClassId(background, edition),
                FeatureId = feature?.Id,
            });
        }

        return new BackgroundImport(
            backgrounds.OrderBy(b => b.Edition, StringComparer.Ordinal).ThenBy(b => b.Name, StringComparer.Ordinal).ToList(),
            features.OrderBy(f => f.Id, StringComparer.Ordinal).ToList());
    }

    // Named skills and tools; a skill pick ("choose") is noted and left out, a tool pick is ToolChoices'
    private List<string> Proficiencies(string backgroundId, JsonObject background)
    {
        var ids = new List<string>();

        foreach (var key in ProficiencyKeys)
        {
            foreach (var (name, value) in background.Objects(key).SelectMany(entry => entry))
            {
                if (IsNamed(name, value))
                {
                    if (_proficiencies.IdOf(name) is { } proficiencyId && !ids.Contains(proficiencyId))
                    {
                        ids.Add(proficiencyId);
                    }
                }
                else if (key == "skillProficiencies")
                {
                    SkippedChoices.Add($"{backgroundId}: {name}");
                }
            }
        }

        return ids;
    }

    // "anyGamingSet": 1, "gaming set": true, or a "choose" list. A House Agent's tools are one
    // set per house, which the app does not ask for.
    private List<ToolChoice> ToolChoices(string backgroundId, JsonObject background)
    {
        var entries = background.Objects("toolProficiencies").ToList();

        if (entries.Count > 1)
        {
            SkippedChoices.Add($"{backgroundId}: one of {entries.Count} sets of tools");
            return [];
        }

        var choices = new List<ToolChoice>();

        foreach (var (name, value) in entries.SelectMany(entry => entry).Where(kvp => !IsNamed(kvp.Key, kvp.Value)))
        {
            if (ToolChoiceReader.Read(name, value, _proficiencies) is { } choice)
            {
                choices.Add(choice);
                continue;
            }

            SkippedChoices.Add($"{backgroundId}: {name}");
        }

        return choices;
    }

    // "thieves' tools": true names one; "gaming set": true is a kind, one of the player's choice
    private static bool IsNamed(string name, JsonNode? value) =>
        value is JsonValue named && named.TryGetValue<bool>(out var isNamed) && isNamed && !ProficiencyIndex.IsKind(name);

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

    // {"magic initiate; cleric|xphb": true}: Magic Initiate, with its cleric list (FeatSpellListClassId)
    private string? FeatId(string backgroundId, JsonObject background, IReadOnlyDictionary<string, string> featIdsByKey)
    {
        var reference = FeatReference(background);

        // Mist Wanderer and Spirit Medium let the player pick the feat (FeatCategories)
        if (reference is null or AnyFromCategory)
        {
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

    /// <summary>
    /// The categories a background's feat may be picked from. {"anyFromCategory": {"category": ["DG"]}}
    /// first is any Dark Gift, picked in place of a feat of its own (a Mist Wanderer's). Each entry
    /// after the first is an alternative to it, so one of those is a Dark Gift the player may take
    /// instead of the background's own feat (a Haunted One's, instead of Survivor).
    /// </summary>
    internal static (string? ChoiceCategory, string? AlternativeCategory) FeatCategories(JsonObject background)
    {
        var entries = background.Objects("feats").ToList();

        return (
            CategoryOf(entries.FirstOrDefault()),
            entries.Skip(1).Select(CategoryOf).FirstOrDefault(category => category != null));
    }

    private static string? CategoryOf(JsonObject? entry)
    {
        var code = entry?[AnyFromCategory].Arr("category").FirstOrDefault().AsString();

        return code == null ? null : FeatImporter.CategoryNames.GetValueOrDefault(code, code);
    }

    // "magic initiate; cleric|xphb": the class whose list the feat's spells come from
    private static string? FeatSpellListClassId(JsonObject background, string edition)
    {
        var option = FeatReference(background)?.Split('|')[0].Split(';').ElementAtOrDefault(1)?.Trim();

        return option == null ? null : edition == RulesEdition.Edition2024 ? $"{option}-2024" : option;
    }

    private static string? FeatReference(JsonObject background) =>
        background.Objects("feats").SelectMany(entry => entry).Select(kvp => kvp.Key).FirstOrDefault();

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
