using CharacterCreatorApp.Domain.DomainModels.Proficiencies;
using CharacterCreatorApp.Domain.Services;
using CharacterCreatorApp.Domain.ValueObjects;
using CharacterCreatorApp.Infrastructure.Seed;
using CharacterCreatorApp.Infrastructure.Seed.Rows;

using FluentAssertions;

namespace CharacterCreatorApp.Infrastructure.IntegrationTests.Seed;

/// <summary>
/// Checks the seed files against each other, for the references no foreign key guards: the id
/// lists inside feats, and ids that only differ by spelling. The data is generated from 5etools,
/// and one pass left thirteen proficiencies under two ids ("thieves_tools" and "thieves-tools"),
/// so a character could list Thieves' Tools twice, and a feat offering "vehicles_land", which
/// nothing defined. No database is involved.
/// </summary>
public partial class SeedDataIntegrityTests
{
    private static readonly string DataPath = SeedService.DataPath;

    [Fact]
    public async Task OnlySrdEntries_CarryTheBooksText()
    {
        // Arrange - only the SRD's text may be published (CC-BY 4.0). Every other spell, feature,
        // feat, background feature and race trait is at most one line of mechanics and where the
        // book has it, so text copied from a book fails here even when its id looks right
        var srdIds = (await ReadAsync<string>("SrdIds.json")).ToHashSet();
        var races = await ReadAsync<RaceSeedRow>("Races.json");
        var traits = races.SelectMany(r => r.Traits.Concat(r.Subraces.SelectMany(s => s.Traits)))
            .Select(t => new DescribedRow(t.Id, t.Description));
        var rows = (await ReadAsync<DescribedRow>(Path.Combine("Spells", "Spells.json")))
            .Concat(await ReadAsync<DescribedRow>("ClassFeatures.json"))
            .Concat(await ReadAsync<DescribedRow>("Feats.json"))
            .Concat(await ReadAsync<DescribedRow>("BackgroundFeatures.json"))
            .Concat(traits);

        var spellMaterials = await ReadAsync<SpellMaterialRow>(Path.Combine("Spells", "Spells.json"));

        // Act
        var withBookText = rows
            .Where(r => !srdIds.Contains(r.Id) && !MechanicsAndReference().IsMatch(r.Description))
            .Select(r => r.Id)
            .Concat(spellMaterials.Where(s => !srdIds.Contains(s.Id) && s.Material != null).Select(s => $"{s.Id} (material)"))
            .ToList();

        // Assert
        withBookText.Should().BeEmpty();
    }

    [Fact]
    public async Task NoSeedFile_HoldsFiveEToolsMarkup()
    {
        // Arrange - seeding reads the files as they are, so the generator has to have taken
        // "{@damage 1d8}" and the rest out
        var files = Directory.EnumerateFiles(DataPath, "*.json", SearchOption.AllDirectories);

        // Act
        var withMarkup = new List<string>();

        foreach (var file in files)
        {
            if ((await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken)).Contains("{@", StringComparison.Ordinal))
            {
                withMarkup.Add(Path.GetRelativePath(DataPath, file));
            }
        }

        // Assert
        withMarkup.Should().BeEmpty();
    }

    [Fact]
    public async Task Proficiencies_HaveOneIdPerName()
    {
        // Act
        var proficiencies = await ReadAsync<ProficiencyRow>("Proficiencies.json");

        // Assert
        proficiencies.GroupBy(p => p.Id).Where(g => g.Count() > 1).Select(g => g.Key).Should().BeEmpty();
        proficiencies.GroupBy(p => p.Name).Where(g => g.Count() > 1).Select(g => g.Key).Should().BeEmpty();
    }

    [Fact]
    public async Task TheSkillsTheSheetKnows_AreExactlyTheSeededSkillProficiencies()
    {
        // Act - Skill.All decides what counts as a skill on the sheet and in a race's skill
        // choice, so a skill the data spelled differently would never count as proficient
        var seededSkills = (await ReadAsync<ProficiencyRow>("Proficiencies.json"))
            .Where(p => p.Type == "skill")
            .Select(p => p.Id);

        // Assert
        seededSkills.Should().BeEquivalentTo(Skill.All.Select(s => s.Id));
    }

    [Fact]
    public async Task EveryProficiencyAFileNames_IsDefined()
    {
        // Arrange
        var defined = (await ReadAsync<ProficiencyRow>("Proficiencies.json")).Select(p => p.Id).ToHashSet();
        var classes = await ReadAsync<ClassRow>("Classes.json");
        var backgrounds = await ReadAsync<BackgroundRow>("Backgrounds.json");
        var races = await ReadAsync<RaceSeedRow>("Races.json");
        var feats = await ReadAsync<FeatRow>("Feats.json");

        // Act
        var references = classes.SelectMany(c => (c.ProficiencyIds ?? []).Concat(c.ProficiencyChoices?.Options ?? []).Concat(ToolOptions(c.ToolChoices)).Select(id => $"class {c.Id}: {id}"))
            .Concat(backgrounds.SelectMany(b => (b.ProficiencyIds ?? []).Concat(ToolOptions(b.ToolChoices)).Select(id => $"background {b.Id}: {id}")))
            .Concat(races.SelectMany(r => r.ProficiencyIds.Concat(r.SkillChoiceOptions).Concat(ToolOptions(r.ToolChoices)).Select(id => $"race {r.Id}: {id}")))
            .Concat(races.SelectMany(r => r.Subraces).SelectMany(s => s.ProficiencyIds.Select(id => $"subrace {s.Id}: {id}")))
            .Concat(feats.SelectMany(f => (f.GrantedProficiencyIds ?? []).Concat(ProficiencyOptions(f)).Select(id => $"feat {f.Id}: {id}")));

        var undefined = references.Where(r => !defined.Contains(r[(r.IndexOf(": ", StringComparison.Ordinal) + 2)..]));

        // Assert
        undefined.Should().BeEmpty("every proficiency a seed file names has to be in Proficiencies.json");
    }

    [Fact]
    public async Task EverySpellAndLanguageAFeatGrants_Exists()
    {
        // Arrange
        var spellIds = (await ReadAsync<SpellRow>(Path.Combine("Spells", "Spells.json"))).Select(s => s.Id).ToHashSet();
        var languages = await ReadAsync<LanguageRow>("Languages.json");
        var feats = await ReadAsync<FeatRow>("Feats.json");

        // Feats name languages by id or by PHB name, the way LanguageResolver reads them
        var languageKeys = languages.Select(l => l.Id)
            .Concat(languages.Where(l => l.Source == "PHB").Select(l => l.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Act
        var missingSpells = feats.SelectMany(f => (f.GrantedSpellIds ?? []).Where(id => !spellIds.Contains(id)).Select(id => $"feat {f.Id}: {id}"));
        var missingLanguages = feats.SelectMany(f => (f.GrantedLanguageIds ?? []).Where(id => !languageKeys.Contains(id)).Select(id => $"feat {f.Id}: {id}"));

        // Assert
        missingSpells.Should().BeEmpty();
        missingLanguages.Should().BeEmpty();
    }

    [Fact]
    public async Task EveryClassAndFeatureAFeatRequires_Exists()
    {
        // Arrange - a feat asking for a class or feature nobody has could never be taken:
        // the Fighting Style feats look for the feature by the name the class data gives it
        var classIds = (await ReadAsync<IdRow>("Classes.json")).Select(c => c.Id).ToHashSet();
        var featureNames = (await ReadAsync<FeatureNameRow>("ClassFeatures.json")).Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var feats = await ReadAsync<FeatRow>("Feats.json");

        // Act
        var missingClasses = feats.SelectMany(f => (f.Prerequisites?.ClassIds ?? []).Where(id => !classIds.Contains(id)).Select(id => $"feat {f.Id}: {id}"));
        var missingFeatures = feats.SelectMany(f => (f.Prerequisites?.Features ?? []).Where(name => !featureNames.Contains(name)).Select(name => $"feat {f.Id}: {name}"));

        // Assert
        missingClasses.Should().BeEmpty();
        missingFeatures.Should().BeEmpty();
    }

    [Fact]
    public async Task EveryFeatAndFeatureABackgroundNames_Exists()
    {
        // Arrange - a background's origin feat and its 2014 feature are keys the database checks
        // on seeding; this says which one is missing before that
        var featIds = (await ReadAsync<IdRow>("Feats.json")).Select(f => f.Id).ToHashSet();
        var featureIds = (await ReadAsync<IdRow>("BackgroundFeatures.json")).Select(f => f.Id).ToHashSet();
        var backgrounds = await ReadAsync<BackgroundSeedRow>("Backgrounds.json");

        // Act
        var missingFeats = backgrounds.Where(b => b.FeatId != null && !featIds.Contains(b.FeatId)).Select(b => $"background {b.Id}: {b.FeatId}");
        var missingFeatures = backgrounds.Where(b => b.FeatureId != null && !featureIds.Contains(b.FeatureId)).Select(b => $"background {b.Id}: {b.FeatureId}");

        // Assert
        missingFeats.Should().BeEmpty();
        missingFeatures.Should().BeEmpty();
    }

    [Fact]
    public async Task EveryBackgroundsAbilityOptions_AreThreeCanonicalAbilities()
    {
        // Arrange
        var backgrounds = await ReadAsync<BackgroundSeedRow>("Backgrounds.json");

        // Act - a 2024 background raises three; the 2014 ones raise none
        var wrong = backgrounds
            .Where(b => b.Edition == RulesEdition.Edition2024
                ? b.AbilityScoreOptions.Count != 3 || !b.AbilityScoreOptions.TrueForAll(IsCanonicalAbilityId)
                : b.AbilityScoreOptions.Count != 0)
            .Select(b => $"{b.Id}: {string.Join(", ", b.AbilityScoreOptions)}");

        // Assert
        wrong.Should().BeEmpty();
    }

    [Fact]
    public async Task EverySpellARaceGrants_Exists()
    {
        // Arrange
        var spellIds = (await ReadAsync<SpellRow>(Path.Combine("Spells", "Spells.json"))).Select(s => s.Id).ToHashSet();
        var races = await ReadAsync<RaceSeedRow>("Races.json");

        // Act
        var missing = races.SelectMany(r => r.Spells.Select(s => $"race {r.Id}: {s.SpellId}"))
            .Concat(races.SelectMany(r => r.Subraces).SelectMany(s => s.Spells.Select(spell => $"subrace {s.Id}: {spell.SpellId}")))
            .Where(r => !spellIds.Contains(r[(r.IndexOf(": ", StringComparison.Ordinal) + 2)..]));

        // Assert
        missing.Should().BeEmpty();
    }

    [Fact]
    public async Task TheIdsHitPointBonusesNames_AreInTheSeedData()
    {
        // Arrange - the domain names these ids to add hit points; renamed data would drop the bonus
        var races = await ReadAsync<RaceSeedRow>("Races.json");
        var subclasses = await ReadAsync<IdRow>("Subclasses.json");
        var feats = await ReadAsync<IdRow>("Feats.json");

        // Act & Assert
        races.Select(r => r.Id).Should().Contain(HitPointBonuses.Dwarf2024RaceId);
        races.SelectMany(r => r.Subraces).Select(s => s.Id).Should().Contain(HitPointBonuses.HillDwarfSubraceId);
        subclasses.Select(s => s.Id).Should().Contain(HitPointBonuses.DraconicSorcererSubclassIds);
        feats.Select(f => f.Id).Should().Contain(HitPointBonuses.ToughFeatIds);
    }

    [Fact]
    public async Task FeatsInBothEditions_HaveTheirEditionInTheirIdAndOneNamePerEdition()
    {
        // Arrange - "alert_2014" and "alert_2024" are both named "Alert": the id has to agree
        // with the edition, and the player, who sees one edition's feats, must not see a name twice
        var feats = await ReadAsync<FeatEditionRow>("Feats.json");

        // Act
        var mismatched = feats
            .Where(f => RulesEdition.All.Any(e => f.Id.EndsWith($"_{e}", StringComparison.Ordinal)) && !f.Id.EndsWith($"_{f.Edition}", StringComparison.Ordinal))
            .Select(f => f.Id);
        var repeatedNames = Repeated(feats.Select(f => $"{f.Edition} {f.Name}"));

        // Assert
        mismatched.Should().BeEmpty();
        repeatedNames.Should().BeEmpty();
    }

    [Fact]
    public async Task RaceAndSubraceRows_AreNotRepeated()
    {
        // Act - a repeated row breaks its table's key, and seeding with it; a subrace id is a
        // character's key, so it may not appear under two races either
        var races = await ReadAsync<RaceSeedRow>("Races.json");
        var subraces = races.SelectMany(r => r.Subraces).ToList();

        // Assert
        Repeated(subraces.Select(s => s.Id)).Should().BeEmpty();
        Repeated(races.SelectMany(r => r.ProficiencyIds.Select(id => $"{r.Id} {id}"))).Should().BeEmpty();
        Repeated(subraces.SelectMany(s => s.ProficiencyIds.Select(id => $"{s.Id} {id}"))).Should().BeEmpty();
        Repeated(races.SelectMany(r => r.Languages.Select(language => $"{r.Id} {language}"))).Should().BeEmpty();
        Repeated(races.SelectMany(r => r.Traits).Concat(subraces.SelectMany(s => s.Traits)).Select(t => t.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task EverySourceAFileNames_IsABookOfTheRowsEdition()
    {
        // Arrange - the book filter offers what Books.json lists, so a row from a book missing
        // there could never be allowed, and one filed under the other edition's book never shown
        var books = (await ReadAsync<BookSeedRow>("Books.json")).ToDictionary(b => b.Code, b => b.Edition);
        var races = await ReadAsync<RaceSeedRow>("Races.json");
        var classes = await ReadAsync<ClassSeedRow>("Classes.json");
        var classEditions = classes.ToDictionary(c => c.Id, c => c.Edition);

        var rows = races.Select(r => (Row: $"race {r.Id}", r.Source, Edition: (string?)r.Edition))
            .Concat(races.SelectMany(r => r.Subraces.Select(s => (Row: $"subrace {s.Id}", s.Source, Edition: (string?)r.Edition))))
            .Concat(classes.Select(c => (Row: $"class {c.Id}", c.Source, Edition: (string?)c.Edition)))
            .Concat((await ReadAsync<SubclassSeedRow>("Subclasses.json")).Select(s => (Row: $"subclass {s.Id}", s.Source, Edition: (string?)classEditions[s.ClassId])))
            .Concat((await ReadAsync<BackgroundSeedRow>("Backgrounds.json")).Select(b => (Row: $"background {b.Id}", b.Source, Edition: (string?)b.Edition)))
            .Concat((await ReadAsync<FeatSeedRow>("Feats.json")).Select(f => (Row: $"feat {f.Id}", f.Source, Edition: (string?)f.Edition)))
            .Concat((await ReadAsync<SpellSeedRow>(Path.Combine("Spells", "Spells.json"))).Select(s => (Row: $"spell {s.Id}", s.Source, Edition: (string?)null)));

        // Act - a spell row has no edition of its own, so only its book is checked
        var wrong = rows
            .Where(r => !books.TryGetValue(r.Source, out var edition) || (r.Edition != null && r.Edition != edition))
            .Select(r => $"{r.Row}: {r.Source}")
            .ToList();

        // Assert
        wrong.Should().BeEmpty();
    }

    [Fact]
    public async Task Books_HaveOnePlayersHandbookPerEdition_TheOneEveryDraftAllows()
    {
        // Act
        var handbooks = (await ReadAsync<BookSeedRow>("Books.json")).Where(b => b.IsPlayersHandbook);

        // Assert - RulesEdition.PlayersHandbook is what a new draft allows without reading the file
        handbooks.Select(b => (b.Edition, b.Code)).Should().BeEquivalentTo(
            RulesEdition.All.Select(edition => (edition, RulesEdition.PlayersHandbook(edition))));
    }

    // Choices of skills and tools list proficiency ids; an ability score choice lists abilities
    private static List<string> ProficiencyOptions(FeatRow feat) =>
        feat.Choice is { Type: "skills" or "skills_tools" } choice ? choice.Options ?? [] : [];

    private static bool IsCanonicalAbilityId(string id) => AbilityScore.TryNormalize(id, out var abilityId) && abilityId == id;

    private static IEnumerable<string> Repeated(IEnumerable<string> keys) =>
        keys.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => g.Key);

    private static IEnumerable<string> ToolOptions(List<ToolChoice>? choices) => (choices ?? []).SelectMany(c => c.Options);

    private static Task<List<T>> ReadAsync<T>(string relativePath) =>
        SeedFile.ReadListAsync<T>(Path.Combine(DataPath, relativePath), TestContext.Current.CancellationToken);

    private sealed record ProficiencyRow(string Id, string Name, string Type);

    private sealed record ClassRow(string Id, List<string>? ProficiencyIds, ProficiencyChoiceRow? ProficiencyChoices, List<ToolChoice>? ToolChoices);

    private sealed record ProficiencyChoiceRow(List<string>? Options);

    private sealed record BackgroundRow(string Id, List<string>? ProficiencyIds, List<ToolChoice>? ToolChoices);

    private sealed record FeatRow(
        string Id,
        List<string>? GrantedProficiencyIds,
        List<string>? GrantedLanguageIds,
        List<string>? GrantedSpellIds,
        FeatChoiceRow? Choice,
        FeatPrerequisitesRow? Prerequisites);

    private sealed record FeatChoiceRow(string Type, List<string>? Options);

    private sealed record FeatPrerequisitesRow(List<string>? ClassIds, List<string>? Features);

    private sealed record FeatureNameRow(string Name);

    private sealed record LanguageRow(string Id, string Name, string Source);

    private sealed record SpellRow(string Id);

    private sealed record IdRow(string Id);

    private sealed record FeatEditionRow(string Id, string Name, string Edition);

    private sealed record DescribedRow(string Id, string Description);

    private sealed record SpellMaterialRow(string Id, string? Material);

    // "Wisdom saving throw. 1d8 necrotic damage.\n\nSee Xanathar's Guide to Everything, page 169."
    [System.Text.RegularExpressions.GeneratedRegex(@"\A([^\n]+\n\n)?See [^\n]+\.\z")]
    private static partial System.Text.RegularExpressions.Regex MechanicsAndReference();
}
