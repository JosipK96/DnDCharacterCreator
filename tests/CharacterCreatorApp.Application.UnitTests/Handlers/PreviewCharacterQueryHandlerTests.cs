using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.PreviewCharacter;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Feats;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class PreviewCharacterQueryHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IRaceRepository> _raceRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IBackgroundRepository> _backgroundRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ILanguageRepository> _languageRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFeatRepository> _featRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IProficiencyRepository> _proficiencyRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFeatureRepository> _featureRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ISpellRepository> _spellRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly PreviewCharacterQueryHandler _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _draftId = Guid.NewGuid();

    public PreviewCharacterQueryHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);

        _languageRepositoryMock
            .Setup(r => r.GetAllForEditionAsync(RulesEdition.Edition2014, _cts.Token))
            .ReturnsAsync(
            [
                new LanguageReadModel { Id = "common-phb", Name = "Common", Type = "standard" },
                new LanguageReadModel { Id = "dwarvish-phb", Name = "Dwarvish", Type = "standard" }
            ]);
        _featRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token)).ReturnsAsync([]);
        _proficiencyRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<string>>(), _cts.Token)).ReturnsAsync([]);
        _featureRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<string>>(), _cts.Token)).ReturnsAsync([]);
        _spellRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<string>>(), _cts.Token)).ReturnsAsync([]);
        _languageRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token)).ReturnsAsync([]);

        _sut = new PreviewCharacterQueryHandler(
            _draftRepositoryMock.Object,
            _raceRepositoryMock.Object,
            _classRepositoryMock.Object,
            _backgroundRepositoryMock.Object,
            _languageRepositoryMock.Object,
            _featRepositoryMock.Object,
            _proficiencyRepositoryMock.Object,
            _featureRepositoryMock.Object,
            _spellRepositoryMock.Object,
            _userContextMock.Object,
            new FakeTimeProvider());
    }

    [Fact]
    public async Task Handle_ReturnsTheStatsTheCharacterWillHave()
    {
        // Arrange - a level 5 dwarf fighter: d10 hit die, CON 13 + 2 racial = 15 (+2)
        ArrangeDraft(level: 5);

        // Act
        var result = await _sut.Handle(new PreviewCharacterQuery(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var stats = result.Value.Stats;
        stats.ProficiencyBonus.Should().Be(3);
        stats.MaxHitPoints.Should().Be(12 + (4 * 8));
        stats.HitDice.Should().Be("5d10");
        stats.Speed.Should().Be(25);

        // Fighters are proficient in strength and constitution saves
        stats.SavingThrows.First(s => s.AbilityId == "STR").IsProficient.Should().BeTrue();
        stats.SavingThrows.First(s => s.AbilityId == "DEX").IsProficient.Should().BeFalse();
    }

    [Theory]
    [InlineData("high_elf", 30)]
    [InlineData("wood_elf", 35)]
    public async Task Handle_TakesTheSpeedFromTheSubraceWhereItHasItsOwn(string subraceId, int expectedSpeed)
    {
        // Arrange - an elf walks 30 feet, and a wood elf 35
        var draft = ArrangeDraft(level: 1);
        draft.SetRace("elf", subraceId);

        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("elf", _cts.Token)).ReturnsAsync(new Race
        {
            Id = "elf",
            Name = "Elf",
            Edition = RulesEdition.Edition2014,
            Source = "PHB",
            Size = "Medium",
            Speed = 30,
            Subraces =
            [
                new Subrace { Id = "high_elf", Name = "High Elf", Source = "PHB", RaceId = "elf" },
                new Subrace { Id = "wood_elf", Name = "Wood Elf", Source = "PHB", RaceId = "elf", Speed = 35 },
            ]
        });

        // Act
        var result = await _sut.Handle(new PreviewCharacterQuery(_draftId), _cts.Token);

        // Assert
        result.Value.Stats.Speed.Should().Be(expectedSpeed);
    }

    [Fact]
    public async Task Handle_ReportsNamesRatherThanIds()
    {
        // Arrange
        ArrangeDraft(level: 1);

        // Act
        var result = await _sut.Handle(new PreviewCharacterQuery(_draftId), _cts.Token);

        // Assert
        result.Value.Name.Should().Be("Thorin");
        result.Value.RaceName.Should().Be("Dwarf");
        result.Value.ClassName.Should().Be("Fighter");
        result.Value.BackgroundName.Should().Be("Soldier");
    }

    [Fact]
    public async Task Handle_WithAMissingChoice_StillReturnsThePreviewAndSaysWhatIsMissing()
    {
        // Arrange - a level 4 fighter owes one ability score improvement
        var draft = ArrangeDraft(level: 4);
        draft.SetAbilityScoreImprovements([]);

        // Act
        var result = await _sut.Handle(new PreviewCharacterQuery(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BlockingIssue.Should().Contain("ability score improvement");
    }

    [Fact]
    public async Task Handle_WithEverythingChosen_ReportsNoBlockingIssue()
    {
        // Arrange
        ArrangeDraft(level: 4);

        // Act
        var result = await _sut.Handle(new PreviewCharacterQuery(_draftId), _cts.Token);

        // Assert
        result.Value.BlockingIssue.Should().BeNull();
    }

    [Theory]
    [InlineData("cleric", "Wisdom", 1, 14, 3)]
    [InlineData("druid", "Wisdom", 1, 16, 4)]
    [InlineData("paladin", "Charisma", 2, 14, 3)]
    [InlineData("artificer", "Intelligence", 1, 16, 3)]
    public async Task Handle_ForA2014PreparedCaster_ListsItsPreparedSpells(string classId, string ability, int level, int score, int prepared)
    {
        // Arrange - its modifier plus its level, half of it for a paladin or artificer
        List<string> spellIds = [.. Enumerable.Range(1, prepared).Select(i => $"level_1_spell_{i}")];
        ArrangePreparedCasterDraft(classId, ability, level, score, spellIds);

        // Act
        var result = await _sut.Handle(new PreviewCharacterQuery(_draftId), _cts.Token);

        // Assert
        result.Value.BlockingIssue.Should().BeNull();
        result.Value.Spells.Select(s => s.Id).Should().Equal(spellIds);
    }

    [Fact]
    public async Task Handle_ForAClericWithFewerSpellsThanItPrepares_SaysWhatIsMissing()
    {
        // Arrange - WIS 16 at level 1 prepares four; two were picked
        ArrangePreparedCasterDraft("cleric", "Wisdom", 1, 16, ["level_1_spell_1", "level_1_spell_2"]);

        // Act
        var result = await _sut.Handle(new PreviewCharacterQuery(_draftId), _cts.Token);

        // Assert
        result.Value.BlockingIssue.Should().Be("A level 1 cleric picks exactly 4 spell(s) above cantrips. The draft has 2.");
    }

    [Fact]
    public async Task Handle_WithResilientForWisdom_MarksTheWisdomSaveProficient()
    {
        // Arrange - the fighter's improvement at 4 spent on Resilient, raising Wisdom
        ArrangeResilientDraft();

        // Act
        var result = await _sut.Handle(new PreviewCharacterQuery(_draftId), _cts.Token);

        // Assert
        result.Value.BlockingIssue.Should().BeNull();
        result.Value.Stats.SavingThrows.Where(s => s.IsProficient).Select(s => s.AbilityId).Should().BeEquivalentTo(["STR", "CON", "WIS"]);
    }

    [Fact]
    public async Task Handle_WithAFeatTakenOnce_ListsItOnceWithoutSpells()
    {
        // Arrange
        ArrangeResilientDraft();

        // Act
        var result = await _sut.Handle(new PreviewCharacterQuery(_draftId), _cts.Token);

        // Assert
        result.Value.Feats.Should().ContainSingle().Which.Feat.Id.Should().Be("resilient");
        result.Value.Feats[0].Spells.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithKeenMindsSkillNotPicked_SaysWhatIsMissing()
    {
        // Arrange
        var draft = ArrangeKeenMindDraft();
        draft.SetFeats(["keen_mind_2014"], [[]]);

        // Act
        var result = await _sut.Handle(new PreviewCharacterQuery(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BlockingIssue.Should().Be("Pick the skills or tools of Keen Mind, one pick each, in that order. Got 0 pick(s).");
    }

    [Fact]
    public async Task Handle_WithKeenMindsSkillPicked_MarksTheSkillProficient()
    {
        // Arrange
        ArrangeKeenMindDraft();

        // Act
        var result = await _sut.Handle(new PreviewCharacterQuery(_draftId), _cts.Token);

        // Assert
        result.Value.BlockingIssue.Should().BeNull();
        result.Value.Stats.Skills.Single(s => s.SkillId == "arcana").IsProficient.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "survivor")]
    [InlineData("mist_walker", "mist_walker")]
    public async Task Handle_WithABackgroundThatAllowsADarkGift_ListsThePickOrElseItsOwnFeat(string? pickedFeatId, string expectedFeatId)
    {
        // Arrange
        ArrangeHauntedOneDraft(pickedFeatId);

        // Act
        var result = await _sut.Handle(new PreviewCharacterQuery(_draftId), _cts.Token);

        // Assert
        result.Value.BlockingIssue.Should().BeNull();
        result.Value.Feats.Should().ContainSingle().Which.Feat.Id.Should().Be(expectedFeatId);
    }

    [Fact]
    public async Task Handle_WithAnotherCategoryPickedInPlaceOfTheBackgroundsFeat_SaysWhatIsWrong()
    {
        // Arrange
        ArrangeHauntedOneDraft("alert");

        // Act
        var result = await _sut.Handle(new PreviewCharacterQuery(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BlockingIssue.Should().Be("A feat picked for the Haunted One background is a Dark Gift; Alert is not.");
    }

    [Fact]
    public async Task Handle_ShowsTheRacesSize()
    {
        // Arrange
        ArrangeDraft(level: 1);

        // Act
        var result = await _sut.Handle(new PreviewCharacterQuery(_draftId), _cts.Token);

        // Assert
        result.Value.Size.Should().Be("Medium");
    }

    [Fact]
    public async Task Handle_WithAnIncompleteDraft_ReturnsValidationError()
    {
        // Arrange - no ability scores set
        var draft = CharacterDraft.CreateEmpty(_userId, new FakeTimeProvider());
        draft.SetNameAndLevel("Thorin", 1);

        _draftRepositoryMock.Setup(r => r.GetByIdAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);

        // Act
        var result = await _sut.Handle(new PreviewCharacterQuery(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.Invalid");
    }

    // A level 4 fighter whose improvement went on Resilient for Wisdom
    private void ArrangeResilientDraft()
    {
        var draft = ArrangeDraft(level: 4);
        draft.SetAbilityScoreImprovements([]);
        draft.SetFeats(["resilient"], [new Dictionary<string, int> { ["WIS"] = 1 }]);
        _featRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token)).ReturnsAsync(
        [
            new FeatReadModel
            {
                Id = "resilient",
                Name = "Resilient",
                Description = "A feat.",
                Edition = RulesEdition.Edition2014,
                Source = "PHB",
                AbilityIncrease = new FeatAbilityIncrease { Options = ["STR", "DEX", "CON", "INT", "WIS", "CHA"] },
                Choice = new FeatChoice { Type = FeatChoice.SavingThrow, Count = 1, Options = ["STR", "DEX", "CON", "INT", "WIS", "CHA"] },
            },
        ]);
    }

    // A level 4 fighter whose improvement went on Keen Mind, with Arcana picked for it
    private CharacterDraft ArrangeKeenMindDraft()
    {
        var draft = ArrangeDraft(level: 4);
        draft.SetAbilityScoreImprovements([]);
        draft.SetFeats(["keen_mind_2014"], [[]], [new FeatProficiencyPick { FeatId = "keen_mind_2014", ProficiencyIds = ["arcana"] }]);
        _featRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token)).ReturnsAsync(
        [
            new FeatReadModel
            {
                Id = "keen_mind_2014",
                Name = "Keen Mind",
                Description = "A feat.",
                Edition = RulesEdition.Edition2014,
                Source = "PHB",
                Choice = new FeatChoice { Type = FeatChoice.Skills, Count = 1, Options = ["arcana", "history"] },
            },
        ]);
        return draft;
    }

    // A level 1 fighter whose background gives Survivor, or a Dark Gift in its place
    private void ArrangeHauntedOneDraft(string? pickedFeatId)
    {
        var draft = ArrangeDraft(level: 1);
        draft.SetBackground("haunted_one", null, pickedFeatId);

        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("haunted_one", _cts.Token)).ReturnsAsync(new Background
        {
            Id = "haunted_one",
            Name = "Haunted One",
            Edition = RulesEdition.Edition2014,
            Source = "PHB",
            FeatId = "survivor",
            FeatAlternativeCategory = "Dark Gift",
            Languages = [],
            BackgroundProficiencies = []
        });
        _featRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token)).ReturnsAsync(
        [
            new FeatReadModel { Id = "survivor", Name = "Survivor", Description = "A feat.", Edition = RulesEdition.Edition2014, Source = "PHB", Category = "Origin" },
            new FeatReadModel { Id = "mist_walker", Name = "Mist Walker", Description = "A feat.", Edition = RulesEdition.Edition2014, Source = "PHB", Category = "Dark Gift" },
            new FeatReadModel { Id = "alert", Name = "Alert", Description = "A feat.", Edition = RulesEdition.Edition2014, Source = "PHB", Category = "General" },
        ]);
    }

    // The dwarf soldier as a 2014 prepared caster with these spells picked, its spellcasting
    // ability at this score; its class's table gives no count, and it casts up to level 1
    private CharacterDraft ArrangePreparedCasterDraft(string classId, string ability, int level, int score, List<string> spellIds)
    {
        var draft = ArrangeDraft(level);
        draft.SetClass(classId);
        var scores = new Dictionary<string, int> { ["STR"] = 10, ["DEX"] = 10, ["CON"] = 10, ["INT"] = 10, ["WIS"] = 10, ["CHA"] = 10 };
        scores[AbilityScore.Normalize(ability)] = score;
        draft.SetBaseAbilityScores(scores, AbilityScoreMethod.Rolled);
        draft.SetSpells(spellIds);

        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(classId, _cts.Token)).ReturnsAsync(new Class
        {
            Id = classId,
            Name = classId,
            Edition = RulesEdition.Edition2014,
            Source = "PHB",
            HitDie = "1d8",
            SubclassLevel = 20,
            Spellcasting = new Spellcasting
            {
                ClassId = classId,
                SpellcastingAbility = ability,
                IsPreparedCaster = true,
                PreparedSpellsLevelDivisor = classId is "paladin" or "artificer" ? 2 : 1,
            },
            SpellProgression = [new ClassSpellProgression { ClassId = classId, CharacterLevel = level, CantripsKnown = 0, SpellsKnown = 0, MaxSpellLevel = 1 }],
        });

        var spells = Enumerable.Range(1, 6).Select(i => PreviewSpell($"level_1_spell_{i}")).ToList();
        _spellRepositoryMock.Setup(r => r.GetByClassIdAsync(classId, _cts.Token)).ReturnsAsync(spells);
        _spellRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.Is<List<string>>(ids => ids.SequenceEqual(spellIds)), _cts.Token))
            .ReturnsAsync(spells.FindAll(s => spellIds.Contains(s.Id)));

        return draft;
    }

    private static SpellReadModel PreviewSpell(string id) => new()
    {
        Id = id,
        Name = id,
        Source = "PHB",
        Level = 1,
        School = "Abjuration",
        CastingTimeUnit = "action",
        RangeType = "point",
        DurationType = "instant",
        Description = "A spell."
    };

    private CharacterDraft ArrangeDraft(int level)
    {
        var draft = CharacterDraft.CreateEmpty(_userId, new FakeTimeProvider());
        draft.ChooseEdition(RulesEdition.Edition2014);
        draft.SetNameAndLevel("Thorin", level);
        draft.SetRace("dwarf");
        draft.SetClass("fighter", level >= 3 ? "champion" : null);
        draft.SetBackground("soldier");
        draft.SetBaseAbilityScores(new Dictionary<string, int>
        {
            ["STR"] = 15,
            ["DEX"] = 14,
            ["CON"] = 13,
            ["INT"] = 12,
            ["WIS"] = 10,
            ["CHA"] = 8
        }, AbilityScoreMethod.Rolled);
        draft.SetAbilityScoreImprovements(
            [.. Enumerable.Range(0, new[] { 4, 6, 8, 12, 14, 16, 19 }.Count(l => l <= level))
                .Select(_ => new Dictionary<string, int> { ["strength"] = 2 })]);

        _draftRepositoryMock.Setup(r => r.GetByIdAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);

        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("dwarf", _cts.Token)).ReturnsAsync(new Race
        {
            Id = "dwarf",
            Name = "Dwarf",
            Edition = RulesEdition.Edition2014,
            Source = "PHB",
            Size = "Medium",
            Speed = 25,
            AbilityBonuses = [new RaceAbilityBonus { RaceId = "dwarf", AbilityScoreId = "con", Bonus = 2 }],
            RaceLanguages = [new RaceLanguage { RaceId = "dwarf", Language = "Common" }],
            RaceProficiencies = [],
            Traits = [],
            Subraces = []
        });

        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("fighter", _cts.Token)).ReturnsAsync(new Class
        {
            Id = "fighter",
            Name = "Fighter",
            Edition = RulesEdition.Edition2014,
            Source = "PHB",
            HitDie = "1d10",
            SubclassLevel = 3,
            AsiLevels = [4, 6, 8, 12, 14, 16, 19],
            SavingThrowAbilityIds = ["STR", "CON"],
            ClassProficiencies = [],
            Features = [],
            Subclasses = [new Subclass { Source = "PHB", Id = "champion", ClassId = "fighter", Name = "Champion", Features = [] }]
        });

        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("soldier", _cts.Token)).ReturnsAsync(new Background
        {
            Id = "soldier",
            Name = "Soldier",
            Edition = RulesEdition.Edition2014,
            Source = "PHB",
            FeatureId = "military-rank",
            Languages = [],
            BackgroundProficiencies = []
        });

        return draft;
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
