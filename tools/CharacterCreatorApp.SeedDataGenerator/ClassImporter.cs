using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Proficiencies;
using CharacterCreatorApp.Domain.ValueObjects;
using CharacterCreatorApp.Infrastructure.Seed.Rows;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Reads the 5etools class files into the app's classes, subclasses and their features, for one
/// rules edition at a time. 5etools keeps both editions side by side: a class, feature or
/// subclass belongs to the 2014 rules when its class source is PHB (TCE for the Artificer) and to
/// the 2024 rules when it is XPHB (EFA). What the app used to hardcode per class (the subclass
/// level, the ability score improvement levels, saving throws, the spell tables) is read from the
/// same data.
/// </summary>
internal sealed partial class ClassImporter
{
    /// <summary>
    /// Subclass ids that do not follow the rule in <see cref="SubclassIds"/>. The Wizard's War
    /// Magic had "war" before the Cleric's War Domain was added, and characters store the id.
    /// </summary>
    private static readonly Dictionary<(string ClassName, string ShortName), string> SubclassIdOverrides = new()
    {
        [("Wizard", "War")] = "war",
        [("Cleric", "War")] = "war-domain",
        [("Cleric", "Knowledge (PSA)")] = "knowledge-psa",
    };

    private readonly ProficiencyIndex _proficiencies;
    private readonly List<JsonObject> _classes = [];
    private readonly List<JsonObject> _subclasses = [];
    private readonly Dictionary<string, JsonObject> _classFeatures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, JsonObject> _subclassFeatures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _bookTextByFeatureId = [];

    public ClassImporter(FiveEToolsSource source, ProficiencyIndex proficiencies)
    {
        _proficiencies = proficiencies;

        foreach (var file in source.ClassFiles())
        {
            _classes.AddRange(file.Objects("class"));
            _subclasses.AddRange(file.Objects("subclass"));

            foreach (var feature in file.Objects("classFeature"))
            {
                _classFeatures.TryAdd(ClassFeatureKey(feature), feature);
            }

            foreach (var feature in file.Objects("subclassFeature"))
            {
                _subclassFeatures.TryAdd(SubclassFeatureKey(feature), feature);
            }
        }

        Renderer = new EntryRenderer(Resolve);
    }

    public EntryRenderer Renderer { get; }

    /// <summary>Features a class or subclass lists that are not in the data.</summary>
    public SortedSet<string> MissingFeatures { get; } = [];

    /// <summary>Tasha's optional class features, which replace a class's own and are left out.</summary>
    public SortedSet<string> SkippedVariants { get; } = [];

    public ClassImport Import(string edition)
    {
        var fiveEToolsEdition = edition == RulesEdition.Edition2014 ? "classic" : "one";
        var classes = _classes
            .Where(c => c.Str("edition") == fiveEToolsEdition)
            .OrderBy(c => c.Str("name"), StringComparer.Ordinal)
            .ToList();

        var subclassIds = SubclassIds(classes, edition);
        var import = new ClassImport([], [], [], []);

        foreach (var @class in classes)
        {
            var classId = Slug.Of(@class.Str("name")!) + Suffix(edition);
            var classFeatures = ClassFeatureRows(@class, classId);
            var classRow = ClassRow(@class, classId, edition, classFeatures);

            import.Classes.Add(classRow);
            import.Features.AddRange(classFeatures);

            foreach (var subclass in SubclassesOf(@class))
            {
                var subclassId = subclassIds[subclass];

                var features = SubclassFeatureRows(subclass, subclassId);

                import.Subclasses.Add(SubclassRow(subclass, subclassId, classRow, edition, features));
                import.Features.AddRange(features);
                import.SubclassData[subclassId] = subclass;
            }
        }

        return import;
    }

    private static string Suffix(string edition) => edition == RulesEdition.Edition2014 ? string.Empty : "-2024";

    private IEnumerable<JsonObject> SubclassesOf(JsonObject @class) =>
        _subclasses
            .Where(s => s.Str("className") == @class.Str("name") && s.Str("classSource") == @class.Str("source"))
            // 5etools repeats every 2014 subclass under the 2024 class, adjusted to fit it; the
            // 2024 rules here offer the 2024 books' own subclasses
            .Where(s => s["_copy"] == null)
            .OrderBy(s => s.Str("name"), StringComparer.Ordinal);

    /// <summary>
    /// A subclass's id is its short name as a slug ("totem-warrior"). Two classes with a subclass
    /// of the same name get the class added to both ("shadow-monk", "shadow-sorcerer"), and a
    /// 2024 subclass ends in "-2024".
    /// </summary>
    private Dictionary<JsonObject, string> SubclassIds(List<JsonObject> classes, string edition)
    {
        var subclasses = classes
            .SelectMany(c => SubclassesOf(c).Select(s => (ClassName: c.Str("name")!, ShortName: s.Str("shortName")!, Subclass: s)))
            .ToList();

        var ids = new Dictionary<JsonObject, string>();

        foreach (var (className, shortName, subclass) in subclasses)
        {
            if (SubclassIdOverrides.TryGetValue((className, shortName), out var id))
            {
                ids[subclass] = id;
            }
        }

        foreach (var group in subclasses.Where(s => !ids.ContainsKey(s.Subclass)).GroupBy(s => Slug.Of(Slug.WithoutParenthetical(s.ShortName))))
        {
            var sharedByClasses = group.Select(s => s.ClassName).Distinct().Count() > 1;

            foreach (var (className, _, subclass) in group)
            {
                ids[subclass] = sharedByClasses ? $"{group.Key}-{Slug.Of(className)}" : group.Key;
            }
        }

        foreach (var subclass in ids.Keys.ToList())
        {
            ids[subclass] += Suffix(edition);
        }

        var duplicates = ids.GroupBy(kvp => kvp.Value).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException($"Subclass ids used twice: {string.Join(", ", duplicates)}. Add an override.");
        }

        return ids;
    }

    private ClassSeedRow ClassRow(JsonObject @class, string classId, string edition, List<FeatureSeedRow> features)
    {
        var references = ClassFeatureReferences(@class).ToList();
        var startingProficiencies = @class["startingProficiencies"];

        return new ClassSeedRow
        {
            Id = classId,
            Name = @class.Str("name")!,
            Edition = edition,
            Source = SourceBooks.Of(@class, edition),
            HitDie = $"1d{@class["hd"].Int("faces")}",
            SubclassLevel = references.First(r => r.GainsSubclass).Level,
            AsiLevels = AsiLevels(@class, references),
            SavingThrowAbilityIds = @class.Arr("proficiency").Select(a => AbilityScore.Normalize(a.AsString()!)).ToList(),
            UnarmoredArmorClass = UnarmoredArmorClassIn(features, classId, fromLevel: 1),
            ProficiencyIds = StartingProficiencyIds(startingProficiencies),
            ProficiencyChoices = SkillChoice(startingProficiencies),
            ToolChoices = ToolChoices(startingProficiencies),
            Spellcasting = Spellcasting(@class, edition),
            SpellProgression = SpellProgression(@class, @class["classTableGroups"])
        };
    }

    /// <summary>
    /// Every level with an Ability Score Improvement, and the 2024 Epic Boon, which takes the
    /// same place at 19 and can be any feat the character qualifies for.
    /// </summary>
    private static List<int> AsiLevels(JsonObject @class, List<FeatureReference> references) =>
        references
            .Where(r => r.Name == "Ability Score Improvement")
            .Select(r => r.Level)
            .Concat(@class.Objects("featProgression")
                .Where(p => p.Arr("category").Any(c => c.AsString() == "EB"))
                .SelectMany(p => (p["progression"] as JsonObject ?? []).Select(kvp => int.Parse(kvp.Key, CultureInfo.InvariantCulture))))
            .Distinct()
            .Order()
            .ToList();

    private List<string> StartingProficiencyIds(JsonNode? startingProficiencies)
    {
        var armor = startingProficiencies.Arr("armor").Select(a => a.AsString() ?? a.Str("proficiency"));

        // A weapon marked optional is a variant rule (the Artificer's firearms)
        var weapons = startingProficiencies.Arr("weapons")
            .Where(w => !w.Bool("optional"))
            .Select(w => w.AsString() ?? w.Str("proficiency"));

        // Only named tools: "anyArtisansTool" and the like are choices, which are not modelled
        var tools = startingProficiencies.Objects("toolProficiencies")
            .SelectMany(t => t.Where(kvp => kvp.Value is JsonValue v && v.TryGetValue<bool>(out var named) && named).Select(kvp => kvp.Key));

        return armor.Concat(weapons).Concat(tools)
            .OfType<string>()
            .Select(_proficiencies.IdOf)
            .OfType<string>()
            .Distinct()
            .ToList();
    }

    // A bard's three instruments; a monk's artisan's tools or instrument, written as two entries
    private List<ToolChoice> ToolChoices(JsonNode? startingProficiencies)
    {
        var entries = startingProficiencies.Objects("toolProficiencies").ToList();

        if (entries.Count > 1)
        {
            return ToolChoiceReader.ReadAlternatives(entries, _proficiencies) is { } alternatives
                ? [alternatives]
                : throw new InvalidOperationException($"Class tool proficiencies the generator cannot read: {startingProficiencies?.ToJsonString()}");
        }

        return entries.SelectMany(entry => entry)
            .Where(kvp => kvp.Value is not JsonValue named || !named.TryGetValue<bool>(out var isNamed) || !isNamed)
            .Select(kvp => ToolChoiceReader.Read(kvp.Key, kvp.Value, _proficiencies)
                ?? throw new InvalidOperationException($"Class tool proficiency the generator cannot read: {kvp.Key}"))
            .ToList();
    }

    private ProficiencyChoiceSeedRow? SkillChoice(JsonNode? startingProficiencies)
    {
        var skills = startingProficiencies.Objects("skills").FirstOrDefault();

        if (skills == null)
        {
            return null;
        }

        if (skills.Int("any") is { } any)
        {
            return new ProficiencyChoiceSeedRow { Type = "skills", Count = any, Options = _proficiencies.Skills.Select(s => s.Id).Order().ToList() };
        }

        var choose = skills["choose"];

        return new ProficiencyChoiceSeedRow
        {
            Type = "skills",
            Count = choose.Int("count") ?? 1,
            Options = choose.Arr("from").Select(s => _proficiencies.IdOf(s.AsString()!)).OfType<string>().ToList()
        };
    }

    private static SpellcastingSeedRow? Spellcasting(JsonObject @class, string edition)
    {
        if (@class.Str("spellcastingAbility") is not { } ability)
        {
            return null;
        }

        return new SpellcastingSeedRow
        {
            Ability = AbilityScore.FromId(ability).Name,

            // Every 2024 caster prepares spells; a 2014 one does when its class says how many
            IsPreparedCaster = edition == RulesEdition.Edition2024 || @class["preparedSpells"] != null
        };
    }

    /// <summary>
    /// What a caster has at each level: cantrips, the levelled spells the player picks, and the
    /// highest spell level with a slot. A level with neither slots nor cantrips has no row, which
    /// is how the 2014 Paladin and Ranger show they cast nothing at level 1.
    /// </summary>
    private static List<SpellProgressionSeedRow> SpellProgression(JsonObject caster, JsonNode? tableGroups)
    {
        var maxSpellLevels = MaxSpellLevels(tableGroups);
        var cantrips = caster.Ints("cantripProgression");
        var known = caster.Ints("spellsKnownProgression");
        var spellbook = caster.Ints("spellsKnownProgressionFixed");
        var prepared = caster.Ints("preparedSpellsProgression");

        var rows = new List<SpellProgressionSeedRow>();

        for (var level = 1; level <= 20; level++)
        {
            var cantripsKnown = level <= cantrips.Count ? cantrips[level - 1] : 0;

            if (maxSpellLevels[level] == 0 && cantripsKnown == 0)
            {
                continue;
            }

            // A wizard picks the spells in their spellbook: six, and two more each level
            var spellsKnown = known.Count > 0 ? known[level - 1]
                : spellbook.Count > 0 ? spellbook.Take(level).Sum()
                : prepared.Count > 0 ? prepared[level - 1]
                : 0;

            rows.Add(new SpellProgressionSeedRow
            {
                Level = level,
                CantripsKnown = cantripsKnown,
                SpellsKnown = spellsKnown,
                MaxSpellLevel = maxSpellLevels[level]
            });
        }

        return rows;
    }

    /// <summary>
    /// The highest spell level with a slot, per character level (index 1 to 20): from the spell
    /// slot table, or from the Warlock's "Slot Level" column.
    /// </summary>
    private static int[] MaxSpellLevels(JsonNode? tableGroups)
    {
        var levels = new int[21];

        foreach (var group in tableGroups as JsonArray ?? [])
        {
            var slotRows = group.Arr("rowsSpellProgression");

            for (var i = 0; i < slotRows.Count && i < 20; i++)
            {
                var slots = slotRows[i] as JsonArray ?? [];

                for (var spellLevel = slots.Count; spellLevel > 0; spellLevel--)
                {
                    if (slots[spellLevel - 1] is JsonValue value && value.TryGetValue<int>(out var count) && count > 0)
                    {
                        levels[i + 1] = Math.Max(levels[i + 1], spellLevel);
                        break;
                    }
                }
            }

            var slotLevelColumn = group.Arr("colLabels")
                .Select(l => FiveEToolsMarkup.ToPlainText(l.AsString() ?? string.Empty))
                .ToList()
                .IndexOf("Slot Level");

            if (slotLevelColumn < 0)
            {
                continue;
            }

            var rows = group.Arr("rows");

            for (var i = 0; i < rows.Count && i < 20; i++)
            {
                levels[i + 1] = Math.Max(levels[i + 1], SlotLevelIn((rows[i] as JsonArray)?.ElementAtOrDefault(slotLevelColumn)));
            }
        }

        return levels;
    }

    // The 2014 table writes a slot level as a link, "{@filter 1st|spells|level=1|class=Warlock}";
    // the 2024 one as the number
    private static int SlotLevelIn(JsonNode? cell)
    {
        if (cell is JsonValue value && value.TryGetValue<int>(out var level))
        {
            return level;
        }

        return cell.AsString() is { } text && SlotLevel().Match(text) is { Success: true } match
            ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)
            : 0;
    }

    /// <summary>
    /// The armour class one of these features gives without armour, read from its text. The app
    /// keeps no level for it, so the generator stops when the feature comes later than the level
    /// the class or subclass is taken at.
    /// </summary>
    private UnarmoredArmorClass? UnarmoredArmorClassIn(List<FeatureSeedRow> features, string ownerId, int fromLevel)
    {
        var found = features
            .Select(f => (Feature: f, ArmorClass: UnarmoredArmorClassParser.Parse(_bookTextByFeatureId[f.Id])))
            .Where(f => f.ArmorClass != null)
            .ToList();

        if (found.Count > 1)
        {
            throw new InvalidOperationException($"{ownerId} has more than one unarmoured armour class: {string.Join(", ", found.Select(f => f.Feature.Id))}.");
        }

        if (found.Count == 1 && found[0].Feature.Level != fromLevel)
        {
            throw new InvalidOperationException($"{found[0].Feature.Id} gives an unarmoured armour class at level {found[0].Feature.Level}, not at {fromLevel}.");
        }

        return found.Count == 1 ? found[0].ArmorClass : null;
    }

    private SubclassSeedRow SubclassRow(
        JsonObject subclass, string subclassId, ClassSeedRow classRow, string edition, List<FeatureSeedRow> features)
    {
        // Only a subclass with a spell progression of its own casts; the Arcane Archer has an
        // ability for its shots but no spells
        var casts = subclass["casterProgression"] != null && subclass.Str("spellcastingAbility") != null;
        var schoolLimit = casts
            ? features.Where(f => f.Name == "Spellcasting").Select(f => SpellSchoolLimitParser.Parse(_bookTextByFeatureId[f.Id])).FirstOrDefault(l => l != null)
            : null;

        return new SubclassSeedRow
        {
            Id = subclassId,
            Name = subclass.Str("name")!,
            ClassId = classRow.Id,
            Source = SourceBooks.Of(subclass, edition),
            SpellcastingAbility = casts ? AbilityScore.FromId(subclass.Str("spellcastingAbility")!).Name : null,
            SpellListClassId = SpellListClass(subclass) is { } className ? Slug.Of(className) + Suffix(edition) : null,
            SpellProgression = casts ? SpellProgression(subclass, subclass["subclassTableGroups"]) : [],
            SpellSchools = schoolLimit?.Schools ?? [],
            AnySchoolSpellLevels = schoolLimit?.AnySchoolLevels ?? [],
            UnarmoredArmorClass = UnarmoredArmorClassIn(features, subclassId, fromLevel: classRow.SubclassLevel)
        };
    }

    /// <summary>
    /// The class whose whole list a subclass opens up, which 5etools writes as expanded spells
    /// "all" of a filter: "level=1|class=Wizard" for the Eldritch Knight at level 3.
    /// </summary>
    private static string? SpellListClass(JsonObject subclass) =>
        subclass.Objects("additionalSpells")
            .SelectMany(block => block["expanded"] as JsonObject ?? [])
            .SelectMany(kvp => kvp.Value as JsonArray ?? [])
            .Select(item => item.Str("all"))
            .OfType<string>()
            .Select(filter => ClassFilter().Match(filter))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .FirstOrDefault();

    private List<FeatureSeedRow> ClassFeatureRows(JsonObject @class, string classId)
    {
        var rows = new List<FeatureSeedRow>();
        var subclassChoiceSeen = false;

        foreach (var reference in ClassFeatureReferences(@class))
        {
            // The first marks where the subclass is chosen and says so; the later ones only
            // say "you gain a feature from your subclass", which the subclass's rows show
            if (reference.GainsSubclass)
            {
                if (subclassChoiceSeen)
                {
                    continue;
                }

                subclassChoiceSeen = true;
            }

            var feature = Resolve("classFeature", reference.Reference);

            if (feature == null)
            {
                MissingFeatures.Add($"{classId}: {reference.Reference}");
                continue;
            }

            if (feature.Bool("isClassFeatureVariant"))
            {
                SkippedVariants.Add($"{classId}: {reference.Name}");
                continue;
            }

            rows.Add(FeatureRow(feature, reference.Level, classId, subclassId: null));
        }

        return rows;
    }

    private List<FeatureSeedRow> SubclassFeatureRows(JsonObject subclass, string subclassId)
    {
        var rows = new List<FeatureSeedRow>();

        foreach (var node in subclass.Arr("subclassFeatures"))
        {
            var reference = node.AsString() ?? node.Str("subclassFeature")!;
            var feature = Resolve("subclassFeature", reference);

            if (feature == null)
            {
                MissingFeatures.Add($"{subclassId}: {reference}");
                continue;
            }

            // The first feature is named after the subclass: an introduction, then references
            // to the features gained at that level. Those are the rows.
            var gained = feature.Objects("entries").Where(e => e.Str("type") == "refSubclassFeature").ToList();
            var isIntroduction = gained.Count > 0
                && (feature.Str("name") == subclass.Str("name") || feature.Str("name") == subclass.Str("shortName"));

            if (!isIntroduction)
            {
                AddSubclassFeature(rows, feature, subclassId);
                continue;
            }

            foreach (var inner in gained)
            {
                var innerReference = inner.Str("subclassFeature")!;
                var innerFeature = Resolve("subclassFeature", innerReference);

                if (innerFeature == null)
                {
                    MissingFeatures.Add($"{subclassId}: {innerReference}");
                    continue;
                }

                AddSubclassFeature(rows, innerFeature, subclassId);
            }
        }

        return rows;
    }

    private void AddSubclassFeature(List<FeatureSeedRow> rows, JsonObject feature, string subclassId)
    {
        if (feature.Bool("isClassFeatureVariant"))
        {
            SkippedVariants.Add($"{subclassId}: {feature.Str("name")}");
            return;
        }

        rows.Add(FeatureRow(feature, feature.Int("level") ?? 0, classId: null, subclassId));
    }

    private FeatureSeedRow FeatureRow(JsonObject feature, int level, string? classId, string? subclassId)
    {
        var name = FiveEToolsMarkup.ToPlainText(feature.Str("name")!);
        var id = $"{classId ?? subclassId}-{level}-{Slug.Of(name)}";
        var bookText = Renderer.Render(feature["entries"]);

        // Kept for rules read from the text (a subclass's school limit) even when the seed file
        // may not carry it
        _bookTextByFeatureId[id] = bookText;

        return new FeatureSeedRow
        {
            Id = id,
            Name = name,
            Description = SrdText.Describe(id, feature, () => bookText),
            Level = level,
            ClassId = classId,
            SubclassId = subclassId,
            Type = subclassId == null ? "class" : "subclass"
        };
    }

    private sealed record FeatureReference(string Reference, bool GainsSubclass)
    {
        public string Name => Reference.Split('|')[0];

        public int Level => int.Parse(Reference.Split('|')[3], CultureInfo.InvariantCulture);
    }

    private static IEnumerable<FeatureReference> ClassFeatureReferences(JsonObject @class) =>
        @class.Arr("classFeatures").Select(node => node.AsString() is { } reference
            ? new FeatureReference(reference, GainsSubclass: false)
            : new FeatureReference(node.Str("classFeature")!, node.Bool("gainSubclassFeature")));

    /// <summary>
    /// Finds the feature a reference names. References leave out what is the default: an empty
    /// class or subclass source means PHB, and a missing feature source means its owner's.
    /// </summary>
    private JsonObject? Resolve(string kind, string reference)
    {
        var parts = reference.Split('|');
        string Part(int index, string fallback) => index < parts.Length && parts[index].Length > 0 ? parts[index] : fallback;

        if (kind == "classFeature")
        {
            var classSource = Part(2, "PHB");
            return _classFeatures.GetValueOrDefault(Key(parts[0], Part(1, string.Empty), classSource, Part(3, string.Empty), Part(4, classSource)));
        }

        var subclassSource = Part(4, "PHB");

        return _subclassFeatures.GetValueOrDefault(Key(
            parts[0], Part(1, string.Empty), Part(2, "PHB"), Part(3, string.Empty), subclassSource, Part(5, string.Empty), Part(6, subclassSource)));
    }

    private static string ClassFeatureKey(JsonObject feature) =>
        Key(feature.Str("name"), feature.Str("className"), feature.Str("classSource"), Level(feature), feature.Str("source"));

    private static string SubclassFeatureKey(JsonObject feature) =>
        Key(feature.Str("name"), feature.Str("className"), feature.Str("classSource"), feature.Str("subclassShortName"),
            feature.Str("subclassSource"), Level(feature), feature.Str("source"));

    private static string? Level(JsonObject feature) => feature.Int("level")?.ToString(CultureInfo.InvariantCulture);

    private static string Key(params string?[] parts) => string.Join('|', parts.Select(p => p?.Trim() ?? string.Empty));

    [GeneratedRegex(@"level=(\d)")]
    private static partial Regex SlotLevel();

    [GeneratedRegex(@"class=([A-Za-z]+)")]
    private static partial Regex ClassFilter();
}

/// <summary>What the class files give for one edition. SubclassData keeps each subclass's raw
/// 5etools entry by id, for the spells it grants.</summary>
internal sealed record ClassImport(
    List<ClassSeedRow> Classes,
    List<SubclassSeedRow> Subclasses,
    List<FeatureSeedRow> Features,
    Dictionary<string, JsonObject> SubclassData);
