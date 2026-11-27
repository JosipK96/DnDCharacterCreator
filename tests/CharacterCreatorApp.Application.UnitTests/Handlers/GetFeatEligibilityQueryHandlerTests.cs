using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.GetFeatEligibility;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels.Feats;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Feats;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class GetFeatEligibilityQueryHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IRaceRepository> _raceRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFeatRepository> _featRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly GetFeatEligibilityQueryHandler _sut;

    private readonly Guid _userId = new("00000000-0000-0000-0000-000000000001");
    private readonly Guid _draftId = new("00000000-0000-0000-0000-000000000002");

    public GetFeatEligibilityQueryHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);

        // A level 4 2014 dwarf fighter: Intelligence 12, Wisdom 10, Constitution 13 + 2
        var draft = CharacterDraft.CreateEmpty(_userId, new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
        draft.ChooseEdition(RulesEdition.Edition2014);
        draft.SetNameAndLevel("Thorin", 4);
        draft.SetRace("dwarf", null, null);
        draft.SetClass("fighter");
        draft.SetBaseAbilityScores(
            new Dictionary<string, int> { ["STR"] = 15, ["DEX"] = 14, ["CON"] = 13, ["INT"] = 12, ["WIS"] = 10, ["CHA"] = 8 },
            AbilityScoreMethod.Rolled);

        _draftRepositoryMock.Setup(r => r.GetByIdAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("fighter", _cts.Token)).ReturnsAsync(new Class
        {
            Id = "fighter",
            Name = "Fighter",
            Edition = RulesEdition.Edition2014,
            Source = "PHB",
            HitDie = "1d10",
            Features = [],
        });
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("dwarf", _cts.Token)).ReturnsAsync(new Race
        {
            Id = "dwarf",
            Name = "Dwarf",
            Edition = RulesEdition.Edition2014,
            Source = "PHB",
            Size = "Medium",
            Speed = 25,
            AbilityBonuses = [new RaceAbilityBonus { RaceId = "dwarf", AbilityScoreId = "CON", Bonus = 2 }],
        });
        _featRepositoryMock.Setup(r => r.GetAllAsync(RulesEdition.Edition2014, _cts.Token)).ReturnsAsync(
        [
            Feat("alert", new FeatPrerequisites()),
            Feat("elven_accuracy", new FeatPrerequisites { RaceIds = ["elf", "half_elf"] }),
            Feat("durable", new FeatPrerequisites { AbilityMinimums = [new AbilityMinimum { AbilityId = "CON", Score = 15 }] }),
            Feat("ritual_caster", new FeatPrerequisites
            {
                AbilityMinimums = [new AbilityMinimum { AbilityId = "INT", Score = 13 }, new AbilityMinimum { AbilityId = "WIS", Score = 13 }],
            }),
            Feat("weapon_master", new FeatPrerequisites { Unchecked = ["Proficiency with a martial weapon"] }),
            Feat("squat_nimbleness", new FeatPrerequisites(), source: "XGE"),
        ]);

        _sut = new GetFeatEligibilityQueryHandler(
            _draftRepositoryMock.Object,
            _classRepositoryMock.Object,
            _raceRepositoryMock.Object,
            new Mock<IBackgroundRepository>(MockBehavior.Strict).Object,
            _featRepositoryMock.Object,
            _userContextMock.Object);
    }

    [Fact]
    public async Task Handle_WithNoPointsSpent_SaysWhatEachFeatOfTheDraftsBooksNeeds()
    {
        // Act
        var result = await _sut.Handle(new GetFeatEligibilityQuery(_draftId, []), _cts.Token);

        // Assert - the dwarf's Constitution bonus counts; Xanathar's is not among the draft's books
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(
        [
            Eligibility("alert", null, null, checkWithDm: false),
            Eligibility("elven_accuracy", "Elf or Half-Elf", "Elf or Half-Elf", checkWithDm: false),
            Eligibility("durable", null, null, checkWithDm: false),
            Eligibility("ritual_caster", "Intelligence or Wisdom 13", "Intelligence or Wisdom 13", checkWithDm: false),
            Eligibility("weapon_master", null, null, checkWithDm: true),
        ]);
    }

    [Fact]
    public async Task Handle_WithAPointInIntelligence_CountsItForAnImprovementButNotForTheRacesFeat()
    {
        // Act - "int" in any spelling is Intelligence
        var result = await _sut.Handle(new GetFeatEligibilityQuery(_draftId, ["int"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Single(e => e.FeatId == "ritual_caster").Should().BeEquivalentTo(
            Eligibility("ritual_caster", null, "Intelligence or Wisdom 13", checkWithDm: false));
    }

    [Fact]
    public async Task Handle_WithPointsSpreadOverIncreases_AddsThemUp()
    {
        // Act - Wisdom 10 + 3
        var result = await _sut.Handle(new GetFeatEligibilityQuery(_draftId, ["WIS", "wisdom", "Wisdom"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Single(e => e.FeatId == "ritual_caster").Unmet.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenTheDraftIsNotTheirs_ReturnsNotFound()
    {
        // Arrange
        _draftRepositoryMock.Setup(r => r.GetByIdAsync(_draftId, _userId, _cts.Token)).ReturnsAsync((CharacterDraft?)null);

        // Act
        var result = await _sut.Handle(new GetFeatEligibilityQuery(_draftId, []), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.NotFound");
    }

    [Fact]
    public async Task Handle_BeforeAClassIsChosen_ReturnsClassAndLevelRequired()
    {
        // Arrange
        var draft = CharacterDraft.CreateEmpty(_userId, new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
        draft.SetNameAndLevel("Thorin", 4);
        _draftRepositoryMock.Setup(r => r.GetByIdAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);

        // Act
        var result = await _sut.Handle(new GetFeatEligibilityQuery(_draftId, []), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.ClassAndLevelRequired");
    }

    [Fact]
    public async Task Handle_BeforeALevelIsChosen_ReturnsClassAndLevelRequired()
    {
        // Arrange
        var draft = CharacterDraft.CreateEmpty(_userId, new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
        draft.SetClass("fighter");
        _draftRepositoryMock.Setup(r => r.GetByIdAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);

        // Act
        var result = await _sut.Handle(new GetFeatEligibilityQuery(_draftId, []), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.ClassAndLevelRequired");
    }

    [Fact]
    public async Task Handle_WhenTheClassIsGone_ReturnsClassNotFound()
    {
        // Arrange
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("fighter", _cts.Token)).ReturnsAsync((Class?)null);

        // Act
        var result = await _sut.Handle(new GetFeatEligibilityQuery(_draftId, []), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("Class.NotFound");
    }

    private static FeatEligibilityReadModel Eligibility(string featId, string? unmet, string? unmetAsRaceFeat, bool checkWithDm) => new()
    {
        FeatId = featId,
        Unmet = unmet,
        UnmetAsRaceFeat = unmetAsRaceFeat,
        CheckWithDm = checkWithDm,
    };

    private static FeatReadModel Feat(string id, FeatPrerequisites prerequisites, string source = "PHB") => new()
    {
        Id = id,
        Name = id,
        Description = "A feat.",
        Edition = RulesEdition.Edition2014,
        Source = source,
        Prerequisites = prerequisites,
    };

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
