using CharacterCreatorApp.Application.Features.Lookup.GetSpells;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;

using FluentAssertions;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Lookup;

public class GetSpellsQueryHandlerTests : IDisposable
{
    private readonly Mock<ISpellRepository> _spellRepositoryMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly GetSpellsQueryHandler _sut;

    public GetSpellsQueryHandlerTests()
    {
        _sut = new GetSpellsQueryHandler(_spellRepositoryMock.Object);
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Handle_WithAClassAndAMaxLevel_ReturnsWhatTheRepositoryFindsForThatClass()
    {
        // Arrange
        _spellRepositoryMock.Setup(r => r.GetByClassIdAndMaxLevelAsync("wizard", 1, _cts.Token))
            .ReturnsAsync([Spell("fire-bolt", 0, "PHB"), Spell("magic-missile", 1, "PHB")]);

        // Act
        var result = await _sut.Handle(new GetSpellsQuery(ClassId: "wizard", MaxLevel: 1), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Select(s => s.Id).Should().Equal("fire-bolt", "magic-missile");
    }

    [Fact]
    public async Task Handle_WithAClassAndNoMaxLevel_ReturnsEveryLevelOfThatClass()
    {
        // Arrange
        _spellRepositoryMock.Setup(r => r.GetByClassIdAsync("wizard", _cts.Token))
            .ReturnsAsync([Spell("fire-bolt", 0, "PHB"), Spell("wish", 9, "PHB")]);

        // Act
        var result = await _sut.Handle(new GetSpellsQuery(ClassId: "wizard"), _cts.Token);

        // Assert
        result.Value.Select(s => s.Id).Should().Equal("fire-bolt", "wish");
    }

    [Fact]
    public async Task Handle_WithAClassAndASubclass_UsesTheClass()
    {
        // Arrange
        _spellRepositoryMock.Setup(r => r.GetByClassIdAndMaxLevelAsync("cleric", 1, _cts.Token))
            .ReturnsAsync([Spell("bless", 1, "PHB")]);

        // Act
        var result = await _sut.Handle(new GetSpellsQuery(ClassId: "cleric", SubclassId: "life", MaxLevel: 1), _cts.Token);

        // Assert
        result.Value.Select(s => s.Id).Should().Equal("bless");
    }

    [Fact]
    public async Task Handle_WithASubclass_KeepsOnlyTheSpellsUpToTheMaxLevel()
    {
        // Arrange
        _spellRepositoryMock.Setup(r => r.GetBySubclassIdAsync("fiend", _cts.Token))
            .ReturnsAsync([Spell("burning-hands", 1, "PHB"), Spell("blindness-deafness", 2, "PHB")]);

        // Act
        var result = await _sut.Handle(new GetSpellsQuery(SubclassId: "fiend", MaxLevel: 1), _cts.Token);

        // Assert
        result.Value.Select(s => s.Id).Should().Equal("burning-hands");
    }

    [Fact]
    public async Task Handle_WithASubclassAndNoMaxLevel_ReturnsEverySpellItAdds()
    {
        // Arrange
        _spellRepositoryMock.Setup(r => r.GetBySubclassIdAsync("fiend", _cts.Token))
            .ReturnsAsync([Spell("burning-hands", 1, "PHB"), Spell("blindness-deafness", 2, "PHB")]);

        // Act
        var result = await _sut.Handle(new GetSpellsQuery(SubclassId: "fiend"), _cts.Token);

        // Assert
        result.Value.Select(s => s.Id).Should().Equal("burning-hands", "blindness-deafness");
    }

    [Fact]
    public async Task Handle_WithSources_KeepsOnlyTheSpellsFromThoseBooks()
    {
        // Arrange
        _spellRepositoryMock.Setup(r => r.GetByClassIdAndMaxLevelAsync("wizard", 1, _cts.Token))
            .ReturnsAsync([Spell("magic-missile", 1, "PHB"), Spell("silvery-barbs", 1, "SCC")]);

        // Act
        var result = await _sut.Handle(new GetSpellsQuery(ClassId: "wizard", MaxLevel: 1, Sources: ["PHB"]), _cts.Token);

        // Assert
        result.Value.Select(s => s.Id).Should().Equal("magic-missile");
    }

    [Fact]
    public async Task Handle_WithNeitherAClassNorASubclass_ReturnsNoSpellsWithoutAskingTheRepository()
    {
        // Act
        var result = await _sut.Handle(new GetSpellsQuery(MaxLevel: 1), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    private static SpellReadModel Spell(string id, int level, string source) => new()
    {
        Id = id,
        Name = id,
        Source = source,
        Level = level,
        Description = "A spell.",
        School = "Evocation",
        CastingTimeUnit = "action",
        RangeType = "ranged",
        DurationType = "instantaneous"
    };
}
