using CharacterCreatorApp.Application.Features.Lookup.GetHiddenOptions;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels.Backgrounds;
using CharacterCreatorApp.Application.ReadModels.Books;
using CharacterCreatorApp.Application.ReadModels.Classes;
using CharacterCreatorApp.Application.ReadModels.Feats;
using CharacterCreatorApp.Application.ReadModels.Races;
using CharacterCreatorApp.Domain.DomainModels.Feats;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Lookup;

public class GetHiddenOptionsQueryHandlerTests : IDisposable
{
    private const string Edition = RulesEdition.Edition2014;

    private readonly Mock<IBookRepository> _bookRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IRaceRepository> _raceRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ISubclassRepository> _subclassRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IBackgroundRepository> _backgroundRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFeatRepository> _featRepositoryMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly GetHiddenOptionsQueryHandler _sut;

    public GetHiddenOptionsQueryHandlerTests()
    {
        // The 2014 books, the Player's Handbook first, as the repository orders them
        _bookRepositoryMock.Setup(r => r.GetAllForEditionAsync(Edition, _cts.Token)).ReturnsAsync(() =>
        [
            Book("PHB", "Player's Handbook (2014)", isPlayersHandbook: true),
            Book("TCE", "Tasha's Cauldron of Everything"),
            Book("XGE", "Xanathar's Guide to Everything"),
        ]);

        _sut = new GetHiddenOptionsQueryHandler(
            _bookRepositoryMock.Object,
            _raceRepositoryMock.Object,
            _classRepositoryMock.Object,
            _subclassRepositoryMock.Object,
            _backgroundRepositoryMock.Object,
            _featRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Races_CountsRacesAndTheSubracesTickingTheirOwnBookWouldOffer()
    {
        // Arrange - an XGE subrace of a Player's Handbook race is offered once XGE is ticked; a TCE
        // subrace of an XGE race needs both books, so ticking TCE alone would not offer it
        _raceRepositoryMock.Setup(r => r.GetAllAsync(Edition, _cts.Token)).ReturnsAsync(() =>
        [
            Race("elf", "PHB", Subrace("high_elf", "PHB"), Subrace("xge_elf", "XGE")),
            Race("xge_race", "XGE", Subrace("xge_race_own", "XGE"), Subrace("tce_of_xge", "TCE")),
            Race("tce_race", "TCE"),
        ]);

        // Act
        var result = await _sut.Handle(Query(HiddenOptionLists.Races, ["PHB"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(4);
        Books(result.Value).Should().Equal(("TCE", "Tasha's Cauldron of Everything", 1), ("XGE", "Xanathar's Guide to Everything", 3));
    }

    [Fact]
    public async Task Handle_RacesWithTheRaceBookTicked_CountsItsSubraceFromAnotherBook()
    {
        // Arrange
        _raceRepositoryMock.Setup(r => r.GetAllAsync(Edition, _cts.Token)).ReturnsAsync(() =>
        [
            Race("xge_race", "XGE", Subrace("xge_race_own", "XGE"), Subrace("tce_of_xge", "TCE")),
        ]);

        // Act
        var result = await _sut.Handle(Query(HiddenOptionLists.Races, ["PHB", "XGE"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(1);
        Books(result.Value).Should().Equal(("TCE", "Tasha's Cauldron of Everything", 1));
    }

    [Fact]
    public async Task Handle_Classes_CountsTheClassesOfTheUntickedBooks()
    {
        // Arrange
        _classRepositoryMock.Setup(r => r.GetClassesAsync(Edition, _cts.Token)).ReturnsAsync(() =>
        [
            Class("fighter", "PHB"),
            Class("artificer", "TCE"),
        ]);

        // Act
        var result = await _sut.Handle(Query(HiddenOptionLists.Classes, ["PHB"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(1);
        Books(result.Value).Should().Equal(("TCE", "Tasha's Cauldron of Everything", 1));
    }

    [Fact]
    public async Task Handle_Backgrounds_CountsTheBackgroundsOfTheUntickedBooks()
    {
        // Arrange
        _backgroundRepositoryMock.Setup(r => r.GetAllAsync(Edition, _cts.Token)).ReturnsAsync(() =>
        [
            new BackgroundReadModel { Id = "acolyte", Source = "PHB" },
            new BackgroundReadModel { Id = "xge_one", Source = "XGE" },
            new BackgroundReadModel { Id = "xge_two", Source = "XGE" },
        ]);

        // Act
        var result = await _sut.Handle(Query(HiddenOptionLists.Backgrounds, ["PHB", "TCE"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(2);
        Books(result.Value).Should().Equal(("XGE", "Xanathar's Guide to Everything", 2));
    }

    [Fact]
    public async Task Handle_Feats_LeavesOutA2014FightingStyleNoImprovementBuys()
    {
        // Arrange
        _featRepositoryMock.Setup(r => r.GetAllAsync(Edition, _cts.Token)).ReturnsAsync(() =>
        [
            Feat("alert", "PHB", null),
            Feat("xge_feat", "XGE", null),
            Feat("tce_style", "TCE", FeatCategory.FightingStyle),
        ]);

        // Act
        var result = await _sut.Handle(Query(HiddenOptionLists.Feats, ["PHB"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(1);
        Books(result.Value).Should().Equal(("XGE", "Xanathar's Guide to Everything", 1));
    }

    [Fact]
    public async Task Handle_FeatsWithACategory_CountsOnlyThatCategory()
    {
        // Arrange
        _featRepositoryMock.Setup(r => r.GetAllAsync(Edition, _cts.Token)).ReturnsAsync(() =>
        [
            Feat("origin_one", "XGE", "Origin"),
            Feat("general_one", "XGE", "General"),
            Feat("origin_two", "TCE", "Origin"),
        ]);

        // Act
        var result = await _sut.Handle(Query(HiddenOptionLists.Feats, ["PHB"]) with { Category = "Origin" }, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(2);
        Books(result.Value).Should().Equal(("TCE", "Tasha's Cauldron of Everything", 1), ("XGE", "Xanathar's Guide to Everything", 1));
    }

    [Fact]
    public async Task Handle_Subclasses_CountsTheClassSubclassesOfTheUntickedBooks()
    {
        // Arrange - a ranger's Hunter is in the Player's Handbook, Gloom Stalker and Horizon Walker in XGE
        _classRepositoryMock.Setup(r => r.GetByIdAsync("ranger", _cts.Token)).ReturnsAsync(() => Class("ranger", "PHB"));
        _subclassRepositoryMock.Setup(r => r.GetByClassIdAsync("ranger", _cts.Token)).ReturnsAsync(() =>
        [
            Subclass("hunter", "PHB"),
            Subclass("gloom_stalker", "XGE"),
            Subclass("horizon_walker", "XGE"),
        ]);

        // Act
        var result = await _sut.Handle(Query(HiddenOptionLists.Subclasses, ["PHB"]) with { ClassId = "ranger" }, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(2);
        Books(result.Value).Should().Equal(("XGE", "Xanathar's Guide to Everything", 2));
    }

    [Fact]
    public async Task Handle_SubclassesOfAnUnknownClass_ReturnsClassNotFound()
    {
        // Arrange
        _classRepositoryMock.Setup(r => r.GetByIdAsync("nobody", _cts.Token)).ReturnsAsync((ClassReadModel?)null);

        // Act
        var result = await _sut.Handle(Query(HiddenOptionLists.Subclasses, ["PHB"]) with { ClassId = "nobody" }, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("Class.NotFound");
    }

    [Fact]
    public async Task Handle_WithEveryBookTicked_HidesNothing()
    {
        // Arrange
        _classRepositoryMock.Setup(r => r.GetClassesAsync(Edition, _cts.Token)).ReturnsAsync(() =>
        [
            Class("fighter", "PHB"),
            Class("artificer", "TCE"),
        ]);

        // Act
        var result = await _sut.Handle(Query(HiddenOptionLists.Classes, ["PHB", "TCE", "XGE"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(0);
        result.Value.Books.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AnEntryFromABookOfTheOtherEdition_IsNeverCounted()
    {
        // Arrange - a 2024 Player's Handbook class is no 2014 book's, ticked or not
        _classRepositoryMock.Setup(r => r.GetClassesAsync(Edition, _cts.Token)).ReturnsAsync(() =>
        [
            Class("fighter", "PHB"),
            Class("fighter_2024", "XPHB"),
        ]);

        // Act
        var result = await _sut.Handle(Query(HiddenOptionLists.Classes, ["PHB"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(0);
        result.Value.Books.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithoutSources_StillCountsThePlayersHandbookAsTicked()
    {
        // Arrange
        _classRepositoryMock.Setup(r => r.GetClassesAsync(Edition, _cts.Token)).ReturnsAsync(() =>
        [
            Class("fighter", "PHB"),
            Class("artificer", "TCE"),
        ]);

        // Act
        var result = await _sut.Handle(Query(HiddenOptionLists.Classes, []), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Books(result.Value).Should().Equal(("TCE", "Tasha's Cauldron of Everything", 1));
    }

    private static GetHiddenOptionsQuery Query(string list, string[] sources) => new(Edition, list, sources);

    private static List<(string Code, string Name, int Count)> Books(HiddenOptionsReadModel hidden) =>
        hidden.Books.ConvertAll(b => (b.Code, b.Name, b.Count));

    private static BookReadModel Book(string code, string name, bool isPlayersHandbook = false) =>
        new() { Code = code, Name = name, Edition = Edition, IsPlayersHandbook = isPlayersHandbook };

    private static RaceReadModel Race(string id, string source, params SubraceReadModel[] subraces) =>
        new() { Id = id, Name = id, Edition = Edition, Source = source, Size = "Medium", Subraces = [.. subraces], Picks = new() };

    private static SubraceReadModel Subrace(string id, string source) => new() { Id = id, Name = id, RaceId = "race", Source = source, Picks = new() };

    private static ClassReadModel Class(string id, string source) => new() { Id = id, Name = id, Edition = Edition, Source = source, HitDie = "1d10" };

    private static SubclassReadModel Subclass(string id, string source) =>
        new() { Id = id, Name = id, ClassId = "class", Source = source };

    private static FeatReadModel Feat(string id, string source, string? category) =>
        new() { Id = id, Name = id, Description = id, Edition = Edition, Source = source, Category = category };

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
