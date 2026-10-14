using System.Text.Json.Nodes;

using CharacterCreatorApp.Domain.DomainModels.Feats;
using CharacterCreatorApp.Domain.ValueObjects;
using CharacterCreatorApp.Infrastructure.Seed.Rows;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Reads feats.json into Feats.json. A feat with a category ("O", "G", "FS", "EB") comes from a
/// book written for the 2024 rules, which introduced them; no 2014 feat has one. A feat in both
/// editions has one name in each and its edition in its id ("alert_2014", "alert_2024").
/// The 2014 Fighting Styles join them as 2014 feats of the Fighting Style category, so a
/// character of either edition picks its style the same way (archery_2014, archery_2024).
/// </summary>
internal sealed class FeatImporter(FiveEToolsSource source, EntryRenderer renderer, ProficiencyIndex proficiencies)
{
    private const string FightingStyle = "Fighting Style";

    /// <summary>The names of 5etools' feat category codes.</summary>
    public static readonly IReadOnlyDictionary<string, string> CategoryNames = new Dictionary<string, string>
    {
        ["O"] = "Origin",
        ["G"] = "General",
        ["FS"] = FightingStyle,
        ["FS:P"] = FightingStyle,
        ["FS:R"] = FightingStyle,
        ["EB"] = "Epic Boon",
        ["D"] = "Dragonmark",
        ["DG"] = "Dark Gift",
    };

    // Blessed Warrior and Druidic Warrior are the paladin's and the ranger's own Fighting Style
    // options, which 5etools says only in words ("When Gaining the Level 2 Paladin...")
    private static readonly Dictionary<string, string> OwnFightingStyleClassIds = new()
    {
        ["FS:P"] = "paladin-2024",
        ["FS:R"] = "ranger-2024",
    };

    // A 2014 style is one of 5etools' optional features, typed by the classes that offer it: the
    // fighter's, the paladin's, the ranger's, and a College of Swords bard's
    private static readonly Dictionary<string, string> FightingStyleClassIds2014 = new()
    {
        ["FS:F"] = "fighter",
        ["FS:P"] = "paladin",
        ["FS:R"] = "ranger",
        ["FS:B"] = "bard",
    };

    private readonly FiveEToolsSource _source = source;
    private readonly EntryRenderer _renderer = renderer;
    private readonly ProficiencyIndex _proficiencies = proficiencies;

    /// <summary>Spells a feat gives that are not in the spell files.</summary>
    public SortedSet<string> UnresolvedSpells { get; } = [];

    /// <summary>Category codes the table above does not name.</summary>
    public SortedSet<string> UnknownCategories { get; } = [];

    /// <summary>Ability increases FeatAbilityIncrease cannot hold, such as two fixed ones; the feat gets none.</summary>
    public SortedSet<string> SkippedAbilityIncreases { get; } = [];

    /// <summary>How many "choose a spell" grants were left out, the app not asking for them.</summary>
    public int SkippedPicks { get; private set; }

    /// <summary>Each feat's id by "name|source", filled by Import: how a background names its feat.</summary>
    public Dictionary<string, string> IdsByKey { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<FeatSeedRow> Import(IReadOnlyDictionary<string, string> spellIdsByKey)
    {
        var feats = _source.Load("feats.json").Objects("feat").Concat(FightingStyles2014()).ToList();

        // By id, not name: the 2024 book spells "Fey Touched" as "Fey-Touched"
        var inBothEditions = feats
            .GroupBy(BaseId)
            .Where(g => g.Select(EditionOf).Distinct().Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();

        var featNames = feats
            .Select(f => f.Str("name")!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(name => name, name => name, StringComparer.OrdinalIgnoreCase);

        var rows = new List<FeatSeedRow>();

        foreach (var feat in feats)
        {
            var row = Row(feat, inBothEditions.Contains(BaseId(feat)), spellIdsByKey, featNames);
            IdsByKey[$"{feat.Str("name")}|{feat.Str("source")}"] = row.Id;
            rows.Add(row);
        }

        return rows
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .ThenBy(r => r.Id, StringComparer.Ordinal)
            .ToList();
    }

    // Only the Player's Handbook's: Tasha's added styles as optional class features, which are left out
    private IEnumerable<JsonObject> FightingStyles2014() =>
        _source.Load("optionalfeatures.json")
            .Objects("optionalfeature")
            .Where(style => style.Str("source") == "PHB" && StyleClassIds(style).Count != 0);

    private static List<string> StyleClassIds(JsonObject feat) =>
        feat.Arr("featureType")
            .Select(type => FightingStyleClassIds2014.GetValueOrDefault(type.AsString() ?? string.Empty))
            .OfType<string>()
            .ToList();

    private static bool IsFightingStyle2014(JsonObject feat) => feat["featureType"] != null;

    private static string BaseId(JsonObject feat) => Slug.Of(feat.Str("name")!, '_');

    private static string EditionOf(JsonObject feat) =>
        feat["category"] != null ? RulesEdition.Edition2024 : RulesEdition.Edition2014;

    private FeatSeedRow Row(
        JsonObject feat,
        bool inBothEditions,
        IReadOnlyDictionary<string, string> spellIdsByKey,
        IReadOnlyDictionary<string, string> featNames)
    {
        var name = feat.Str("name")!;
        var edition = EditionOf(feat);

        var prerequisites = IsFightingStyle2014(feat)
            ? new FeatPrerequisites { ClassIds = StyleClassIds(feat), Features = [FightingStyle] }
            : CategoryCode(feat) is { } code && OwnFightingStyleClassIds.TryGetValue(code, out var classId)
                ? new FeatPrerequisites { ClassIds = [classId], Features = [FightingStyle] }
                : FeatPrerequisiteParser.Parse(feat["prerequisite"], edition, featNames);

        var id = BaseId(feat) + (inBothEditions ? $"_{edition}" : string.Empty);

        return new FeatSeedRow
        {
            Id = id,
            Name = name,
            Description = SrdText.Describe(id, feat, () => _renderer.Render(feat["entries"])),
            Edition = edition,
            Source = SourceBooks.Of(feat, edition),
            Category = IsFightingStyle2014(feat) ? FightingStyle : CategoryOf(feat),
            Prerequisites = prerequisites,
            IsRepeatable = feat.Bool("repeatable"),
            AbilityIncrease = AbilityIncrease(feat),
            GrantedProficiencyIds = GrantedProficiencies(feat),
            GrantedLanguageIds = GrantedLanguages(feat),
            GrantedSpellIds = GrantedSpells(feat, spellIdsByKey),
            Choice = Choice(feat),
            SpellChoice = AdditionalSpells.Choice(feat.Objects("additionalSpells").ToList(), edition),
        };
    }

    // "O", or ["FS", "FS:P"] for a feat in more than one
    private static string? CategoryCode(JsonObject feat) =>
        feat.Str("category") ?? feat.Arr("category").FirstOrDefault().AsString();

    private string? CategoryOf(JsonObject feat)
    {
        var code = CategoryCode(feat);

        if (code == null)
        {
            return null;
        }

        if (CategoryNames.TryGetValue(code, out var category))
        {
            return category;
        }

        UnknownCategories.Add(code);
        return code;
    }

    /// <summary>
    /// What the feat raises. {"cha": 1} is fixed; {"choose": {"from": ["str", "dex"], "amount": 1}}
    /// is one of several; "max" is the cap, 30 for an Epic Boon. Several alternatives are one
    /// choice only when they range over the same abilities with the same points and one of them
    /// may put all the points on one ability, as the 2024 Ability Score Improvement's +2 to one or
    /// +1 to two do: FeatAbilityIncrease always allows that.
    /// </summary>
    private FeatAbilityIncrease? AbilityIncrease(JsonObject feat)
    {
        var alternatives = feat.Objects("ability").ToList();

        if (alternatives.Count == 0)
        {
            return null;
        }

        var choices = alternatives.Select(AbilityIncreaseAlternative).OfType<AbilityIncreaseChoice>().ToList();

        if (choices.Count != alternatives.Count
            || choices.Select(c => c.Points).Distinct().Count() > 1
            || choices.Select(c => string.Join(",", c.Options.Order(StringComparer.Ordinal))).Distinct().Count() > 1
            || !choices.Exists(c => c.MaxPerAbility == c.Points))
        {
            SkippedAbilityIncreases.Add($"{feat.Str("name")} ({feat.Str("source")}): {feat["ability"]!.ToJsonString()}");
            return null;
        }

        return new FeatAbilityIncrease
        {
            Options = choices[0].Options.ToList(),
            Points = choices[0].Points,
            Maximum = alternatives.Select(a => a.Int("max")).Max() ?? 20,
        };
    }

    // One alternative: a single fixed increase or a single choice; anything more is not one choice
    private static AbilityIncreaseChoice? AbilityIncreaseAlternative(JsonObject alternative)
    {
        var increases = alternative.Where(kvp => kvp.Key is not ("max" or "hidden")).ToList();

        if (increases.Count != 1)
        {
            return null;
        }

        var (key, value) = increases[0];

        if (key == "choose")
        {
            var options = value.Arr("from").Select(a => AbilityScore.TryNormalize(a.AsString() ?? string.Empty, out var id) ? id : null).ToList();
            var amount = value.Int("amount") ?? 1;

            return options.Contains(null) ? null : new AbilityIncreaseChoice(options.OfType<string>().ToList(), amount * (value.Int("count") ?? 1), amount);
        }

        return AbilityScore.TryNormalize(key, out var abilityId) && value is JsonValue fixedAmount && fixedAmount.TryGetValue<int>(out var points)
            ? new AbilityIncreaseChoice([abilityId], points, points)
            : null;
    }

    private static readonly string[] ProficiencyKeys = ["skillProficiencies", "toolProficiencies", "armorProficiencies", "weaponProficiencies"];

    private static readonly string[] SpellKinds = ["prepared", "known", "innate"];

    // Only named ones: {"athletics": true}. "any" and "choose" are picks, which Choice covers.
    private List<string> GrantedProficiencies(JsonObject feat) =>
        ProficiencyKeys
            .SelectMany(feat.Objects)
            .SelectMany(entry => entry.Where(kvp => kvp.Value is JsonValue value && value.TryGetValue<bool>(out var named) && named))
            .Select(kvp => _proficiencies.IdOf(kvp.Key))
            .OfType<string>()
            .Distinct()
            .ToList();

    // {"sylvan": true}; "any" and "anyStandard" are picks
    private static List<string> GrantedLanguages(JsonObject feat) =>
        feat.Objects("languageProficiencies")
            .SelectMany(entry => entry.Where(kvp => kvp.Value is JsonValue value && value.TryGetValue<bool>(out var named) && named))
            .Select(kvp => kvp.Key)
            .Distinct()
            .ToList();

    /// <summary>
    /// The spells a feat always gives, such as Fey Touched's Misty Step. A block with a name is one
    /// option of a choice (Magic Initiate's "Cleric Spells"), which SpellChoice holds.
    /// </summary>
    private List<string> GrantedSpells(JsonObject feat, IReadOnlyDictionary<string, string> spellIdsByKey)
    {
        var spellIds = new List<string>();

        foreach (var block in feat.Objects("additionalSpells").Where(b => b.Str("name") == null))
        {
            foreach (var kind in SpellKinds)
            {
                foreach (var reference in AdditionalSpells.References(block[kind], () => SkippedPicks++))
                {
                    if (AdditionalSpells.IsFilter(reference))
                    {
                        continue;
                    }

                    var (name, spellSource) = AdditionalSpells.Parse(reference);

                    if (spellIdsByKey.TryGetValue(SpellImporter.Key(name, spellSource), out var spellId))
                    {
                        spellIds.Add(spellId);
                    }
                    else
                    {
                        UnresolvedSpells.Add($"{feat.Str("name")}: {reference}");
                    }
                }
            }
        }

        return spellIds.Distinct().ToList();
    }

    private FeatChoice? Choice(JsonObject feat)
    {
        // Skilled: any three skills or tools
        var skillOrTool = feat.Objects("skillToolLanguageProficiencies")
            .SelectMany(entry => entry.Arr("choose").OfType<JsonObject>())
            .FirstOrDefault();

        if (skillOrTool != null)
        {
            return new FeatChoice
            {
                Type = "skills_tools",
                Count = skillOrTool.Int("count") ?? 1,
                Options = _proficiencies.SkillsAndTools.Select(p => p.Id).Order(StringComparer.Ordinal).ToList()
            };
        }

        // Skill Expert: any one skill, or one from a list
        foreach (var entry in feat.Objects("skillProficiencies"))
        {
            if (entry.Int("any") is { } any)
            {
                return new FeatChoice { Type = "skills", Count = any, Options = _proficiencies.Skills.Select(p => p.Id).Order(StringComparer.Ordinal).ToList() };
            }

            if (entry["choose"] is JsonObject choose)
            {
                return new FeatChoice
                {
                    Type = "skills",
                    Count = choose.Int("count") ?? 1,
                    Options = choose.Arr("from").Select(s => _proficiencies.IdOf(s.AsString() ?? string.Empty)).OfType<string>().ToList()
                };
            }
        }

        // Resilient: the ability whose saving throw it adds, which is the one it raises
        var save = feat.Objects("savingThrowProficiencies").Select(entry => entry["choose"]).OfType<JsonObject>().FirstOrDefault();

        if (save != null)
        {
            return new FeatChoice
            {
                Type = FeatChoice.SavingThrow,
                Count = 1,
                Options = save.Arr("from").Select(a => AbilityScore.Normalize(a.AsString()!)).ToList()
            };
        }

        return null;
    }
}
