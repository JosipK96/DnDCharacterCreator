using System.Globalization;
using System.Text.Json.Nodes;

using CharacterCreatorApp.Domain.ValueObjects;
using CharacterCreatorApp.Infrastructure.Seed.Rows;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Reads races.json into Races.json: the 2014 Player's Handbook races under their own ids
/// ("dwarf", "hill_dwarf") and the 2024 species under "_2024" ones. A 2014 subrace is an entry
/// of 5etools' subrace list. One without a name is the race as the book prints it: part of the
/// race itself (the half-elf's), or, beside a named one, the "Standard" choice next to it (the
/// human's +1 to everything, which the variant human's picks replace). A 2024 species' lineages
/// are versions of it that swap one trait for their own, and the dragonborn's ten ancestries a
/// template filled in per dragon.
/// </summary>
internal sealed class RaceImporter(FiveEToolsSource source, EntryRenderer renderer, ProficiencyIndex proficiencies)
{
    // 5etools' size codes; a feat's "small race" prerequisite is read with these too
    internal static readonly Dictionary<string, string> Sizes = new()
    {
        ["T"] = "Tiny",
        ["S"] = "Small",
        ["M"] = "Medium",
        ["L"] = "Large",
    };

    // The books whose races the app offers, and the rules each was written for
    private static readonly Dictionary<string, string> Books = new()
    {
        ["PHB"] = RulesEdition.Edition2014,
        ["XPHB"] = RulesEdition.Edition2024,
    };

    // Entries the app shows another way, as the sheet's size, speed and languages
    private static readonly HashSet<string> NotTraits = ["Age", "Alignment", "Size", "Speed", "Languages", "Ability Score Increase", "Creature Type"];

    private static readonly string[] SpellKinds = ["known", "innate", "prepared"];
    private static readonly string[] ItemProficiencyKeys = ["weaponProficiencies", "armorProficiencies", "toolProficiencies"];

    private readonly FiveEToolsSource _source = source;
    private readonly EntryRenderer _renderer = renderer;
    private readonly ProficiencyIndex _proficiencies = proficiencies;

    /// <summary>Choices a race offers that the app does not ask for yet, such as a dwarf's tools.</summary>
    public SortedSet<string> SkippedChoices { get; } = [];

    /// <summary>Spells a race gives that are not in the spell files.</summary>
    public SortedSet<string> UnresolvedSpells { get; } = [];

    /// <summary>How many "choose a spell" grants were left out, such as a high elf's cantrip.</summary>
    public int SkippedPicks { get; private set; }

    public List<RaceSeedRow> Import(IReadOnlyDictionary<string, string> spellIdsByKey)
    {
        var file = _source.Load("races.json");
        var subraces = file.Objects("subrace").ToList();

        return file.Objects("race")
            .Where(race => Books.ContainsKey(race.Str("source")!))
            .Select(race => Race(race, subraces, spellIdsByKey))
            .OrderBy(r => r.Edition, StringComparer.Ordinal)
            .ThenBy(r => r.Name, StringComparer.Ordinal)
            .ToList();
    }

    private RaceSeedRow Race(JsonObject race, List<JsonObject> allSubraces, IReadOnlyDictionary<string, string> spellIdsByKey)
    {
        var name = race.Str("name")!;
        var book = race.Str("source")!;
        var edition = Books[book];
        var id = Slug.Of(name, '_') + (edition == RulesEdition.Edition2024 ? "_2024" : string.Empty);

        // Only the book's own subraces: the Monsters of the Multiverse ones and the rest belong
        // to books the app does not offer
        var subraces = allSubraces
            .Where(s => s.Str("raceName") == name && s.Str("raceSource") == book && s.Str("source") == book)
            .ToList();

        // A subrace without a name is the race as the book prints it. Alone it is part of the
        // race; beside a named one it is a choice of its own, since the named one replaces it
        var named = subraces.Where(s => !string.IsNullOrEmpty(s.Str("name"))).ToList();
        var nameless = subraces.Where(s => string.IsNullOrEmpty(s.Str("name"))).ToList();
        var parts = new List<JsonObject> { race };

        if (named.Count == 0)
        {
            parts.AddRange(nameless);
        }

        // A trait every version swaps out ("Elven Lineage") leaves the species. One only some swap
        // stays: the drow's longer Darkvision replaces the one high and wood elves keep.
        var versions = Versions(race).ToList();
        var replacedTraits = versions.Count == 0
            ? []
            : versions.Skip(1).Aggregate(versions[0].ReplacedTraits.ToHashSet(), (common, v) =>
            {
                common.IntersectWith(v.ReplacedTraits);
                return common;
            });
        var speed = Speed(race["speed"]) ?? 30;
        var (skillCount, skillOptions, fixedSkills) = Skills(id, parts);
        var (abilityCount, abilityOptions) = AbilityChoice(id, parts);
        var (featCount, featCategory) = FeatChoice(id, parts);

        return new RaceSeedRow
        {
            Id = id,
            Name = name,
            Edition = edition,
            Source = SourceBooks.Of(race, edition),
            Size = string.Join(" or ", race.Arr("size").Select(s => s.AsString()).OfType<string>().Select(s => Sizes.GetValueOrDefault(s, s))),
            Speed = speed,
            SkillChoiceCount = skillCount,
            SkillChoiceOptions = skillOptions,
            LanguageChoiceCount = parts.Sum(p => LanguageChoices(p)),
            AbilityChoiceCount = abilityCount,
            AbilityChoiceOptions = abilityOptions,
            FeatChoiceCount = featCount,
            FeatChoiceCategory = featCategory,
            Languages = parts.SelectMany(NamedLanguages).Distinct().ToList(),
            AbilityBonuses = parts.SelectMany(AbilityBonuses).ToList(),
            ProficiencyIds = fixedSkills.Concat(parts.SelectMany(p => ItemProficiencies(id, p))).Distinct().ToList(),
            // The traits come from every part, so they are SRD text only when every part is
            Traits = Traits(parts.FirstOrDefault(p => !SrdText.IsSrd(p)) ?? race, id, parts.SelectMany(p => p.Arr("entries")).Where(e => !replacedTraits.Contains(e.Str("name") ?? string.Empty))),
            Spells = Spells(id, parts.Select(p => p["additionalSpells"]), spellIdsByKey),
            Subraces = (named.Count == 0 ? [] : nameless)
                .Select(s => Subrace(name, "Standard", speed, edition, s, spellIdsByKey))
                .Concat(named.Select(s => Subrace(name, s.Str("name")!, speed, edition, s, spellIdsByKey)))
                .Concat(versions.Select(v => Lineage(race, speed, v, spellIdsByKey)))
                .ToList(),
        };
    }

    private SubraceSeedRow Subrace(string raceName, string name, int raceSpeed, string edition, JsonObject subrace, IReadOnlyDictionary<string, string> spellIdsByKey)
    {
        var fullName = $"{name} {raceName}";
        var id = Slug.Of(fullName, '_');
        var (skillCount, skillOptions, fixedSkills) = Skills(id, [subrace]);
        var (abilityCount, abilityOptions) = AbilityChoice(id, [subrace]);
        var (featCount, featCategory) = FeatChoice(id, [subrace]);
        var speed = Speed(subrace["speed"]);

        return new SubraceSeedRow
        {
            Id = id,
            Name = fullName,
            Source = SourceBooks.Of(subrace, edition),
            Speed = speed == raceSpeed ? null : speed,

            // The high elf lists all of an elf's languages again, with one more of the player's
            LanguageChoiceCount = LanguageChoices(subrace),
            AbilityChoiceCount = abilityCount,
            AbilityChoiceOptions = abilityOptions,
            SkillChoiceCount = skillCount,
            SkillChoiceOptions = skillOptions,
            FeatChoiceCount = featCount,
            FeatChoiceCategory = featCategory,
            AbilityBonuses = AbilityBonuses(subrace).ToList(),
            ProficiencyIds = fixedSkills.Concat(ItemProficiencies(id, subrace)).Distinct().ToList(),
            Traits = Traits(subrace, id, subrace.Arr("entries")),
            Spells = Spells(id, [subrace["additionalSpells"]], spellIdsByKey),
        };
    }

    private SubraceSeedRow Lineage(JsonObject race, int raceSpeed, Version version, IReadOnlyDictionary<string, string> spellIdsByKey)
    {
        var id = Slug.Of(version.FullName, '_') + "_2024";
        var speed = Speed(version.Node["speed"]);

        return new SubraceSeedRow
        {
            Id = id,
            Name = version.Name,

            // A version is part of its species' entry, in the same book
            Source = SourceBooks.Of(race, RulesEdition.Edition2024),
            Speed = speed == null || speed == raceSpeed ? null : speed,
            Traits = Traits(race, id, version.Traits),
            Spells = Spells(id, [version.Node["additionalSpells"]], spellIdsByKey),
        };
    }

    /// <summary>
    /// A 2024 species' versions: "Elf; Drow Lineage" and the others as they are, and the
    /// dragonborn's "Dragonborn ({{color}})" once for each dragon, with its variables filled in.
    /// </summary>
    private static IEnumerable<Version> Versions(JsonObject race)
    {
        foreach (var version in race.Objects("_versions"))
        {
            if (version["_abstract"] is not JsonObject template)
            {
                yield return Version.Of(version);
                continue;
            }

            foreach (var implementation in version.Objects("_implementations"))
            {
                var text = template.ToJsonString();

                foreach (var (variable, value) in implementation["_variables"] as JsonObject ?? [])
                {
                    text = text.Replace($"{{{{{variable}}}}}", value.AsString(), StringComparison.Ordinal);
                }

                yield return Version.Of(JsonNode.Parse(text)!.AsObject());
            }
        }
    }

    private (int Count, List<string> Options, List<string> Fixed) Skills(string ownerId, IEnumerable<JsonObject> parts)
    {
        var count = 0;
        var options = new List<string>();
        var fixedSkills = new List<string>();

        foreach (var entry in parts.SelectMany(p => p.Objects("skillProficiencies")))
        {
            foreach (var (key, value) in entry)
            {
                switch (key)
                {
                    // Skill Versatility: any two
                    case "any":
                        count += value?.GetValue<int>() ?? 1;
                        break;

                    // Keen Senses (2024): one of three
                    case "choose":
                        count += value.Int("count") ?? 1;
                        options.AddRange(value.Arr("from").Select(s => _proficiencies.IdOf(s.AsString()!)).OfType<string>());
                        break;
                    default:
                        if (_proficiencies.IdOf(key) is { } skillId)
                        {
                            fixedSkills.Add(skillId);
                        }
                        else
                        {
                            SkippedChoices.Add($"{ownerId}: skill {key}");
                        }

                        break;
                }
            }
        }

        return (count, options, fixedSkills);
    }

    // {"common": true, "dwarvish": true, "anyStandard": 1}: the named ones
    private static IEnumerable<string> NamedLanguages(JsonObject part) =>
        part.Objects("languageProficiencies")
            .SelectMany(entry => entry)
            .Where(kvp => kvp.Value is JsonValue value && value.TryGetValue<bool>(out var named) && named && kvp.Key != "other")
            .Select(kvp => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(kvp.Key));

    private static int LanguageChoices(JsonObject part) =>
        part.Objects("languageProficiencies").Sum(entry => (entry.Int("anyStandard") ?? 0) + (entry.Int("any") ?? 0));

    // {"cha": 2, "choose": {...}}: the fixed ones; AbilityChoice reads the "choose"
    private static IEnumerable<AbilityBonusSeedRow> AbilityBonuses(JsonObject part)
    {
        foreach (var (key, value) in part.Objects("ability").SelectMany(entry => entry))
        {
            if (AbilityScore.TryNormalize(key, out var abilityId) && value is JsonValue bonus && bonus.TryGetValue<int>(out var amount))
            {
                yield return new AbilityBonusSeedRow { AbilityScoreId = abilityId, Bonus = amount };
            }
        }
    }

    /// <summary>
    /// The +1s the player picks: {"choose": {"from": ["str", "dex", ...], "count": 2}}, a
    /// half-elf's two or a variant human's. Anything else, such as +2 to one pick, is noted and
    /// left out: a race's choice is +1 each.
    /// </summary>
    private (int Count, List<string> Options) AbilityChoice(string ownerId, IEnumerable<JsonObject> parts)
    {
        var count = 0;
        var options = new List<string>();

        foreach (var choose in parts.SelectMany(p => p.Objects("ability")).Select(entry => entry["choose"]).OfType<JsonObject>())
        {
            if ((choose.Int("amount") ?? 1) != 1 || count > 0)
            {
                SkippedChoices.Add($"{ownerId}: ability increases {choose.ToJsonString()}");
                continue;
            }

            count = choose.Int("count") ?? 1;
            options.AddRange(choose.Arr("from").Select(a => AbilityScore.Normalize(a.AsString()!)));
        }

        return (count, options);
    }

    /// <summary>
    /// The feats the player picks: {"any": 1} for a variant human's, {"anyFromCategory":
    /// {"category": ["O"], "count": 1}} for a 2024 human's Origin feat. A feat the race names
    /// is noted and left out.
    /// </summary>
    private (int Count, string? Category) FeatChoice(string ownerId, IEnumerable<JsonObject> parts)
    {
        var count = 0;
        string? category = null;

        foreach (var (key, value) in parts.SelectMany(p => p.Objects("feats")).SelectMany(entry => entry))
        {
            var categories = value.Arr("category").Select(c => c.AsString()).OfType<string>().ToList();

            if (key == "any" && value is JsonValue any && any.TryGetValue<int>(out var anyCount) && category == null)
            {
                count += anyCount;
            }
            else if (key == "anyFromCategory" && count == 0 && categories.Count == 1 && FeatImporter.CategoryNames.TryGetValue(categories[0], out var name))
            {
                count = value.Int("count") ?? 1;
                category = name;
            }
            else
            {
                SkippedChoices.Add($"{ownerId}: feat {key} {value?.ToJsonString()}");
            }
        }

        return (count, category);
    }

    // Weapons, armour and tools by name, "longsword|phb" or "light"; a choice is noted and left out
    private IEnumerable<string> ItemProficiencies(string ownerId, JsonObject part)
    {
        foreach (var key in ItemProficiencyKeys)
        {
            foreach (var (name, value) in part.Objects(key).SelectMany(entry => entry))
            {
                if (value is JsonValue named && named.TryGetValue<bool>(out var isNamed) && isNamed)
                {
                    if (_proficiencies.IdOfItem(name) is { } proficiencyId)
                    {
                        yield return proficiencyId;
                    }

                    continue;
                }

                SkippedChoices.Add($"{ownerId}: {key[..^"Proficiencies".Length]} {name}");
            }
        }
    }

    private List<TraitSeedRow> Traits(JsonObject owner, string ownerId, IEnumerable<JsonNode?> entries)
    {
        var traits = new List<TraitSeedRow>();

        foreach (var entry in entries.OfType<JsonObject>())
        {
            var name = entry.Str("name");

            if (name == null || NotTraits.Contains(name))
            {
                continue;
            }

            var id = $"{ownerId}_{Slug.Of(name, '_')}";
            id = traits.Exists(t => t.Id == id) ? $"{id}_{traits.Count}" : id;

            traits.Add(new TraitSeedRow
            {
                Id = id,
                Name = name,
                Description = SrdText.Describe(id, owner, () => _renderer.Render(entry["entries"])),
            });
        }

        return traits;
    }

    /// <summary>
    /// The spells a race gives at their levels ("known" cantrips at 1, "innate" spells at 3 and
    /// 5). A block with a name is one lineage's, which that lineage's own version carries.
    /// </summary>
    private List<GrantedSpellSeedRow> Spells(string ownerId, IEnumerable<JsonNode?> additionalSpells, IReadOnlyDictionary<string, string> spellIdsByKey)
    {
        var spells = new List<GrantedSpellSeedRow>();

        foreach (var block in additionalSpells.SelectMany(node => node as JsonArray ?? (node is JsonObject one ? [one.DeepClone()] : [])).OfType<JsonObject>())
        {
            if (block.Str("name") != null)
            {
                continue;
            }

            foreach (var kind in SpellKinds)
            {
                foreach (var (levelKey, value) in block[kind] as JsonObject ?? [])
                {
                    var level = int.TryParse(levelKey, NumberStyles.None, CultureInfo.InvariantCulture, out var characterLevel) ? characterLevel : 1;

                    foreach (var reference in AdditionalSpells.References(value, () => SkippedPicks++))
                    {
                        if (AdditionalSpells.IsFilter(reference))
                        {
                            SkippedPicks++;
                            continue;
                        }

                        var (name, spellSource) = AdditionalSpells.Parse(reference);

                        if (spellIdsByKey.TryGetValue(SpellImporter.Key(name, spellSource), out var spellId))
                        {
                            if (!spells.Exists(s => s.SpellId == spellId))
                            {
                                spells.Add(new GrantedSpellSeedRow { SpellId = spellId, Level = level });
                            }
                        }
                        else
                        {
                            UnresolvedSpells.Add($"{ownerId}: {reference}");
                        }
                    }
                }
            }
        }

        return spells;
    }

    private static int? Speed(JsonNode? speed) =>
        speed is JsonValue walk && walk.TryGetValue<int>(out var feet) ? feet : speed.Int("walk");

    /// <summary>
    /// One version of a species: "Elf; Drow Lineage" is named "Drow Lineage", "Dragonborn
    /// (Black)" is "Black". Its traits are what its changes put in place of the species' own,
    /// and the names it replaces or removes are left off the species.
    /// </summary>
    private sealed record Version(JsonObject Node, string FullName, string Name, List<JsonNode?> Traits, List<string> ReplacedTraits)
    {
        public static Version Of(JsonObject version)
        {
            var fullName = version.Str("name")!;
            var name = fullName.Contains(';', StringComparison.Ordinal)
                ? fullName[(fullName.IndexOf(';', StringComparison.Ordinal) + 1)..].Trim()
                : fullName.EndsWith(')') && fullName.Contains('(', StringComparison.Ordinal)
                    ? fullName[(fullName.LastIndexOf('(') + 1)..^1]
                    : fullName;

            var traits = new List<JsonNode?>();
            var replaced = new List<string>();
            var changes = version["_mod"]?["entries"];

            foreach (var change in (changes as JsonArray ?? (changes is JsonObject one ? [one.DeepClone()] : [])).OfType<JsonObject>())
            {
                replaced.AddRange(Names(change["replace"]).Concat(Names(change["names"])));

                var items = change["items"];
                traits.AddRange(items as JsonArray ?? (items is JsonObject item ? [item.DeepClone()] : []));
            }

            return new Version(version, fullName, name, traits, replaced);
        }

        private static IEnumerable<string> Names(JsonNode? node) =>
            node is JsonArray many ? many.Select(n => n.AsString()).OfType<string>() : node.AsString() is { } single ? [single] : [];
    }
}
