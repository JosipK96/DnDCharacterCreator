using CharacterCreatorApp.Domain.ValueObjects;
using CharacterCreatorApp.SeedDataGenerator;

// Turns the raw 5etools data into the seed files the app loads on startup:
//
//   dotnet run --project tools/CharacterCreatorApp.SeedDataGenerator -- <5etools data folder> [<Data folder>]
//
// The Data folder defaults to src/CharacterCreatorApp.Infrastructure/Data in this repository. The
// 5etools data itself stays outside the repository; only what this writes is committed.

if (args.Length is < 1 or > 2)
{
    Console.Error.WriteLine("Usage: CharacterCreatorApp.SeedDataGenerator <5etools data folder> [<Data folder>]");
    return 2;
}

var source = new FiveEToolsSource(args[0]);
var dataPath = args.Length == 2 ? args[1] : DefaultDataPath();

Console.WriteLine($"Reading {args[0]}");
Console.WriteLine($"Writing {dataPath}");

await SeedWriter.WriteAsync(dataPath, "Languages.json", new LanguageImporter(source).Import(), l => l.Id);

var proficiencies = await ProficiencyIndex.LoadAsync(dataPath);
var classImporter = new ClassImporter(source, proficiencies);
var classes = RulesEdition.All.Select(classImporter.Import).ToList();

await SeedWriter.WriteAsync(dataPath, "Classes.json", classes.SelectMany(c => c.Classes).ToList(), c => c.Id);
await SeedWriter.WriteAsync(dataPath, "Subclasses.json", classes.SelectMany(c => c.Subclasses).ToList(), s => s.Id);
await SeedWriter.WriteAsync(dataPath, "ClassFeatures.json", classes.SelectMany(c => c.Features).ToList(), f => f.Id);

var spellImporter = new SpellImporter(source, classImporter.Renderer);
var spells = spellImporter.Import(classes);

await SeedWriter.WriteAsync(dataPath, Path.Combine("Spells", "Spells.json"), spells.Spells, s => s.Id);
await SeedWriter.WriteAsync(dataPath, Path.Combine("Spells", "SpellClasses.json"), spells.SpellClasses, r => $"{r.SpellId} {r.ClassId}");
await SeedWriter.WriteAsync(dataPath, Path.Combine("Spells", "SpellSubclasses.json"), spells.SpellSubclasses, r => $"{r.SpellId} {r.SubclassId}");

var featImporter = new FeatImporter(source, classImporter.Renderer, proficiencies);
var feats = featImporter.Import(spells.IdsByKey);

await SeedWriter.WriteAsync(dataPath, "Feats.json", feats, f => f.Id);

var raceImporter = new RaceImporter(source, classImporter.Renderer, proficiencies);
var races = raceImporter.Import(spells.IdsByKey);

await SeedWriter.WriteAsync(dataPath, "Races.json", races, r => r.Id, r => r.Subraces.Select(s => s.Id));

var backgroundImporter = new BackgroundImporter(source, classImporter.Renderer, proficiencies);
var backgrounds = backgroundImporter.Import(featImporter.IdsByKey);

await SeedWriter.WriteAsync(dataPath, "Backgrounds.json", backgrounds.Backgrounds, b => b.Id);
await SeedWriter.WriteAsync(dataPath, "BackgroundFeatures.json", backgrounds.Features, f => f.Id);

// Which rows kept book text because it is in the SRD; every other description is mechanics only
await SeedWriter.WriteAsync(dataPath, "SrdIds.json", SrdText.KeptIds.ToList(), id => id);

var problems = new List<string>();
Warn("Proficiency names with no id in Proficiencies.json", proficiencies.Unknown, problems);
Warn("Features a class or subclass lists that are not in the data", classImporter.MissingFeatures, problems);
Warn("References to features that are not in the data", classImporter.Renderer.UnresolvedReferences, problems);
Warn("Spells a subclass gives that are not in the spell files", spellImporter.UnresolvedSpells, problems);
Warn("Spells a feat gives that are not in the spell files", featImporter.UnresolvedSpells, problems);
Warn("Spells a race gives that are not in the spell files", raceImporter.UnresolvedSpells, problems);
Warn("Race choices left out, since the app does not ask for them yet", raceImporter.SkippedChoices, problems: null);
Warn("Feats a background names that are not in the feat file", backgroundImporter.UnresolvedFeats, problems);
Warn("Background choices left out, since the app does not ask for them yet", backgroundImporter.SkippedChoices, problems: null);
Warn("Feat categories without a name", featImporter.UnknownCategories, problems: null);
Warn("Feat ability increases left out, since one choice cannot hold them", featImporter.SkippedAbilityIncreases, problems: null);
Warn("Class lists naming a class the app does not have", spellImporter.UnknownClasses, problems: null);
Warn("Entry types shown as plain entries", classImporter.Renderer.UnknownTypes, problems: null);
Warn("Tasha's optional class features left out", classImporter.SkippedVariants, problems: null);
Warn("Subclass spell options left out, since the app does not ask which one", spellImporter.SkippedChoices, problems: null);
Console.WriteLine($"Spell picks left out: {spellImporter.SkippedPicks} from subclasses, {featImporter.SkippedPicks} from feats, {raceImporter.SkippedPicks} from races");

return problems.Count == 0 ? 0 : 1;

static void Warn(string title, IReadOnlyCollection<string> items, List<string>? problems)
{
    if (items.Count == 0)
    {
        return;
    }

    Console.WriteLine();
    Console.WriteLine($"{title} ({items.Count}):");

    foreach (var item in items)
    {
        Console.WriteLine($"  {item}");
    }

    problems?.Add(title);
}

// The Infrastructure project's Data folder, found from wherever this runs inside the repository
static string DefaultDataPath()
{
    for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
    {
        if (File.Exists(Path.Combine(directory.FullName, "CharacterCreatorApp.sln")))
        {
            return Path.Combine(directory.FullName, "src", "CharacterCreatorApp.Infrastructure", "Data");
        }
    }

    throw new InvalidOperationException("Not inside the repository; pass the Data folder as the second argument.");
}
