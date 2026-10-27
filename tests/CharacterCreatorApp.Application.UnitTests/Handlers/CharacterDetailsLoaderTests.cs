using CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterDetails;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class CharacterDetailsLoaderTests : IDisposable
{
    private readonly Mock<ICharacterRepository> _characterRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IProficiencyRepository> _proficiencyRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFeatureRepository> _featureRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ISpellRepository> _spellRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ILanguageRepository> _languageRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFeatRepository> _featRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IRaceRepository> _raceRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IBackgroundRepository> _backgroundRepositoryMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly CharacterDetailsLoader _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _characterId = Guid.NewGuid();

    public CharacterDetailsLoaderTests()
    {
        _sut = new CharacterDetailsLoader(
            _characterRepositoryMock.Object,
            _proficiencyRepositoryMock.Object,
            _featureRepositoryMock.Object,
            _spellRepositoryMock.Object,
            _languageRepositoryMock.Object,
            _featRepositoryMock.Object,
            _classRepositoryMock.Object,
            _raceRepositoryMock.Object,
            _backgroundRepositoryMock.Object);
    }

    [Fact]
    public async Task LoadAsync_WithTheirCharacter_ListsWhatTheSheetShowsAndWorksOutItsNumbers()
    {
        // Arrange - a level 1 dwarf fighter: d10, CON 14 (+2), speed 25, STR and CON saves
        ArrangeCharacter();
        ArrangeSources();
        ArrangeLists();

        // Act
        var result = await _sut.LoadAsync(_characterId, _userId, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var sheet = result.Value;
        sheet.Stats!.MaxHitPoints.Should().Be(12);
        sheet.Stats.HitDice.Should().Be("1d10");
        sheet.Stats.Speed.Should().Be(25);
        sheet.Stats.SavingThrows.Where(s => s.IsProficient).Select(s => s.AbilityId).Should().BeEquivalentTo(["STR", "CON"]);
        sheet.Proficiencies.Should().ContainSingle().Which.Id.Should().Be("athletics");
        sheet.Features.Should().ContainSingle().Which.Id.Should().Be("military-rank");
        sheet.Languages.Should().ContainSingle().Which.Id.Should().Be("common-phb");
        sheet.Feats.Should().ContainSingle().Which.Feat.Id.Should().Be("tough");
    }

    [Fact]
    public async Task LoadAsync_WithASubrace_TakesTheSpeedFromIt()
    {
        // Arrange - a wood elf walks 35 feet where an elf walks 30
        ArrangeCharacter(raceId: "elf", subraceId: "wood_elf");
        ArrangeSources(Race("elf", speed: 30, subraces: [new Subrace { Id = "wood_elf", Name = "Wood Elf", Source = "PHB", RaceId = "elf", Speed = 35 }]));
        ArrangeLists();

        // Act
        var result = await _sut.LoadAsync(_characterId, _userId, _cts.Token);

        // Assert
        result.Value.Stats!.Speed.Should().Be(35);
    }

    [Fact]
    public async Task LoadAsync_WhenTheCharacterIsNotTheirs_ReturnsNotFound()
    {
        // Arrange
        _characterRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(_characterId, _userId, _cts.Token))
            .ReturnsAsync((CharacterDetailsReadModel?)null);

        // Act
        var result = await _sut.LoadAsync(_characterId, _userId, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.NotFound");
    }

    [Fact]
    public async Task LoadAsync_WhenItsClassCannotBeLoaded_ReturnsNotFoundRatherThanASheetOnFallbacks()
    {
        // Arrange
        ArrangeCharacter();
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("fighter", _cts.Token)).ReturnsAsync((Class?)null);

        // Act
        var result = await _sut.LoadAsync(_characterId, _userId, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.ClassNotFound");
    }

    [Fact]
    public async Task LoadAsync_WhenItsRaceCannotBeLoaded_ReturnsNotFoundRatherThanASheetOnFallbacks()
    {
        // Arrange
        ArrangeCharacter();
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("fighter", _cts.Token)).ReturnsAsync(Fighter());
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("dwarf", _cts.Token)).ReturnsAsync((Race?)null);

        // Act
        var result = await _sut.LoadAsync(_characterId, _userId, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.RaceNotFound");
    }

    [Fact]
    public async Task LoadAsync_WhenItsBackgroundCannotBeLoaded_ReturnsNotFoundRatherThanASheetOnFallbacks()
    {
        // Arrange
        ArrangeCharacter();
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("fighter", _cts.Token)).ReturnsAsync(Fighter());
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("dwarf", _cts.Token)).ReturnsAsync(Race("dwarf", speed: 25));
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("soldier", _cts.Token)).ReturnsAsync((Background?)null);

        // Act
        var result = await _sut.LoadAsync(_characterId, _userId, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.BackgroundNotFound");
    }

    [Fact]
    public async Task LoadAsync_WhenItsSubclassIsNoLongerInTheClass_ReturnsNotFound()
    {
        // Arrange
        ArrangeCharacter(subclassId: "battle_master");
        ArrangeSources();

        // Act
        var result = await _sut.LoadAsync(_characterId, _userId, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.SubclassNotFound");
    }

    [Fact]
    public async Task LoadAsync_WhenItsSubraceIsNoLongerInTheRace_ReturnsNotFound()
    {
        // Arrange
        ArrangeCharacter(subraceId: "hill_dwarf");
        ArrangeSources();

        // Act
        var result = await _sut.LoadAsync(_characterId, _userId, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.SubraceNotFound");
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    private static Race Race(string id, int speed, List<Subrace>? subraces = null) => new()
    {
        Id = id,
        Name = id,
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        Size = "Medium",
        Speed = speed,
        AbilityBonuses = [],
        RaceLanguages = [],
        RaceProficiencies = [],
        Traits = [],
        Subraces = subraces ?? []
    };

    private static Class Fighter() => new()
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
    };

    private void ArrangeCharacter(string raceId = "dwarf", string? subraceId = null, string? subclassId = null)
    {
        _characterRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(_characterId, _userId, _cts.Token))
            .ReturnsAsync(new CharacterDetailsReadModel
            {
                Id = _characterId,
                Name = "Thorin",
                Level = 1,
                RaceId = raceId,
                SubraceId = subraceId,
                ClassId = "fighter",
                SubclassId = subclassId,
                BackgroundId = "soldier",
                AbilityScores = new Dictionary<string, int>
                {
                    ["STR"] = 15,
                    ["DEX"] = 14,
                    ["CON"] = 14,
                    ["INT"] = 12,
                    ["WIS"] = 10,
                    ["CHA"] = 8
                },
                ProficiencyIds = ["athletics"],
                LanguageIds = ["common-phb"],
                FeatIds = ["tough"],
            });
    }

    private void ArrangeSources(Race? race = null)
    {
        race ??= Race("dwarf", speed: 25);

        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("fighter", _cts.Token)).ReturnsAsync(Fighter());
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(race.Id, _cts.Token)).ReturnsAsync(race);
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
    }

    private void ArrangeLists()
    {
        _proficiencyRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<List<string>>(), _cts.Token))
            .ReturnsAsync([new ProficiencyReadModel { Id = "athletics", Name = "Athletics", Type = "skill" }]);
        _featureRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.Is<List<string>>(ids => ids.Contains("military-rank")), _cts.Token))
            .ReturnsAsync([new FeatureReadModel { Id = "military-rank", Name = "Military Rank", Description = "Rank" }]);
        _languageRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token))
            .ReturnsAsync([new LanguageReadModel { Id = "common-phb", Name = "Common", Type = "standard" }]);
        _featRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token))
            .ReturnsAsync([new FeatReadModel { Id = "tough", Name = "Tough", Description = "Hardy", Edition = RulesEdition.Edition2014 }]);
    }
}
