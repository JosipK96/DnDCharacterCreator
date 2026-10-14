using System.Globalization;
using System.Text.Json.Nodes;

using CharacterCreatorApp.Domain.DomainModels.Feats;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Reads a 5etools feat's prerequisite list into <see cref="FeatPrerequisites"/>. 5etools lists
/// alternatives, each an object of requirements that all have to hold. Alternatives that ask for
/// the same kinds of thing only differ in the ability, race or class they accept ("Strength 13"
/// or "Dexterity 13", both at level 4), so they fold into one set whose lists hold the
/// alternatives. Alternatives that ask for different kinds of thing do not fold, see
/// <see cref="DescribeOnly"/>.
/// </summary>
internal static class FeatPrerequisiteParser
{
    // What TCE and the 2024 books call "Spellcasting or Pact Magic feature", and a few books
    // "Spellcasting feature": class and subclass features of these names
    private static readonly string[] SpellcastingOrPactMagic = ["Spellcasting", "Pact Magic"];
    private static readonly string[] SpellcastingOnly = ["Spellcasting"];

    // Words that lose their capital inside a sentence; a class or race name keeps it
    private static readonly string[] LowerCaseStarts = ["The ", "A ", "An ", "Level ", "Proficiency ", "No "];

    /// <param name="prerequisites">The feat's "prerequisite" array, or null.</param>
    /// <param name="edition">
    /// The feat's edition, which decides the ids a class or race prerequisite names: a 2024 feat
    /// asks for "fighter-2024" and "elf_2024".
    /// </param>
    /// <param name="featNames">
    /// Every feat's name, looked up case-insensitively: a prerequisite names a feat in lower case
    /// ("purple dragon rook|frhof").
    /// </param>
    public static FeatPrerequisites Parse(JsonNode? prerequisites, string edition, IReadOnlyDictionary<string, string> featNames)
    {
        var alternatives = (prerequisites as JsonArray ?? []).OfType<JsonObject>().ToList();

        if (alternatives.Select(Shape).Distinct().Count() > 1)
        {
            return DescribeOnly(alternatives, edition, featNames);
        }

        var result = new FeatPrerequisites();

        foreach (var alternative in alternatives)
        {
            Read(alternative, edition, featNames, result);
        }

        return result;
    }

    private static void Read(JsonObject alternative, string edition, IReadOnlyDictionary<string, string> featNames, FeatPrerequisites result)
    {
        foreach (var (name, value) in alternative)
        {
            switch (name)
            {
                case "level":
                    ReadLevel(value, edition, result);
                    break;
                case "ability":
                    ReadAbilities(value, result);
                    break;
                case "race":
                    ReadRaces(value, edition, result);
                    break;
                case "feature":
                    AddFeatures(result, Names(value));
                    break;
                case "spellcasting2020":
                    AddFeatures(result, SpellcastingOrPactMagic);
                    break;
                case "spellcastingFeature":
                    AddFeatures(result, SpellcastingOnly);
                    break;
                default:
                    AddOnce(result.Unchecked, FiveEToolsMarkup.ToPlainText(Describe(name, value, featNames)));
                    break;
            }
        }
    }

    // The kinds of thing an alternative asks for; a level with a class is a class
    private static string Shape(JsonObject alternative) =>
        string.Join('+', alternative
            .Select(kvp => kvp.Key == "level" && kvp.Value is JsonObject level && level["class"] != null ? "class" : kvp.Key)
            .Order(StringComparer.Ordinal));

    /// <summary>
    /// Squire of Solamnia takes a fighter, a paladin or a Knight of Solamnia; folded, it would ask
    /// the knight to be a fighter or paladin as well. So when the alternatives ask for different
    /// kinds of thing (six feats in the data), the prerequisite is shown in words and not checked:
    /// "Fighter or Paladin or the Knight of Solamnia background", with what every alternative asks
    /// for ("A Dragonlance campaign") on a line of its own.
    /// </summary>
    private static FeatPrerequisites DescribeOnly(List<JsonObject> alternatives, string edition, IReadOnlyDictionary<string, string> featNames)
    {
        var described = alternatives.Select(alternative =>
        {
            var one = new FeatPrerequisites();
            Read(alternative, edition, featNames, one);
            return one.Describe().ToList();
        }).ToList();

        var common = described[0].Where(line => described.TrueForAll(d => d.Contains(line))).ToList();
        var parts = described.Select(d => d.Except(common).ToList()).ToList();
        var separator = parts.Exists(p => p.Count > 1) ? ", or " : " or ";

        var sentence = string.Join(separator, parts.Select((lines, i) =>
            string.Join(" and ", lines.Select((line, j) => i == 0 && j == 0 ? line : MidSentence(line)))));

        return new FeatPrerequisites { Unchecked = [sentence, .. common] };
    }

    // A number, or {"level": 4, "class": {"name": "Fighter"}} for a feat only one class can take
    private static void ReadLevel(JsonNode? value, string edition, FeatPrerequisites result)
    {
        var level = value is JsonValue number && number.TryGetValue<int>(out var plain) ? plain : value.Int("level") ?? 1;
        result.MinimumLevel = result.MinimumLevel is { } existing ? Math.Min(existing, level) : level;

        if ((value as JsonObject)?["class"].Str("name") is { } className)
        {
            AddOnce(result.ClassIds, Slug.Of(className) + (edition == RulesEdition.Edition2024 ? "-2024" : string.Empty));
        }
    }

    // [{"str": 13}, {"dex": 13}] is Strength or Dexterity 13
    private static void ReadAbilities(JsonNode? value, FeatPrerequisites result)
    {
        foreach (var option in (value as JsonArray ?? []).OfType<JsonObject>())
        {
            foreach (var (ability, score) in option)
            {
                if (AbilityScore.TryNormalize(ability, out var abilityId)
                    && score is JsonValue scoreValue && scoreValue.TryGetValue<int>(out var minimum)
                    && result.AbilityMinimums.TrueForAll(m => m.AbilityId != abilityId))
                {
                    result.AbilityMinimums.Add(new AbilityMinimum { AbilityId = abilityId, Score = minimum });
                }
            }
        }
    }

    // Race ids are the names with underscores, "half-elf" is "half_elf". One naming a subrace is
    // that subrace, whose id the race importer makes from both names: {"name": "elf", "subrace":
    // "wood"} is "wood_elf". "small race" is any race of that size. A race the data does not have
    // ("Vampire (Ixalan)", MTF's deep gnome) matches nothing, which is what it should do.
    private static void ReadRaces(JsonNode? value, string edition, FeatPrerequisites result)
    {
        var suffix = edition == RulesEdition.Edition2024 ? "_2024" : string.Empty;

        foreach (var race in (value as JsonArray ?? []).OfType<JsonObject>())
        {
            if (race.Str("name") is not { } raceName)
            {
                continue;
            }

            if (race.Str("subrace") is { } subraceName)
            {
                AddOnce(result.SubraceIds, Slug.Of($"{subraceName} {raceName}", '_') + suffix);
            }
            else if (SizeOf(raceName) is { } size)
            {
                AddOnce(result.Sizes, size);
            }
            else
            {
                AddOnce(result.RaceIds, Slug.Of(raceName, '_') + suffix);
            }
        }
    }

    // "small race" is "Small", spelt as the races' sizes are
    private static string? SizeOf(string raceName)
    {
        var words = raceName.Split(' ');

        return words.Length == 2 && words[1].Equals("race", StringComparison.OrdinalIgnoreCase)
            && RaceImporter.Sizes.Values.FirstOrDefault(s => s.Equals(words[0], StringComparison.OrdinalIgnoreCase)) is { } size
            ? size
            : null;
    }

    private static void AddFeatures(FeatPrerequisites result, IEnumerable<string> features)
    {
        foreach (var feature in features)
        {
            AddOnce(result.Features, feature);
        }
    }

    private static string Describe(string name, JsonNode? value, IReadOnlyDictionary<string, string> featNames) => name switch
    {
        "feat" => $"The {string.Join(" or ", FeatNames(value, featNames))} feat",
        "featCategory" => $"{WithArticle(Categories(value))} feat",
        "exclusiveFeatCategory" => $"No other {Categories(value)} feat",
        "background" => $"The {string.Join(" or ", Names(value))} background",
        "campaign" => $"{WithArticle(string.Join(" or ", Names(value)))} campaign",
        "proficiency" => $"Proficiency with {Proficiencies(value)}",
        "spellcasting" => "The ability to cast at least one spell",
        "spellcastingPrepared" => "The Spellcasting feature of a class that prepares spells",
        "other" => value.AsString() ?? name,
        "otherSummary" => value.Str("entry") ?? name,
        _ => name,
    };

    // Strings are 5etools references, "name|source|display text"; objects carry a name
    private static IEnumerable<string> Names(JsonNode? value) =>
        (value as JsonArray ?? [])
            .Select(item => item.AsString() is { } reference ? DisplayPart(reference) : item.Str("name"))
            .OfType<string>();

    private static string DisplayPart(string reference)
    {
        var parts = reference.Split('|');
        return parts.Length >= 3 && parts[2].Length > 0 ? parts[2] : parts[0];
    }

    // "initiate of high sorcery|dsotdq|initiate of high sorcery (nuitari)" is the feat by its own
    // name, "Initiate of High Sorcery", then the choice made in it, " (Nuitari)"
    private static IEnumerable<string> FeatNames(JsonNode? value, IReadOnlyDictionary<string, string> featNames) =>
        (value as JsonArray ?? [])
            .Select(item => item.AsString())
            .OfType<string>()
            .Select(reference =>
            {
                var parts = reference.Split('|');
                var name = featNames.GetValueOrDefault(parts[0], parts[0]);
                var choice = parts.Length >= 3 && parts[2].StartsWith(parts[0], StringComparison.OrdinalIgnoreCase)
                    ? parts[2][parts[0].Length..]
                    : string.Empty;

                return name + CultureInfo.InvariantCulture.TextInfo.ToTitleCase(choice);
            });

    // ["D"] is "Dragonmark"
    private static string Categories(JsonNode? value) =>
        string.Join(" or ", (value as JsonArray ?? []).Select(code => code.AsString()).OfType<string>()
            .Select(code => FeatImporter.CategoryNames.GetValueOrDefault(code, code)));

    // [{"armor": "medium"}] is medium armor, [{"weapon": "martial"}] martial weapons
    private static string Proficiencies(JsonNode? value) =>
        string.Join(" or ", (value as JsonArray ?? []).OfType<JsonObject>().SelectMany(item => item).Select(kind => kind.Key switch
        {
            "armor" when kind.Value.AsString() == "shield" => "shields",
            "armor" => $"{kind.Value.AsString()} armor",
            "weapon" or "weaponGroup" => $"{kind.Value.AsString()} weapons",
            _ => kind.Value?.ToJsonString() ?? kind.Key,
        }));

    private static string WithArticle(string noun) =>
        ("AEIOU".Contains(noun.FirstOrDefault(), StringComparison.Ordinal) ? "An " : "A ") + noun;

    private static string MidSentence(string line) =>
        LowerCaseStarts.Any(start => line.StartsWith(start, StringComparison.Ordinal))
            ? char.ToLowerInvariant(line[0]) + line[1..]
            : line;

    private static void AddOnce(List<string> list, string value)
    {
        if (!list.Contains(value))
        {
            list.Add(value);
        }
    }
}
