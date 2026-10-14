using System.Globalization;
using System.Text.Json.Nodes;

using CharacterCreatorApp.Infrastructure.Seed.Rows;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Reads every 5etools spell file into Spells.json, the class spell lists into SpellClasses.json,
/// and what subclasses give into SpellSubclasses.json. A spell reprinted for the 2024 rules is a
/// second spell ("fireball_2024" next to "fireball"); a spell only the older books have, such as
/// Toll the Dead, is one spell that 5etools puts on both editions' class lists.
/// </summary>
internal sealed class SpellImporter(FiveEToolsSource source, EntryRenderer renderer)
{
    /// <summary>
    /// Books written for the 2024 rules. Their spells get "_2024" ids, so the ids 2014 characters
    /// already store never change.
    /// </summary>
    private static readonly HashSet<string> Sources2024 = new(StringComparer.OrdinalIgnoreCase) { "XPHB", "FRHoF", "EFA", "AU" };

    private static readonly Dictionary<string, string> Schools = new()
    {
        ["A"] = "Abjuration",
        ["C"] = "Conjuration",
        ["D"] = "Divination",
        ["E"] = "Enchantment",
        ["V"] = "Evocation",
        ["I"] = "Illusion",
        ["N"] = "Necromancy",
        ["T"] = "Transmutation",
        ["P"] = "Psionic",
    };

    private readonly FiveEToolsSource _source = source;
    private readonly EntryRenderer _renderer = renderer;

    /// <summary>Class list entries naming a class the app does not have.</summary>
    public SortedSet<string> UnknownClasses { get; } = [];

    /// <summary>Spells a subclass names that are not in the spell files.</summary>
    public SortedSet<string> UnresolvedSpells { get; } = [];

    /// <summary>Subclass options (a Circle of the Land terrain) whose spells depend on a choice the app does not ask for.</summary>
    public SortedSet<string> SkippedChoices { get; } = [];

    /// <summary>How many "choose a spell" grants were left out, for the same reason.</summary>
    public int SkippedPicks { get; private set; }

    public SpellImport Import(IReadOnlyList<ClassImport> classImports)
    {
        var spells = _source.SpellFiles().SelectMany(file => file.Objects("spell")).ToList();

        var idsByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var spell in spells)
        {
            idsByKey[Key(spell.Str("name")!, spell.Str("source")!)] = IdOf(spell);
        }

        var rows = spells
            .Select(spell => Row(spell, idsByKey[Key(spell.Str("name")!, spell.Str("source")!)]))
            .OrderBy(s => s.Name, StringComparer.Ordinal)
            .ThenBy(s => s.Id, StringComparer.Ordinal)
            .ToList();

        var classIds = classImports
            .SelectMany(i => i.Classes)
            .ToDictionary(c => (c.Name, c.Edition), c => c.Id);

        var classRows = spells
            .SelectMany(spell => ClassListRows(spell, idsByKey[Key(spell.Str("name")!, spell.Str("source")!)], classIds))
            .DistinctBy(r => (r.SpellId, r.ClassId))
            .ToList();

        var subclassRows = classImports
            .SelectMany(i => i.SubclassData)
            .SelectMany(kvp => SubclassRows(kvp.Key, kvp.Value, spells, idsByKey))
            .GroupBy(r => (r.SpellId, r.SubclassId))
            .Select(g => g.OrderByDescending(r => r.IsAlwaysPrepared).ThenBy(r => r.Level).First())
            .ToList();

        return new SpellImport(rows, classRows, subclassRows, idsByKey);
    }

    /// <summary>The lookup key for a spell by name and book, which is how 5etools refers to one.</summary>
    public static string Key(string name, string source) => $"{name}|{source}";

    private static string IdOf(JsonObject spell) =>
        Slug.Of(spell.Str("name")!, '_') + (Sources2024.Contains(spell.Str("source")!) ? "_2024" : string.Empty);

    private SpellSeedRow Row(JsonObject spell, string id)
    {
        var time = spell.Objects("time").FirstOrDefault();
        var range = spell["range"];
        var distance = range?["distance"];
        var components = spell["components"];
        var material = components?["m"];
        var duration = spell.Objects("duration").FirstOrDefault();
        var school = spell.Str("school") ?? string.Empty;

        return new SpellSeedRow
        {
            // The id keeps the book's name, so the ids characters store never change
            Id = id,
            Name = SrdText.SrdName(spell) ?? spell.Str("name")!,
            Level = spell.Int("level") ?? 0,
            School = Schools.GetValueOrDefault(school, school),
            CastingTimeNumber = time.Int("number") ?? 1,
            CastingTimeUnit = time.Str("unit") ?? "action",
            RangeType = range.Str("type") ?? "special",
            RangeDistanceType = distance.Str("type"),
            RangeDistanceAmount = distance.Int("amount"),
            HasVerbalComponent = components.Bool("v"),
            HasSomaticComponent = components.Bool("s"),

            // "m" is the description, an object with it and a cost, or true for one without. The
            // wording is the book's, so only an SRD spell keeps it; the others say only that there is one
            HasMaterialComponent = material != null,
            Material = SrdText.IsSrd(spell) && (material.AsString() ?? material.Str("text")) is { } text ? FiveEToolsMarkup.ToPlainText(text) : null,
            DurationType = duration.Str("type") ?? "special",
            DurationAmount = duration?["duration"].Int("amount"),
            DurationUnit = duration?["duration"].Str("type"),
            IsConcentration = duration.Bool("concentration"),
            IsRitual = spell["meta"].Bool("ritual"),
            Description = SrdText.Describe(id, spell, () => Description(spell), SrdText.SpellMechanics(spell))
        };
    }

    // The spell, then what casting it at a higher level or a higher character level adds
    private string Description(JsonObject spell)
    {
        var text = _renderer.Render(spell["entries"]);
        var higher = _renderer.Render(spell["entriesHigherLevel"]);

        return higher.Length == 0 ? text : $"{text}\n\n{higher}";
    }

    /// <summary>
    /// The classes whose list has the spell, from 5etools' index of spell lists: "class" for the
    /// book's own lists and "classVariant" for lists another book adds it to (Xanathar's spells
    /// are on the class lists only that way).
    /// </summary>
    private IEnumerable<SpellClassSeedRow> ClassListRows(JsonObject spell, string spellId, Dictionary<(string Name, string Edition), string> classIds)
    {
        var index = _source.Load(Path.Combine("spells", "sources.json"))[spell.Str("source")!]?[spell.Str("name")!];

        foreach (var entry in index.Objects("class").Concat(index.Objects("classVariant")))
        {
            var name = entry.Str("name")!;
            var classSource = entry.Str("source")!;
            var edition = classSource switch
            {
                "PHB" or "TCE" => "2014",
                "XPHB" or "EFA" => "2024",
                _ => null,
            };

            if (edition != null && classIds.TryGetValue((name, edition), out var classId))
            {
                yield return new SpellClassSeedRow { SpellId = spellId, ClassId = classId };
            }
            else
            {
                UnknownClasses.Add($"{name}|{classSource}");
            }
        }
    }

    /// <summary>
    /// What a subclass gives: spells it always has prepared or known ("prepared", "known",
    /// "innate"), and spells it adds to what its class may pick ("expanded"). A block with a
    /// name is one option of a choice, such as a Circle of the Land terrain, which is skipped.
    /// </summary>
    private IEnumerable<SpellSubclassSeedRow> SubclassRows(
        string subclassId, JsonObject subclass, List<JsonObject> spells, Dictionary<string, string> idsByKey)
    {
        foreach (var block in subclass.Objects("additionalSpells"))
        {
            if (block.Str("name") is { } option)
            {
                SkippedChoices.Add($"{subclassId}: {option}");
                continue;
            }

            foreach (var kind in new[] { "prepared", "known", "innate", "expanded" })
            {
                var alwaysPrepared = kind != "expanded";

                foreach (var (levelKey, value) in block[kind] as JsonObject ?? [])
                {
                    // "3" is a character level. "s1" is a spell level, open as soon as it can be
                    // cast, and "_" means from the start.
                    var level = int.TryParse(levelKey, NumberStyles.None, CultureInfo.InvariantCulture, out var characterLevel) ? characterLevel : 1;

                    foreach (var reference in AdditionalSpells.References(value, () => SkippedPicks++))
                    {
                        if (reference.StartsWith("source=", StringComparison.Ordinal))
                        {
                            // Every spell of a book: Chronurgy and Graviturgy add Explorer's
                            // Guide to Wildemount's to the wizard list
                            var book = reference["source=".Length..];

                            foreach (var spell in spells.Where(s => string.Equals(s.Str("source"), book, StringComparison.OrdinalIgnoreCase)))
                            {
                                yield return Grant(idsByKey[Key(spell.Str("name")!, spell.Str("source")!)], subclassId, level, alwaysPrepared);
                            }

                            continue;
                        }

                        if (AdditionalSpells.IsFilter(reference))
                        {
                            // A whole class list ("level=1|class=Wizard"), which SpellListClassId covers
                            continue;
                        }

                        var (name, spellSource) = AdditionalSpells.Parse(reference);

                        if (idsByKey.TryGetValue(Key(name, spellSource), out var spellId))
                        {
                            yield return Grant(spellId, subclassId, level, alwaysPrepared);
                        }
                        else
                        {
                            UnresolvedSpells.Add($"{subclassId}: {reference}");
                        }
                    }
                }
            }
        }
    }

    private static SpellSubclassSeedRow Grant(string spellId, string subclassId, int level, bool alwaysPrepared) =>
        new() { SpellId = spellId, SubclassId = subclassId, Level = level, IsAlwaysPrepared = alwaysPrepared };
}

/// <summary>What the spell files give. IdsByKey finds a spell's id by <see cref="SpellImporter.Key"/>.</summary>
internal sealed record SpellImport(
    List<SpellSeedRow> Spells,
    List<SpellClassSeedRow> SpellClasses,
    List<SpellSubclassSeedRow> SpellSubclasses,
    IReadOnlyDictionary<string, string> IdsByKey);
