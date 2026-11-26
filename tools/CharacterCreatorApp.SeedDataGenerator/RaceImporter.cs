using System.Globalization;
using System.Text.Json.Nodes;

using CharacterCreatorApp.Domain.DomainModels.Proficiencies;
using CharacterCreatorApp.Domain.DomainModels.Spells;
using CharacterCreatorApp.Domain.ValueObjects;
using CharacterCreatorApp.Infrastructure.Seed.Rows;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Reads races.json into Races.json: the races of every book but the DMG, for the edition each
/// book was written for. The Player's Handbook races keep their ids ("dwarf", "hill_dwarf", and
/// "elf_2024" for a 2024 species); a race from any other book has its book in its id
/// ("aasimar_vgm", "aasimar_mpmm", "warforged_efa_2024"), so a race reprinted in a later book is a
/// second race and no id changes when one is. A 5etools copy ("_copy") is resolved from the race
/// it copies.
/// <para>
/// A 2014 subrace is an entry of 5etools' subrace list, from its race's book or another one of
/// the same edition (Eberron's dragonmarks, Mordenkainen's tiefling bloodlines); its id is its
/// name and its race's id ("mark_of_warding_dwarf"). One without a name is the race as the book
/// prints it: part of the race itself (the half-elf's), or, beside a named one, the "Standard"
/// choice next to it (the human's +1 to everything, which the variant human's picks replace).
/// What a subrace replaces of its race instead of adding to it (Duergar's Superior Darkvision for
/// the dwarf's Darkvision, Draconblood's ability increases) leaves the race, and every subrace
/// that does not replace it gets the race's.
/// </para>
/// <para>
/// A race's versions are versions of it that swap a trait for their own: a 2024 species'
/// lineages, a Monsters of the Multiverse kobold's legacies, the dragonborn's ancestries a
/// template filled in per dragon. They are its subraces.
/// </para>
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

    // The DMG's races are monsters adapted as a DM's option (Bullywug, Skeleton, Kobold with
    // Strength -4), and its aasimar and eladrin are reprinted in VGM, MTF and MPMM
    private static readonly HashSet<string> LeftOutBooks = ["DMG"];

    // Entries the app shows another way, as the sheet's size, speed and languages
    private static readonly HashSet<string> NotTraits = ["Age", "Alignment", "Size", "Speed", "Languages", "Ability Score Increase", "Creature Type"];

    // What a subrace or a version may replace of its race's instead of adding to it
    private static readonly string[] ReplaceableKeys = ["ability", "skillProficiencies"];

    // What a part of a race a child takes keeps of it: where its text comes from and whether it is SRD
    private static readonly string[] SliceKeys = ["name", "source", "page", "srd", "srd52"];

    private static readonly string[] SpellKinds = ["known", "innate", "prepared"];
    private static readonly string[] ItemProficiencyKeys = ["weaponProficiencies", "armorProficiencies", "toolProficiencies"];

    private static readonly Dictionary<string, string> SpeedNames = new()
    {
        ["fly"] = "Flying",
        ["swim"] = "Swimming",
        ["climb"] = "Climbing",
        ["burrow"] = "Burrowing",
    };

    private readonly FiveEToolsSource _source = source;
    private readonly EntryRenderer _renderer = renderer;
    private readonly ProficiencyIndex _proficiencies = proficiencies;

    /// <summary>The imported races by name, for a feat's race prerequisite.</summary>
    public RaceNames Names { get; } = new();

    /// <summary>Choices a race offers that the app does not ask for yet, such as a dwarf's tools.</summary>
    public SortedSet<string> SkippedChoices { get; } = [];

    /// <summary>Spells a race gives that are not in the spell files.</summary>
    public SortedSet<string> UnresolvedSpells { get; } = [];

    /// <summary>How many "choose a spell" grants were left out, such as a high elf's cantrip.</summary>
    public int SkippedPicks { get; private set; }

    public List<RaceSeedRow> Import(IReadOnlyDictionary<string, string> spellIdsByKey)
    {
        var file = _source.Load("races.json");
        var allRaces = file.Objects("race").ToList();
        var allSubraces = file.Objects("subrace").ToList();

        // A copy names a race by name and book, and a subrace by its race's too
        JsonObject? Find(JsonObject copy) => copy.Str("raceName") is { } raceName
            ? allSubraces.Find(s => s.Str("name") == copy.Str("name") && s.Str("source") == copy.Str("source") && s.Str("raceName") == raceName && s.Str("raceSource") == copy.Str("raceSource"))
            : allRaces.Find(r => r.Str("name") == copy.Str("name") && r.Str("source") == copy.Str("source"));

        var subraces = allSubraces
            .Where(s => !LeftOutBooks.Contains(s.Str("source")!))
            .Select(s => FiveEToolsCopy.Resolve(s, Find))
            .ToList();

        return allRaces
            .Where(race => !LeftOutBooks.Contains(race.Str("source")!))
            .Select(race => Race(FiveEToolsCopy.Resolve(race, Find), subraces, spellIdsByKey))
            .OrderBy(r => r.Edition, StringComparer.Ordinal)
            .ThenBy(r => r.Name, StringComparer.Ordinal)
            .ThenBy(r => r.Id, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>"aasimar_mpmm" for a race from MPMM; a Player's Handbook race is its name alone.</summary>
    internal static string BaseId(string name, string book) =>
        Slug.Of(name, '_') + (SourceBooks.IsPlayersHandbook(book) ? string.Empty : $"_{Slug.Of(book, '_')}");

    /// <summary>
    /// A subrace's own name: "Variant; Mark of Detection" is "Mark of Detection", and
    /// "Zendikar; Grotag Tribe" is "Grotag Tribe".
    /// </summary>
    internal static string SubraceName(string name) =>
        name.Contains(';', StringComparison.Ordinal) ? name[(name.LastIndexOf(';') + 1)..].Trim() : name;

    /// <summary>
    /// A version's own name: "Elf; Drow Lineage" is "Drow Lineage", "Dragonborn (Black)" is
    /// "Black" and "Dragonborn (Chromatic; Black)" is "Black" too.
    /// </summary>
    internal static string VersionName(string fullName)
    {
        var name = fullName.EndsWith(')') && fullName.Contains('(', StringComparison.Ordinal)
            ? fullName[(fullName.LastIndexOf('(') + 1)..^1]
            : fullName;

        return SubraceName(name);
    }

    /// <summary>
    /// A trait the generator writes for what 5etools has as data and the book only in prose:
    /// "Flying speed 50 feet. Swimming speed equal to your walking speed." Null when the speed is
    /// only a walking one.
    /// </summary>
    internal static string? OtherSpeeds(JsonNode? speed)
    {
        var speeds = new List<string>();

        foreach (var (key, kind) in SpeedNames)
        {
            switch (speed is JsonObject all ? all[key] : null)
            {
                case JsonValue value when value.TryGetValue<int>(out var feet):
                    speeds.Add(string.Create(CultureInfo.InvariantCulture, $"{kind} speed {feet} feet."));
                    break;
                case JsonValue value when value.TryGetValue<bool>(out var same) && same:
                    speeds.Add($"{kind} speed equal to your walking speed.");
                    break;
                case JsonObject withCondition when withCondition.Int("number") is { } feet:
                    speeds.Add(string.Create(CultureInfo.InvariantCulture, $"{kind} speed {feet} feet, at times."));
                    break;
            }
        }

        return speeds.Count == 0 ? null : string.Join(" ", speeds);
    }

    /// <summary>"Your creature type is Fey." for a race that is not Humanoid; null for one that is.</summary>
    internal static string? CreatureType(JsonObject race)
    {
        var types = race.Arr("creatureTypes").Select(t => t.AsString()).OfType<string>().ToList();

        return types.Count == 0 || types.All(t => t == "humanoid")
            ? null
            : $"Your creature type is {string.Join(" or ", types.Select(t => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(t)))}.";
    }

    /// <summary>
    /// The ability increases the player picks: {"choose": {"from": [...], "count": 2}} is a
    /// half-elf's two +1s, an "amount" of 2 a SCAG tiefling variant's +2 to one. A 2014 race of
    /// Tasha's lineage rules (every Monsters of the Multiverse race) with no increases of its own
    /// raises one ability by 2 and another by 1, or three by 1: 3 points over all six, at most 2
    /// on one.
    /// </summary>
    internal static AbilityChoiceRow AbilityChoice(IEnumerable<JsonObject> parts, bool lineageRules, Action<string> skipped)
    {
        var all = parts.ToList();
        AbilityChoiceRow? choice = null;

        foreach (var choose in all.SelectMany(p => p.Objects("ability")).Select(entry => entry["choose"]).OfType<JsonObject>())
        {
            var options = choose.Arr("from").Select(a => a.AsString()).OfType<string>().ToList();

            if (choice != null || options.Count == 0)
            {
                skipped($"ability increases {choose.ToJsonString()}");
                continue;
            }

            var amount = choose.Int("amount") ?? 1;
            choice = new AbilityChoiceRow((choose.Int("count") ?? 1) * amount, options.ConvertAll(AbilityScore.Normalize), amount, amount);
        }

        if (choice == null && lineageRules && !all.Exists(p => p.Objects("ability").Any()))
        {
            choice = new AbilityChoiceRow(3, AbilityScore.All.Select(a => a.Id).ToList(), 2, 1);
        }

        return choice ?? new AbilityChoiceRow(0, [], 1, 1);
    }

    private static bool UsesLineageRules(JsonObject race) =>
        race["lineage"] is JsonValue lineage && !(lineage.TryGetValue<bool>(out var flag) && !flag);

    // What the subrace replaces of its race's: an "overwrite" key, or the race's spells when it
    // has its own (a tiefling bloodline's Legacy of Dis) or replaces the trait that names them
    // (a winged tiefling's wings for the Infernal Legacy)
    private static bool Replaces(JsonObject race, JsonObject subrace, string key)
    {
        if (key != "additionalSpells")
        {
            return subrace["overwrite"].Bool(key);
        }

        var replaced = ReplacedTraitNames(subrace).ToHashSet();

        return subrace["additionalSpells"] != null
            || race.Objects("entries").Any(e => replaced.Contains(e.Str("name") ?? string.Empty) && e.ToJsonString().Contains("{@spell ", StringComparison.Ordinal));
    }

    // The traits a subrace names as replacing its race's ("Superior Darkvision" for "Darkvision")
    private static IEnumerable<string> ReplacedTraitNames(JsonObject subrace) =>
        subrace.Objects("entries").Select(e => e["data"].Str("overwrite")).OfType<string>();

    // The race's keys and traits a child takes because others replace them, with what tells
    // where its text comes from, so the race's SRD text stays SRD text
    private static JsonObject Slice(JsonObject race, IEnumerable<string> keys, IReadOnlyCollection<string> traitNames)
    {
        var slice = new JsonObject();

        foreach (var key in SliceKeys.Concat(keys))
        {
            if (race[key] is { } value)
            {
                slice[key] = value.DeepClone();
            }
        }

        slice["entries"] = new JsonArray(race.Objects("entries").Where(e => traitNames.Contains(e.Str("name") ?? string.Empty)).Select(e => (JsonNode)e.DeepClone()).ToArray());
        return slice;
    }

    private RaceSeedRow Race(JsonObject race, List<JsonObject> allSubraces, IReadOnlyDictionary<string, string> spellIdsByKey)
    {
        var name = race.Str("name")!;
        var book = race.Str("source")!;
        var edition = SourceBooks.EditionOfBook(book);
        var suffix = edition == RulesEdition.Edition2024 ? "_2024" : string.Empty;
        var baseId = BaseId(name, book);
        var id = baseId + suffix;

        Names.AddRace(edition, name, id);

        var subraces = allSubraces
            .Where(s => s.Str("raceName") == name && s.Str("raceSource") == book && SourceBooks.EditionOfBook(s.Str("source")!) == edition)
            .ToList();

        // A subrace without a name is the race as the book prints it. Alone it is part of the
        // race; beside a named one it is a choice of its own, since the named one replaces it
        var named = subraces.Where(s => !string.IsNullOrEmpty(s.Str("name"))).ToList();
        var nameless = subraces.Where(s => string.IsNullOrEmpty(s.Str("name"))).ToList();
        var choices = named.Count == 0 ? [] : nameless.Concat(named).ToList();
        var versions = Versions(race).ToList();

        // A trait every version swaps out ("Elven Lineage") leaves the species. One only some swap
        // stays: the drow's longer Darkvision replaces the one high and wood elves keep.
        var replacedByEveryVersion = versions.Count == 0
            ? []
            : versions.Skip(1).Aggregate(versions[0].ReplacedTraits.ToHashSet(), (common, v) =>
            {
                common.IntersectWith(v.ReplacedTraits);
                return common;
            });

        // What a subrace or version replaces leaves the race for every child that does not
        var movedKeys = ReplaceableKeys.Append("additionalSpells")
            .Where(key => race[key] != null && (choices.Exists(s => Replaces(race, s, key)) || versions.Exists(v => v.Node.ContainsKey(key))))
            .ToHashSet();
        var movedTraits = choices.SelectMany(ReplacedTraitNames).ToHashSet();

        var racePart = race.DeepClone().AsObject();

        foreach (var key in movedKeys)
        {
            racePart.Remove(key);
        }

        var parts = new List<JsonObject> { racePart };

        if (named.Count == 0)
        {
            parts.AddRange(nameless);
        }

        var speed = Speed(race["speed"]) ?? 30;
        var (skillCount, skillOptions, fixedSkills) = Skills(id, parts);
        var abilityChoice = AbilityChoice(parts, edition == RulesEdition.Edition2014 && UsesLineageRules(race) && !movedKeys.Contains("ability"), note => SkippedChoices.Add($"{id}: {note}"));
        var (featCount, featCategory) = FeatChoice(id, parts);
        var toolChoices = new List<ToolChoice>();
        var itemProficiencyIds = parts.SelectMany(p => ItemProficiencies(id, p, toolChoices)).ToList();
        var languages = parts.SelectMany(NamedLanguages).Distinct().ToList();

        var traitEntries = parts
            .SelectMany(p => p.Arr("entries").Select(e => (Owner: p, Entry: e)))
            .Where(t => !replacedByEveryVersion.Contains(t.Entry.Str("name") ?? string.Empty) && !movedTraits.Contains(t.Entry.Str("name") ?? string.Empty));
        var traits = Traits(id, traitEntries);
        AddGenerated(traits, id, race, OtherSpeeds(race["speed"]), "Speeds");
        AddGenerated(traits, id, race, CreatureType(race), "Creature Type");

        var children = choices
            .Select(s =>
            {
                var subraceName = string.IsNullOrEmpty(s.Str("name")) ? "Standard" : SubraceName(s.Str("name")!);
                var subraceId = Slug.Of(subraceName, '_') + "_" + baseId + suffix;

                if (!string.IsNullOrEmpty(s.Str("name")))
                {
                    Names.AddSubrace(edition, name, s.Str("name")!, subraceId);
                }

                var replaced = ReplacedTraitNames(s).ToHashSet();
                var slice = Slice(race, movedKeys.Where(k => !Replaces(race, s, k)), movedTraits.Where(t => !replaced.Contains(t)).ToList());
                NoteSubraceLanguages(subraceId, s, languages);

                return Child(subraceId, $"{subraceName} {name}", s, edition, speed, [slice, s], s["speed"], spellIdsByKey);
            })
            .Concat(versions.Select(v =>
            {
                var versionId = $"{baseId}_{Slug.Of(v.Name, '_')}{suffix}";
                var slice = Slice(race, movedKeys.Where(k => !v.Node.ContainsKey(k)), []);
                var versionTraits = v.Traits.Select(e => (Owner: race, Entry: e));

                return Child(versionId, v.Name, race, edition, speed, [slice, v.Node], v.Node["speed"], spellIdsByKey, versionTraits);
            }))
            .ToList();

        return new RaceSeedRow
        {
            Id = id,
            Name = name,
            Edition = edition,
            Source = SourceBooks.Of(race, edition),
            Size = string.Join(" or ", race.Arr("size").Select(s => s.AsString()).OfType<string>().Select(SizeName)),
            Speed = speed,
            SkillChoiceCount = skillCount,
            SkillChoiceOptions = skillOptions,
            LanguageChoiceCount = parts.Sum(p => LanguageChoices(p)),
            AbilityChoiceCount = abilityChoice.Points,
            AbilityChoiceOptions = abilityChoice.Options,
            AbilityChoiceMaxPerAbility = abilityChoice.MaxPerAbility,
            AbilityChoiceMinPerAbility = abilityChoice.MinPerAbility,
            FeatChoiceCount = featCount,
            FeatChoiceCategory = featCategory,
            Languages = languages,
            AbilityBonuses = parts.SelectMany(AbilityBonuses).ToList(),
            ProficiencyIds = fixedSkills.Concat(itemProficiencyIds).Distinct().ToList(),
            ToolChoices = toolChoices,
            Traits = traits,
            Spells = Spells(id, parts.Select(p => p["additionalSpells"]), spellIdsByKey),
            SpellChoice = SpellChoice(parts.Select(p => p["additionalSpells"]), edition),
            Subraces = children,
        };
    }

    // 5etools' "V" is a size that varies: a verdan starts Small and grows to Medium
    private static string SizeName(string code) => code == "V" ? "Small or Medium" : Sizes.GetValueOrDefault(code, code);

    /// <summary>
    /// A subrace or version. Its parts are what it takes of its race (what others replace) and its
    /// own entry; its source is the book its entry comes from, its traits each part's entries or
    /// a version's own.
    /// </summary>
    private SubraceSeedRow Child(
        string id,
        string name,
        JsonObject sourceEntry,
        string edition,
        int raceSpeed,
        List<JsonObject> parts,
        JsonNode? ownSpeed,
        IReadOnlyDictionary<string, string> spellIdsByKey,
        IEnumerable<(JsonObject Owner, JsonNode? Entry)>? ownTraits = null)
    {
        var (skillCount, skillOptions, fixedSkills) = Skills(id, parts);
        var abilityChoice = AbilityChoice(parts, lineageRules: false, note => SkippedChoices.Add($"{id}: {note}"));
        var (featCount, featCategory) = FeatChoice(id, parts);
        var speed = Speed(ownSpeed);

        var traitEntries = ownTraits == null
            ? parts.SelectMany(p => p.Arr("entries").Select(e => (Owner: p, Entry: e)))
            : parts[0].Arr("entries").Select(e => (Owner: parts[0], Entry: e)).Concat(ownTraits);
        var traits = Traits(id, traitEntries);
        AddGenerated(traits, id, sourceEntry, OtherSpeeds(ownSpeed), "Speeds");

        return new SubraceSeedRow
        {
            Id = id,
            Name = name,
            Source = SourceBooks.Of(sourceEntry, edition),
            Speed = speed == null || speed == raceSpeed ? null : speed,

            // The high elf lists all of an elf's languages again, with one more of the player's
            LanguageChoiceCount = parts.Sum(LanguageChoices),
            AbilityChoiceCount = abilityChoice.Points,
            AbilityChoiceOptions = abilityChoice.Options,
            AbilityChoiceMaxPerAbility = abilityChoice.MaxPerAbility,
            AbilityChoiceMinPerAbility = abilityChoice.MinPerAbility,
            SkillChoiceCount = skillCount,
            SkillChoiceOptions = skillOptions,
            FeatChoiceCount = featCount,
            FeatChoiceCategory = featCategory,
            AbilityBonuses = parts.SelectMany(AbilityBonuses).ToList(),
            ProficiencyIds = fixedSkills.Concat(parts.SelectMany(p => ItemProficiencies(id, p, toolChoices: null))).Distinct().ToList(),
            Traits = traits,
            Spells = Spells(id, parts.Select(p => p["additionalSpells"]), spellIdsByKey),
            SpellChoice = SpellChoice(parts.Select(p => p["additionalSpells"]), edition),
        };
    }

    // A subrace has no languages of its own in the model: one it names beyond its race's is noted
    private void NoteSubraceLanguages(string subraceId, JsonObject subrace, List<string> raceLanguages)
    {
        foreach (var language in NamedLanguages(subrace).Where(l => !raceLanguages.Contains(l)))
        {
            SkippedChoices.Add($"{subraceId}: language {language}");
        }
    }

    /// <summary>
    /// A race's versions: "Elf; Drow Lineage" and the others as they are, and the dragonborn's
    /// "Dragonborn ({{color}})" once for each dragon, with its variables filled in.
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

    // Weapons, armour and tools by name, "longsword|phb" or "light". A race's tool pick (a dwarf's)
    // goes in toolChoices; a subrace has none in the data, and any other choice is noted and left out.
    private IEnumerable<string> ItemProficiencies(string ownerId, JsonObject part, List<ToolChoice>? toolChoices)
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

                if (toolChoices != null && key == "toolProficiencies" && ToolChoiceReader.Read(name, value, _proficiencies) is { } choice)
                {
                    toolChoices.Add(choice);
                    continue;
                }

                SkippedChoices.Add($"{ownerId}: {key[..^"Proficiencies".Length]} {name}");
            }
        }
    }

    // Each entry is described from the part it comes from: a PHB race's trait a subrace of
    // another book takes keeps its SRD text, and a book's trait says where that book has it
    private List<TraitSeedRow> Traits(string ownerId, IEnumerable<(JsonObject Owner, JsonNode? Entry)> entries)
    {
        var traits = new List<TraitSeedRow>();

        foreach (var (owner, node) in entries)
        {
            if (node is not JsonObject entry || entry.Str("name") is not { } name || NotTraits.Contains(name))
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

    // A trait the generator writes in its own words, with where the book has the rest
    private static void AddGenerated(List<TraitSeedRow> traits, string ownerId, JsonObject owner, string? mechanics, string name)
    {
        if (mechanics == null)
        {
            return;
        }

        traits.Add(new TraitSeedRow
        {
            Id = $"{ownerId}_{Slug.Of(name, '_')}",
            Name = name,
            Description = $"{mechanics}\n\n{SrdText.Reference(owner)}",
        });
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

    // A named block is one lineage's, which that lineage's own version carries
    private static BonusSpellChoice? SpellChoice(IEnumerable<JsonNode?> additionalSpells, string edition) =>
        AdditionalSpells.Choice(
            additionalSpells
                .SelectMany(node => node as JsonArray ?? (node is JsonObject one ? [one.DeepClone()] : []))
                .OfType<JsonObject>()
                .Where(block => block.Str("name") == null)
                .ToList(),
            edition);

    private static int? Speed(JsonNode? speed) =>
        speed is JsonValue walk && walk.TryGetValue<int>(out var feet) ? feet : speed.Int("walk");

    /// <summary>A race's or subrace's ability choice: Points over Options, from MinPerAbility to MaxPerAbility on each one raised.</summary>
    internal sealed record AbilityChoiceRow(int Points, List<string> Options, int MaxPerAbility, int MinPerAbility);

    /// <summary>
    /// One version of a race: "Elf; Drow Lineage" is named "Drow Lineage", "Dragonborn
    /// (Black)" is "Black". Its traits are what its changes put in place of the race's own,
    /// and the names it replaces or removes are left off the race.
    /// </summary>
    private sealed record Version(JsonObject Node, string Name, List<JsonNode?> Traits, List<string> ReplacedTraits)
    {
        public static Version Of(JsonObject version)
        {
            var traits = new List<JsonNode?>();
            var replaced = new List<string>();
            var changes = version["_mod"]?["entries"];

            foreach (var change in (changes as JsonArray ?? (changes is JsonObject one ? [one.DeepClone()] : [])).OfType<JsonObject>())
            {
                replaced.AddRange(Names(change["replace"]).Concat(Names(change["names"])));

                var items = change["items"];
                traits.AddRange(items as JsonArray ?? (items is JsonObject item ? [item.DeepClone()] : []));
            }

            return new Version(version, VersionName(version.Str("name")!), traits, replaced);
        }

        private static IEnumerable<string> Names(JsonNode? node) =>
            node is JsonArray many ? many.Select(n => n.AsString()).OfType<string>() : node.AsString() is { } single ? [single] : [];
    }
}
