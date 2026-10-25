using CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterSheetPdf;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.Services;
using CharacterCreatorApp.Infrastructure.Pdf;

using FluentAssertions;

using UglyToad.PdfPig;

namespace CharacterCreatorApp.Infrastructure.IntegrationTests.Pdf;

public class CharacterSheetPdfRendererTests
{
    private readonly CharacterSheetPdfRenderer _renderer = new();

    [Theory]
    [InlineData(SheetPaper.A4, 595, 842)]
    [InlineData(SheetPaper.Letter, 612, 792)]
    public void Render_OnEitherPaper_DrawsTheSheetOnPagesOfThatSize(SheetPaper paper, int width, int height)
    {
        // Arrange
        var fighter = Fighter();

        // Act
        var pdf = _renderer.Render(fighter, paper);

        // Assert
        using var document = PdfDocument.Open(pdf);
        document.GetPages().Should().AllSatisfy(page =>
            ((int)Math.Round(page.Width), (int)Math.Round(page.Height)).Should().Be((width, height)));
    }

    [Fact]
    public void Render_ALevelOneFighter_FillsTheFirstPageAndLeavesEquipmentAndAttacksEmpty()
    {
        // Arrange
        var fighter = Fighter();

        // Act
        var pdf = _renderer.Render(fighter, SheetPaper.A4);

        // Assert
        using var document = PdfDocument.Open(pdf);
        var firstPage = WordsOf(document, 1);

        firstPage.Should().Contain(["Bruenor", "Fighter", "1"]);
        firstPage.Should().Contain(["Strength", "+3", "16", "Athletics", "CURRENT", "HIT", "POINTS", "Second", "Wind"]);
        firstPage.Should().Contain(["ATTACKS", "&", "SPELLCASTING", "EQUIPMENT"]);
        firstPage.Should().NotContain("Longsword", "equipment is not modelled, so nothing fills its box");
        document.NumberOfPages.Should().Be(2, "the second page holds the fighter's features in full");
    }

    [Fact]
    public void Render_ALevelTenWizard_RunsOntoMorePagesWithoutSplittingAnEntry()
    {
        // Arrange
        var wizard = Wizard();

        // Act
        var pdf = _renderer.Render(wizard, SheetPaper.A4);

        // Assert
        using var document = PdfDocument.Open(pdf);
        document.NumberOfPages.Should().BeGreaterThan(3);

        var pages = Enumerable.Range(1, document.NumberOfPages).Select(n => WordsOf(document, n)).ToList();

        // Each entry's description ends in a word of its own: the page that has the entry's name
        // after page 1 has that word too
        var entries = wizard.Spells.Select(s => (s.Name, s.Description))
            .Concat(wizard.Features.Select(f => (f.Name, f.Description)));

        foreach (var (name, description) in entries)
        {
            var lastWord = description.Split(' ')[^1];
            var pageWithEnd = pages.FindIndex(1, words => words.Contains(lastWord));

            pageWithEnd.Should().BeGreaterThan(0, $"{name}'s text is on the pages after the first");
            pages[pageWithEnd].Should().Contain(name.Split(' ')[0], $"{name} starts on the page it ends on");
        }
    }

    [Fact]
    public void Render_MoreFeaturesThanTheFirstPageBoxHolds_ShrinksTheListToKeepItOnPageOne()
    {
        // Arrange
        var wizard = Wizard();
        wizard.Features = Enumerable.Range(1, 150).Select(i => Feature($"feature{i}", $"Featurename{i} Arcana", 1, "wizard")).ToList();

        // Act
        var pdf = _renderer.Render(wizard, SheetPaper.Letter);

        // Assert
        using var document = PdfDocument.Open(pdf);
        WordsOf(document, 1).Should().Contain(["Featurename1", "Featurename150", "FEATURES", "EQUIPMENT"]);
    }

    [Fact]
    public void Render_ALevelTenWizard_DrawsATickBoxRowForEachSpellLevelItHasSlotsOf()
    {
        // Arrange
        var wizard = Wizard();

        // Act
        var pdf = _renderer.Render(wizard, SheetPaper.A4);

        // Assert
        using var document = PdfDocument.Open(pdf);
        var text = string.Join(' ', Enumerable.Range(2, document.NumberOfPages - 1).SelectMany(n => WordsOf(document, n)));

        text.Should().Contain("1st level: 4").And.Contain("4th level: 3").And.Contain("5th level: 2");
        text.Should().NotContain("6th level:");
    }

    private static List<string> WordsOf(PdfDocument document, int pageNumber) =>
        document.GetPage(pageNumber).GetWords().Select(w => w.Text).ToList();

    private static CharacterDetailsReadModel Fighter() => Character(
        name: "Bruenor",
        className: "Fighter",
        classId: "fighter",
        level: 1,
        scores: new() { ["STR"] = 16, ["DEX"] = 14, ["CON"] = 15, ["INT"] = 8, ["WIS"] = 12, ["CHA"] = 10 },
        features: [Feature("second_wind", "Second Wind", 1, "fighter"), Feature("fighting_style", "Fighting Style", 1, "fighter")],
        spells: [],
        spellSlots: SpellSlots.None);

    // Long descriptions, so the spells and features take several pages, each ending in a word
    // of its own
    private static CharacterDetailsReadModel Wizard() => Character(
        name: "Elminster",
        className: "Wizard",
        classId: "wizard",
        level: 10,
        scores: new() { ["STR"] = 8, ["DEX"] = 14, ["CON"] = 14, ["INT"] = 18, ["WIS"] = 12, ["CHA"] = 10 },
        features: Enumerable.Range(1, 12).Select(i => Feature($"feature{i}", $"Featurename{i} Arcana", (i % 10) + 1, "wizard")).ToList(),
        spells: Enumerable.Range(1, 30).Select(i => Spell($"spell{i}", $"Spellname{i} Bolt", i % 6)).ToList(),
        spellSlots: new SpellSlots([4, 3, 3, 3, 2], null));

    private static CharacterDetailsReadModel Character(
        string name, string className, string classId, int level, Dictionary<string, int> scores,
        List<FeatureReadModel> features, List<SpellReadModel> spells, SpellSlots spellSlots) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = name,
        Level = level,
        Edition = "2014",
        RaceId = "dwarf",
        RaceName = "Dwarf",
        ClassId = classId,
        ClassName = className,
        BackgroundId = "soldier",
        BackgroundName = "Soldier",
        Size = "Medium",
        AbilityScores = scores,
        ProficiencyIds = ["athletics", "perception"],
        Proficiencies =
        [
            new ProficiencyReadModel { Id = "athletics", Name = "Athletics", Type = "skill" },
            new ProficiencyReadModel { Id = "perception", Name = "Perception", Type = "skill" },
            new ProficiencyReadModel { Id = "light_armor", Name = "Light Armor", Type = "armor" },
        ],
        Languages = [new LanguageReadModel { Id = "common", Name = "Common", Type = "standard" }],
        Features = features,
        Spells = spells,
        Stats = CharacterStatsCalculator.Calculate(new CharacterStatsInput(
            Level: level,
            AbilityScores: scores,
            ProficiencyIds: ["athletics", "perception"],
            HitDie: "d10",
            HitDieResults: new Dictionary<int, int>(),
            SavingThrowAbilityIds: ["STR", "CON"],
            Speed: 25,
            RaceId: "dwarf",
            SubraceId: null,
            SubclassId: null,
            FeatIds: [],
            UnarmoredArmorClasses: [],
            SpellSlots: spellSlots)),
    };

    private static FeatureReadModel Feature(string id, string name, int level, string classId) => new()
    {
        Id = id,
        Name = name,
        Level = level,
        ClassId = classId,
        Description = $"{LongText(60)} {id}end",
    };

    private static SpellReadModel Spell(string id, string name, int level) => new()
    {
        Id = id,
        Name = name,
        Level = level,
        School = "Evocation",
        CastingTimeNumber = 1,
        CastingTimeUnit = "action",
        RangeType = "point",
        RangeDistanceType = "feet",
        RangeDistanceAmount = 120,
        HasVerbalComponent = true,
        HasSomaticComponent = true,
        DurationType = "instant",
        Description = $"{LongText(140)} {id}end",
    };

    private static readonly string[] Vocabulary = ["a", "creature", "within", "range", "takes", "damage", "and", "falls", "prone"];

    private static string LongText(int words) =>
        string.Join(' ', Enumerable.Range(0, words).Select(i => Vocabulary[i % Vocabulary.Length]));
}
