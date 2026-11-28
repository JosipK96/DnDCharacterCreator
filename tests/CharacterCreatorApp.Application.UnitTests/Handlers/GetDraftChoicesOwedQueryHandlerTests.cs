using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.GetDraftChoicesOwed;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels.Languages;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class GetDraftChoicesOwedQueryHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IRaceRepository> _raceRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IBackgroundRepository> _backgroundRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ILanguageRepository> _languageRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly GetDraftChoicesOwedQueryHandler _sut;

    private readonly Guid _userId = new("00000000-0000-0000-0000-000000000001");
    private readonly Guid _draftId = new("00000000-0000-0000-0000-000000000002");

    private static readonly List<LanguageReadModel> Choosable =
    [
        new() { Id = "common", Name = "Common", Type = "Standard" },
        new() { Id = "elvish", Name = "Elvish", Type = "Standard" },
        new() { Id = "dwarvish", Name = "Dwarvish", Type = "Standard" },
    ];

    public GetDraftChoicesOwedQueryHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);

        _sut = new GetDraftChoicesOwedQueryHandler(
            _draftRepositoryMock.Object,
            _raceRepositoryMock.Object,
            _classRepositoryMock.Object,
            _backgroundRepositoryMock.Object,
            _languageRepositoryMock.Object,
            _userContextMock.Object);
    }

    [Fact]
    public async Task Handle_ForAHighElfFighterAcolyte_ReturnsWhatEachChoiceOwes()
    {
        // Arrange - a level 6 fighter (improvements at 4 and 6, a d10), a high elf (Common and
        // Elvish, one language to pick) and an acolyte (two to pick)
        ArrangeDraft(level: 6, raceId: "elf", subraceId: "high-elf", classId: "fighter", backgroundId: "acolyte");
        ArrangeRace(new Race
        {
            Id = "elf",
            Name = "Elf",
            Edition = RulesEdition.Edition2014,
            Size = "Medium",
            RaceLanguages = [new RaceLanguage { RaceId = "elf", Language = "Common" }, new RaceLanguage { RaceId = "elf", Language = "Elvish" }],
            Subraces = [new Subrace { Id = "high-elf", Name = "High Elf", RaceId = "elf", LanguageChoiceCount = 1 }],
        });
        ArrangeClass("fighter", "1d10", 4, 6, 8, 12, 14, 16, 19);
        ArrangeBackground(new Background { Id = "acolyte", Name = "Acolyte", Edition = RulesEdition.Edition2014, Languages = ["any", "any"] });
        _languageRepositoryMock.Setup(r => r.GetChoosableAsync(RulesEdition.Edition2014, _cts.Token)).ReturnsAsync(Choosable);

        // Act
        var result = await _sut.Handle(new GetDraftChoicesOwedQuery(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new
        {
            Improvements = 2,
            ImprovementLevels = new List<int> { 4, 6, 8, 12, 14, 16, 19 },
            BonusFeats = 0,
            BonusFeatCategory = (string?)null,
            BackgroundFeatId = (string?)null,
            Languages = 3,
            KnownLanguageIds = new List<string> { "common", "elvish" },
            HitDieSize = 10,
            HitDieFixedValue = 6,
        });
    }

    [Theory]
    [InlineData(3, 0)]
    [InlineData(4, 1)]
    [InlineData(10, 3)]
    public async Task Handle_ForARogueOfALevel_CountsTheImprovementLevelsReached(int level, int improvements)
    {
        // Arrange - a rogue improves at 4, 8 and 10 as well as the usual levels
        ArrangeDraft(level: level, classId: "rogue");
        ArrangeClass("rogue", "1d8", 4, 8, 10, 12, 16, 19);

        // Act
        var result = await _sut.Handle(new GetDraftChoicesOwedQuery(_draftId), _cts.Token);

        // Assert
        result.Value.Improvements.Should().Be(improvements);
    }

    [Theory]
    [InlineData("1d6", 6, 4)]
    [InlineData("1d12", 12, 7)]
    public async Task Handle_ForAClass_ReturnsItsHitDieAndItsFixedValue(string hitDie, int size, int fixedValue)
    {
        // Arrange
        ArrangeDraft(level: 1, classId: "fighter");
        ArrangeClass("fighter", hitDie);

        // Act
        var result = await _sut.Handle(new GetDraftChoicesOwedQuery(_draftId), _cts.Token);

        // Assert
        result.Value.HitDieSize.Should().Be(size);
        result.Value.HitDieFixedValue.Should().Be(fixedValue);
    }

    [Theory]
    [InlineData("human-variant", 0, null, 1, null)]
    [InlineData(null, 1, "Origin", 1, "Origin")]
    public async Task Handle_ForARaceThatGivesAFeat_ReturnsItsCountAndCategory(
        string? subraceId, int raceFeats, string? raceCategory, int bonusFeats, string? bonusCategory)
    {
        // Arrange - a variant human's any feat comes with the subrace, a 2024 human's Origin one with the species
        ArrangeDraft(level: 1, raceId: "human", subraceId: subraceId);
        ArrangeRace(new Race
        {
            Id = "human",
            Name = "Human",
            Edition = RulesEdition.Edition2014,
            Size = "Medium",
            FeatChoiceCount = raceFeats,
            FeatChoiceCategory = raceCategory,
            Subraces = [new Subrace { Id = "human-variant", Name = "Variant Human", RaceId = "human", FeatChoiceCount = 1 }],
        });
        _languageRepositoryMock.Setup(r => r.GetChoosableAsync(RulesEdition.Edition2014, _cts.Token)).ReturnsAsync(Choosable);

        // Act
        var result = await _sut.Handle(new GetDraftChoicesOwedQuery(_draftId), _cts.Token);

        // Assert
        result.Value.BonusFeats.Should().Be(bonusFeats);
        result.Value.BonusFeatCategory.Should().Be(bonusCategory);
    }

    [Theory]
    [InlineData("dark-gift", "dark-gift")]
    [InlineData(null, "survivor")]
    public async Task Handle_ForABackgroundWhoseFeatMayBePicked_ReturnsThePickOrElseItsOwn(string? picked, string expected)
    {
        // Arrange - a Haunted One gives Survivor, or a Dark Gift picked in its place
        ArrangeDraft(level: 1, backgroundId: "haunted_one", backgroundFeatId: picked);
        ArrangeBackground(new Background
        {
            Id = "haunted_one",
            Name = "Haunted One",
            Edition = RulesEdition.Edition2014,
            FeatId = "survivor",
            FeatAlternativeCategory = "Dark Gift",
        });
        _languageRepositoryMock.Setup(r => r.GetChoosableAsync(RulesEdition.Edition2014, _cts.Token)).ReturnsAsync(Choosable);

        // Act
        var result = await _sut.Handle(new GetDraftChoicesOwedQuery(_draftId), _cts.Token);

        // Assert
        result.Value.BackgroundFeatId.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_BeforeTheRaceClassAndBackgroundAreChosen_OwesNothingAndLoadsNothing()
    {
        // Arrange - the strict repositories have nothing set up, so any load would throw
        ArrangeDraft(level: 1);

        // Act
        var result = await _sut.Handle(new GetDraftChoicesOwedQuery(_draftId), _cts.Token);

        // Assert
        result.Value.Should().BeEquivalentTo(new
        {
            Improvements = 0,
            ImprovementLevels = new List<int>(),
            BonusFeats = 0,
            BackgroundFeatId = (string?)null,
            Languages = 0,
            KnownLanguageIds = new List<string>(),
            HitDieSize = 0,
            HitDieFixedValue = 0,
        });
    }

    [Fact]
    public async Task Handle_WithAMissingDraft_ReturnsNotFound()
    {
        // Arrange
        _draftRepositoryMock.Setup(r => r.GetByIdAsync(_draftId, _userId, _cts.Token)).ReturnsAsync((CharacterDraft?)null);

        // Act
        var result = await _sut.Handle(new GetDraftChoicesOwedQuery(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.NotFound");
    }

    private void ArrangeDraft(
        int level,
        string? raceId = null,
        string? subraceId = null,
        string? classId = null,
        string? backgroundId = null,
        string? backgroundFeatId = null)
    {
        var draft = CharacterDraft.CreateEmpty(_userId, new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
        draft.ChooseEdition(RulesEdition.Edition2014);
        draft.SetNameAndLevel("Elminster", level);

        if (raceId != null)
        {
            draft.SetRace(raceId, subraceId);
        }

        if (classId != null)
        {
            draft.SetClass(classId);
        }

        if (backgroundId != null)
        {
            draft.SetBackground(backgroundId, featId: backgroundFeatId);
        }

        _draftRepositoryMock.Setup(r => r.GetByIdAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
    }

    private void ArrangeRace(Race race) =>
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(race.Id, _cts.Token)).ReturnsAsync(race);

    private void ArrangeClass(string classId, string hitDie, params int[] asiLevels) =>
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(classId, _cts.Token)).ReturnsAsync(new Class
        {
            Id = classId,
            Name = classId,
            Edition = RulesEdition.Edition2014,
            HitDie = hitDie,
            AsiLevels = [.. asiLevels],
        });

    private void ArrangeBackground(Background background) =>
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(background.Id, _cts.Token)).ReturnsAsync(background);

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
