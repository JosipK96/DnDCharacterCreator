using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.PreviewCharacter;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
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
            Size = "Medium",
            Speed = 30,
            Subraces =
            [
                new Subrace { Id = "high_elf", Name = "High Elf", RaceId = "elf" },
                new Subrace { Id = "wood_elf", Name = "Wood Elf", RaceId = "elf", Speed = 35 },
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
            HitDie = "1d10",
            SubclassLevel = 3,
            AsiLevels = [4, 6, 8, 12, 14, 16, 19],
            SavingThrowAbilityIds = ["STR", "CON"],
            ClassProficiencies = [],
            Features = [],
            Subclasses = [new Subclass { Id = "champion", ClassId = "fighter", Name = "Champion", Features = [] }]
        });

        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("soldier", _cts.Token)).ReturnsAsync(new Background
        {
            Id = "soldier",
            Name = "Soldier",
            Edition = RulesEdition.Edition2014,
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
